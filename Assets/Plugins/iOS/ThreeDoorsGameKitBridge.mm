#import <Foundation/Foundation.h>
#import <GameKit/GameKit.h>
#import <UIKit/UIKit.h>
#import "UnityAppController.h"
#import "UnityInterface.h"

#include <stdint.h>

static NSString *const TDOFDefaultReceiverName = @"AppleGameServices";
static NSString *const TDOFCallbackMethodName = @"OnNativeCloudMessage";

static NSString *gReceiverName = TDOFDefaultReceiverName;
static NSMutableDictionary<NSString *, NSArray<GKSavedGame *> *> *gConflictGroups;
static BOOL gAuthenticationInFlight;
static int gAuthenticationRequestId;

@interface TDOFSavedGameListener : NSObject <GKLocalPlayerListener>
@end

static TDOFSavedGameListener *gSavedGameListener;

static void TDOFRunOnMain(dispatch_block_t block)
{
    if ([NSThread isMainThread])
    {
        block();
        return;
    }

    dispatch_async(dispatch_get_main_queue(), block);
}

static NSString *TDOFStringFromUTF8(const char *value)
{
    if (value == nullptr)
    {
        return nil;
    }

    return [NSString stringWithUTF8String:value];
}

static BOOL TDOFIsValidIdentifier(NSString *identifier)
{
    if (identifier == nil)
    {
        return NO;
    }

    NSCharacterSet *whitespace = [NSCharacterSet whitespaceAndNewlineCharacterSet];
    return [[identifier stringByTrimmingCharactersInSet:whitespace] length] > 0;
}

static NSString *TDOFDescribeError(NSError *error)
{
    if (error == nil)
    {
        return @"";
    }

    NSString *description = error.localizedDescription ?: @"Unknown native error";
    return [NSString stringWithFormat:@"%@ (%ld): %@", error.domain, (long)error.code, description];
}

static NSDictionary<NSString *, id> *TDOFSavePayload(GKSavedGame *savedGame, NSData *data)
{
    NSString *name = savedGame.name ?: @"";
    NSString *encodedData = [data base64EncodedStringWithOptions:0] ?: @"";
    int64_t modifiedAt = savedGame.modificationDate != nil
        ? (int64_t)savedGame.modificationDate.timeIntervalSince1970
        : 0;

    return @{
        @"name": name,
        @"data": encodedData,
        @"modifiedAtUnixSeconds": @(modifiedAt)
    };
}

static void TDOFSendEnvelope(
    NSString *kind,
    int requestId,
    BOOL success,
    NSString *error,
    NSArray<NSDictionary<NSString *, id> *> *saves)
{
    TDOFRunOnMain(^{
        NSDictionary<NSString *, id> *payload = @{
            @"kind": kind ?: @"unknown",
            @"requestId": @(requestId),
            @"success": @(success),
            @"error": error ?: @"",
            @"saves": saves ?: @[]
        };

        NSError *serializationError = nil;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:payload
                                                           options:0
                                                             error:&serializationError];
        NSString *json = jsonData != nil
            ? [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding]
            : nil;

        if (json == nil)
        {
            json = @"{\"kind\":\"bridge\",\"requestId\":0,\"success\":false,\"error\":\"JSON serialization failed\",\"saves\":[]}";
        }

        NSString *receiver = gReceiverName.length > 0 ? gReceiverName : TDOFDefaultReceiverName;
        UnitySendMessage(receiver.UTF8String, TDOFCallbackMethodName.UTF8String, json.UTF8String);
    });
}

static BOOL TDOFRequireAuthenticatedPlayer(NSString *kind, int requestId)
{
    if ([GKLocalPlayer localPlayer].isAuthenticated)
    {
        return YES;
    }

    TDOFSendEnvelope(kind, requestId, NO, @"Game Center local player is not authenticated", @[]);
    return NO;
}

static BOOL TDOFRequireInitializedBridge(NSString *kind, int requestId)
{
    if (gSavedGameListener != nil)
    {
        return YES;
    }

    TDOFSendEnvelope(kind, requestId, NO, @"Cloud bridge must be initialized first", @[]);
    return NO;
}

