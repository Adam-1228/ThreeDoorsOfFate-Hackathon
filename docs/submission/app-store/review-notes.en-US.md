# App Review Notes

Three Doors of Fate is a single-player card RPG with no in-app purchases or subscriptions.

## This submission

- Build under review: `1.0.3 (7)`.
- Version 1.0.3 adds Korean and English language options. Changing the language does not reset the current run or saved progress.
- The main menu, Settings, character and difficulty selection, three-door choices, combat HUD and log, rewards, shops, events, achievements, endings, and How to Play are available in both languages.
- In English mode, all 72 card names, effects, and full card images are in English. Existing Korean card data and images remain unchanged in Korean mode.
- The Game Center access point no longer overlaps the upper-right Settings button and is hidden in Settings and How to Play.
- The app uses only optional rewarded ads. There are no automatic, interstitial, or banner ads.
- The final archive omits `NSUserTrackingUsageDescription`, and `ITSAppUsesNonExemptEncryption` is `false`.

## Language review path

1. Launch the app and select the gear-shaped Settings button at the bottom right of the main screen.
2. Select Korean or English.
3. Select Start Game, choose a difficulty and character, then enter door selection and combat.
4. In English mode, card-preview names, rules text, full card images, and card names in the combat log are in English.
5. During play, use the upper-right Settings button to switch back to Korean.

## Login and saving

- The Apple Game Center system authentication screen may appear at launch.
- Game Center sign-in is used for iCloud save synchronization, achievements, and leaderboards.
- Sign-in is not required, so App Review `Sign-in required` is `No`.
- If sign-in is canceled or the device is offline, local saving and the full single-player game remain available.
- There is no separate developer account, username, or password, so no demo account is available or required.

## Rewarded ad review path

1. Launch the app and select Start Game.
2. Select a difficulty, then choose the Gambler, the Oracle, or the Exile.
3. On the character confirmation screen, select the optional rewarded-ad button at the bottom (`Watch an ad for an item` in English mode).
4. Canceling the ad, or an ad or reward failure, grants no relic and does not consume the daily successful-reward count.
5. Successful completion grants exactly one eligible undiscovered relic and increases that character's successful-reward count for the day by one.

Each character can receive at most three successful rewards per local calendar day. Easy uses only Easy relics; Normal uses undiscovered Easy and Normal relics; Hard uses undiscovered relics across all difficulties. If none remain eligible, the rewarded-ad option is hidden.

## Privacy choices

The app does not provide personalized ads or engage in cross-app tracking as defined by Apple. Before Google Mobile Ads SDK initialization, the publisher first-party identifier and personalized-ad processing are disabled. The app does not request ATT permission. Reviewers can reopen the Google UMP privacy choices screen from in-game Settings where required for the applicable region. Privacy choices and ad availability do not block gameplay or local saving.

## Additional information

- Support: https://adam-1228.github.io/three-doors-of-fate/support/
- Privacy Policy: https://adam-1228.github.io/three-doors-of-fate/privacy/
- Export compliance: The app does not implement its own non-exempt encryption, and `ITSAppUsesNonExemptEncryption` is `false`.
- Review contact: Use the actual contact details in the authenticated App Store Connect account and verify them immediately before submission.