typedef void (^TDOFLoadedGamesCompletion)(
    NSArray<NSDictionary<NSString *, id> *> *saves,
    NSArray<NSString *> *errors);

static void TDOFLoadSavedGames(
    NSArray<GKSavedGame *> *savedGames,
    TDOFLoadedGamesCompletion completion)
{
    if (savedGames.count == 0)
    {
        completion(@[], @[]);
        return;
    }

    NSMutableArray *slots = [NSMutableArray arrayWithCapacity:savedGames.count];
    NSMutableArray<NSString *> *errors = [NSMutableArray array];
    for (NSUInteger index = 0; index < savedGames.count; index++)
    {
        [slots addObject:NSNull.null];
    }

    dispatch_group_t group = dispatch_group_create();
    [savedGames enumerateObjectsUsingBlock:^(GKSavedGame *savedGame, NSUInteger index, BOOL *stop) {
        dispatch_group_enter(group);
        [savedGame loadDataWithCompletionHandler:^(NSData *data, NSError *error) {
            TDOFRunOnMain(^{
                if (error != nil)
                {
                    [errors addObject:TDOFDescribeError(error)];
                }
                else if (data == nil)
                {
                    [errors addObject:[NSString stringWithFormat:
                        @"Saved game '%@' returned no data", savedGame.name ?: @""]];
                }
                else
                {
                    slots[index] = TDOFSavePayload(savedGame, data);
                }

                dispatch_group_leave(group);
            });
        }];
    }];

    dispatch_group_notify(group, dispatch_get_main_queue(), ^{
        NSMutableArray<NSDictionary<NSString *, id> *> *loaded = [NSMutableArray array];
        for (id item in slots)
        {
            if ([item isKindOfClass:NSDictionary.class])
            {
                [loaded addObject:item];
            }
        }

        completion([loaded copy], [errors copy]);
    });
}

static NSString *TDOFCombinedErrors(NSArray<NSString *> *errors)
{
    return errors.count > 0 ? [errors componentsJoinedByString:@" | "] : @"";
}

static BOOL TDOFPresentAuthenticationViewController(UIViewController *authenticationViewController)
{
    UIViewController *presenter = UnityGetGLViewController();
    while (presenter.presentedViewController != nil)
    {
        presenter = presenter.presentedViewController;
    }

    if (presenter == nil)
    {
        return NO;
    }

    if (presenter == authenticationViewController ||
        authenticationViewController.presentingViewController != nil)
    {
        return YES;
    }

    [presenter presentViewController:authenticationViewController animated:YES completion:nil];
    return YES;
}

static void TDOFConfigureAccessPoint(void)
{
    if (@available(iOS 14.0, *))
    {
        [GKAccessPoint shared].active = YES;
    }
}

@implementation TDOFSavedGameListener

- (void)player:(GKPlayer *)player didModifySavedGame:(GKSavedGame *)savedGame
{
    TDOFRunOnMain(^{
        if (savedGame == nil)
        {
            TDOFSendEnvelope(@"modified", 0, NO, @"GameKit reported an empty modified save", @[]);
            return;
        }

        TDOFLoadSavedGames(@[savedGame], ^(
            NSArray<NSDictionary<NSString *, id> *> *saves,
            NSArray<NSString *> *errors) {
            TDOFSendEnvelope(
                @"modified",
                0,
                errors.count == 0,
                TDOFCombinedErrors(errors),
                saves);
        });
    });
}

- (void)player:(GKPlayer *)player hasConflictingSavedGames:(NSArray<GKSavedGame *> *)savedGames
{
    TDOFRunOnMain(^{
        if (gConflictGroups == nil)
        {
            gConflictGroups = [NSMutableDictionary dictionary];
        }

        NSMutableDictionary<NSString *, NSMutableArray<GKSavedGame *> *> *groups =
            [NSMutableDictionary dictionary];
        for (GKSavedGame *savedGame in savedGames ?: @[])
        {
            NSString *name = savedGame.name;
            if (name.length == 0)
            {
                continue;
            }

            NSMutableArray<GKSavedGame *> *group = groups[name];
            if (group == nil)
            {
                group = [NSMutableArray array];
                groups[name] = group;
            }

            [group addObject:savedGame];
        }

        [groups enumerateKeysAndObjectsUsingBlock:^(
            NSString *name,
            NSMutableArray<GKSavedGame *> *group,
            BOOL *stop) {
            gConflictGroups[name] = [group copy];
        }];

        if (savedGames.count == 0)
        {
            TDOFSendEnvelope(@"conflict", 0, NO, @"GameKit reported an empty conflict group", @[]);
            return;
        }

        TDOFLoadSavedGames(savedGames, ^(
            NSArray<NSDictionary<NSString *, id> *> *saves,
            NSArray<NSString *> *errors) {
            TDOFSendEnvelope(
                @"conflict",
                0,
                errors.count == 0,
                TDOFCombinedErrors(errors),
                saves);
        });
    });
}

@end

#define TDOF_EXPORT extern "C" __attribute__((visibility("default")))

TDOF_EXPORT void TDOF_CloudInitialize(const char *receiverName)
{
    NSString *receiver = TDOFStringFromUTF8(receiverName);
    TDOFRunOnMain(^{
        if (receiver.length == 0)
        {
            TDOFSendEnvelope(@"initialize", 0, NO, @"Receiver name is null, empty, or invalid UTF-8", @[]);
            return;
        }

        gReceiverName = [receiver copy];
        if (gConflictGroups == nil)
        {
            gConflictGroups = [NSMutableDictionary dictionary];
        }

        GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
        if (gSavedGameListener == nil)
        {
            gSavedGameListener = [[TDOFSavedGameListener alloc] init];
            [localPlayer registerListener:gSavedGameListener];
        }

        TDOFSendEnvelope(@"initialize", 0, YES, @"", @[]);
    });
}

TDOF_EXPORT void TDOF_GameCenterAuthenticate(int requestId)
{
    TDOFRunOnMain(^{
        if (!TDOFRequireInitializedBridge(@"authenticate", requestId))
        {
            return;
        }

        if (gAuthenticationInFlight)
        {
            TDOFSendEnvelope(
                @"authenticate",
                requestId,
                NO,
                @"A Game Center authentication request is already in progress",
                @[]);
            return;
        }

        gAuthenticationInFlight = YES;
        gAuthenticationRequestId = requestId;
        GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
        localPlayer.authenticateHandler = ^(UIViewController *viewController, NSError *error) {
            TDOFRunOnMain(^{
                if (!gAuthenticationInFlight || gAuthenticationRequestId != requestId)
                {
                    return;
                }

                if (viewController != nil)
                {
                    if (!TDOFPresentAuthenticationViewController(viewController))
                    {
                        gAuthenticationInFlight = NO;
                        TDOFSendEnvelope(
                            @"authenticate",
                            requestId,
                            NO,
                            @"Unable to present Game Center authentication",
                            @[]);
                    }
                    return;
                }

                gAuthenticationInFlight = NO;
                if (localPlayer.isAuthenticated)
                {
                    TDOFConfigureAccessPoint();
                    TDOFSendEnvelope(@"authenticate", requestId, YES, @"", @[]);
                    return;
                }

                NSString *message = error != nil
                    ? TDOFDescribeError(error)
                    : @"Game Center authentication did not complete";
                TDOFSendEnvelope(@"authenticate", requestId, NO, message, @[]);
            });
        };
    });
}

TDOF_EXPORT void TDOF_GameCenterSetAccessPointVisible(int visible)
{
    TDOFRunOnMain(^{
        if (@available(iOS 14.0, *))
        {
            [GKAccessPoint shared].active = visible != 0;
        }
    });
}

TDOF_EXPORT void TDOF_GameCenterReportScore(
    int requestId,
    const char *leaderboardId,
    int64_t score)
{
    NSString *identifier = TDOFStringFromUTF8(leaderboardId);
    TDOFRunOnMain(^{
        if (!TDOFIsValidIdentifier(identifier))
        {
            TDOFSendEnvelope(
                @"score",
                requestId,
                NO,
                @"Leaderboard ID is null, empty, or invalid UTF-8",
                @[]);
            return;
        }

        if (!TDOFRequireInitializedBridge(@"score", requestId) ||
            !TDOFRequireAuthenticatedPlayer(@"score", requestId))
        {
            return;
        }

        if (@available(iOS 14.0, *))
        {
            GKLocalPlayer *localPlayer = [GKLocalPlayer localPlayer];
            [GKLeaderboard submitScore:(NSInteger)score
                               context:0
                                player:localPlayer
                        leaderboardIDs:@[identifier]
                     completionHandler:^(NSError *error) {
                TDOFRunOnMain(^{
                    TDOFSendEnvelope(
                        @"score",
                        requestId,
                        error == nil,
                        TDOFDescribeError(error),
                        @[]);
                });
            }];
            return;
        }

        TDOFSendEnvelope(@"score", requestId, NO, @"Game Center score reporting requires iOS 14 or later", @[]);
    });
}

TDOF_EXPORT void TDOF_GameCenterReportAchievement(int requestId, const char *achievementId)
{
    NSString *identifier = TDOFStringFromUTF8(achievementId);
    TDOFRunOnMain(^{
        if (!TDOFIsValidIdentifier(identifier))
        {
            TDOFSendEnvelope(
                @"achievement",
                requestId,
                NO,
                @"Achievement ID is null, empty, or invalid UTF-8",
                @[]);
            return;
        }

        if (!TDOFRequireInitializedBridge(@"achievement", requestId) ||
            !TDOFRequireAuthenticatedPlayer(@"achievement", requestId))
        {
            return;
        }

        GKAchievement *achievement = [[GKAchievement alloc] initWithIdentifier:identifier];
        achievement.percentComplete = 100.0;
        achievement.showsCompletionBanner = YES;
        [GKAchievement reportAchievements:@[achievement] withCompletionHandler:^(NSError *error) {
            TDOFRunOnMain(^{
                TDOFSendEnvelope(
                    @"achievement",
                    requestId,
                    error == nil,
                    TDOFDescribeError(error),
                    @[]);
            });
        }];
    });
}

TDOF_EXPORT void TDOF_CloudFetch(int requestId, const char *saveName)
{
    NSString *requestedName = TDOFStringFromUTF8(saveName);
    TDOFRunOnMain(^{
        if (requestedName.length == 0)
        {
            TDOFSendEnvelope(@"fetch", requestId, NO, @"Save name is null, empty, or invalid UTF-8", @[]);
            return;
        }

        if (!TDOFRequireInitializedBridge(@"fetch", requestId) ||
            !TDOFRequireAuthenticatedPlayer(@"fetch", requestId))
        {
            return;
        }

        [[GKLocalPlayer localPlayer] fetchSavedGamesWithCompletionHandler:^(
            NSArray<GKSavedGame *> *savedGames,
            NSError *error) {
            TDOFRunOnMain(^{
                if (error != nil)
                {
                    TDOFSendEnvelope(@"fetch", requestId, NO, TDOFDescribeError(error), @[]);
                    return;
                }

                NSPredicate *matchingName = [NSPredicate predicateWithBlock:^BOOL(
                    GKSavedGame *savedGame,
                    NSDictionary<NSString *, id> *bindings) {
                    return [savedGame.name isEqualToString:requestedName];
                }];
                NSArray<GKSavedGame *> *matches = [savedGames filteredArrayUsingPredicate:matchingName];

                TDOFLoadSavedGames(matches, ^(
                    NSArray<NSDictionary<NSString *, id> *> *saves,
                    NSArray<NSString *> *errors) {
                    TDOFSendEnvelope(
                        @"fetch",
                        requestId,
                        errors.count == 0,
                        TDOFCombinedErrors(errors),
                        saves);
                });
            });
        }];
    });
}

TDOF_EXPORT void TDOF_CloudSave(int requestId, const char *saveName, const char *base64Data)
{
    NSString *requestedName = TDOFStringFromUTF8(saveName);
    NSString *encodedData = TDOFStringFromUTF8(base64Data);
    TDOFRunOnMain(^{
        if (requestedName.length == 0)
        {
            TDOFSendEnvelope(@"save", requestId, NO, @"Save name is null, empty, or invalid UTF-8", @[]);
            return;
        }

        if (encodedData == nil)
        {
            TDOFSendEnvelope(@"save", requestId, NO, @"Save data is null or invalid UTF-8", @[]);
            return;
        }

        NSData *data = [[NSData alloc] initWithBase64EncodedString:encodedData options:0];
        if (data == nil)
        {
            TDOFSendEnvelope(@"save", requestId, NO, @"Save data is not valid base64", @[]);
            return;
        }

        if (!TDOFRequireInitializedBridge(@"save", requestId) ||
            !TDOFRequireAuthenticatedPlayer(@"save", requestId))
        {
            return;
        }

        [[GKLocalPlayer localPlayer] saveGameData:data
                                        withName:requestedName
                               completionHandler:^(GKSavedGame *savedGame, NSError *error) {
            TDOFRunOnMain(^{
                if (error != nil)
                {
                    TDOFSendEnvelope(@"save", requestId, NO, TDOFDescribeError(error), @[]);
                    return;
                }

                if (savedGame == nil)
                {
                    TDOFSendEnvelope(@"save", requestId, NO, @"GameKit returned no saved game", @[]);
                    return;
                }

                TDOFSendEnvelope(@"save", requestId, YES, @"", @[TDOFSavePayload(savedGame, data)]);
            });
        }];
    });
}

TDOF_EXPORT void TDOF_CloudResolve(int requestId, const char *saveName, const char *base64Data)
{
    NSString *requestedName = TDOFStringFromUTF8(saveName);
    NSString *encodedData = TDOFStringFromUTF8(base64Data);
    TDOFRunOnMain(^{
        if (requestedName.length == 0)
        {
            TDOFSendEnvelope(@"resolve", requestId, NO, @"Save name is null, empty, or invalid UTF-8", @[]);
            return;
        }

        if (encodedData == nil)
        {
            TDOFSendEnvelope(@"resolve", requestId, NO, @"Resolved data is null or invalid UTF-8", @[]);
            return;
        }

        NSData *data = [[NSData alloc] initWithBase64EncodedString:encodedData options:0];
        if (data == nil)
        {
            TDOFSendEnvelope(@"resolve", requestId, NO, @"Resolved data is not valid base64", @[]);
            return;
        }

        if (!TDOFRequireInitializedBridge(@"resolve", requestId) ||
            !TDOFRequireAuthenticatedPlayer(@"resolve", requestId))
        {
            return;
        }

        NSArray<GKSavedGame *> *conflicts = [gConflictGroups[requestedName] copy];
        if (conflicts.count == 0)
        {
            TDOFSendEnvelope(@"resolve", requestId, NO, @"No retained conflict group exists for this save name", @[]);
            return;
        }

        [[GKLocalPlayer localPlayer] resolveConflictingSavedGames:conflicts
                                                        withData:data
                                               completionHandler:^(
            NSArray<GKSavedGame *> *savedGames,
            NSError *error) {
            TDOFRunOnMain(^{
                if (error != nil)
                {
                    TDOFSendEnvelope(@"resolve", requestId, NO, TDOFDescribeError(error), @[]);
                    return;
                }

                if ([gConflictGroups[requestedName] isEqualToArray:conflicts])
                {
                    [gConflictGroups removeObjectForKey:requestedName];
                }

                TDOFLoadSavedGames(savedGames ?: @[], ^(
                    NSArray<NSDictionary<NSString *, id> *> *saves,
                    NSArray<NSString *> *errors) {
                    TDOFSendEnvelope(
                        @"resolve",
                        requestId,
                        errors.count == 0,
                        TDOFCombinedErrors(errors),
                        saves);
                });
            });
        }];
    });
}
