# App Review Notes — Version 1.2.0

Three Doors of Fate is a solo card RPG. No account is required, and there are no in-app purchases or subscriptions.

## This submission

- Build under review: `1.2.0 (12000)`.
- The in-game gallery now contains 20 achievements across two pages of ten slots.
- Undiscovered achievements deliberately hide their artwork, title, and condition. After an achievement is earned, its artwork and short earned description become selectable in one shared detail panel.
- This release adds 12 new Game Center achievements. Together with the eight existing achievements, the catalog totals 20 achievements and 1,000 points.
- The character confirmation and class-information screens now keep a Settings button available. Settings includes the existing Return to Title action.
- Progress-log text has a wider horizontal safe inset so it does not touch the ornamental frame.
- Shop relic artwork is clipped inside a safe viewport beneath one existing frame overlay.
- Sound effects remain disabled; the existing menu, normal-room, combat, and boss background music is unchanged.
- Saved-progress keys, privacy behavior, optional rewarded-ad behavior, territories, price, and manual release settings are unchanged.

## Review path for modified UI

1. Launch the app. No account is required.
2. From the main menu, open Achievements. Confirm that each page contains ten relic-style slots and that undiscovered slots show only `Undiscovered`.
3. Return to the main menu, start a game, choose a difficulty, and select any character.
4. On the character confirmation screen, open the fixed Settings button at the upper right. The existing Return to Title action is available there.
5. Start the run and inspect Progress Log while choosing among the three doors; text remains within the ornamental border.
6. If a relic is offered in a shop, its art is clipped below one frame and does not overlap the surrounding UI.

The new achievements are milestone-based and complete through normal play. They cover class mechanics, combat thresholds, deck and relic combinations, build upgrades, Endless progress, and difficult-run completion across all three classes. They do not require a separate developer account or demo credentials.

## Login, Game Center, and saving

- The Apple Game Center system authentication screen may appear at launch.
- Game Center sign-in is optional and is used only for iCloud save synchronization, achievements, and leaderboards.
- If sign-in is canceled or the device is offline, local saving and the full single-player game remain available.
- There is no separate developer account, username, password, or demo account.

## Rewarded ads and privacy

- The app uses only optional rewarded ads. There are no automatic, interstitial, or banner ads.
- The rewarded-ad path is unchanged: Start Game → choose a difficulty → choose a character → select `Watch an ad for an item` on the character confirmation screen.
- Canceling an ad, or an ad/reward failure, grants no relic and does not consume the daily successful-reward count.
- Before Google Mobile Ads SDK initialization, the publisher first-party identifier and personalized-ad processing are disabled.
- The app does not perform cross-app tracking and does not request ATT permission.
- Where applicable, reviewers can reopen Google UMP privacy choices from in-game Settings.
- Privacy choices and ad availability do not block gameplay or local saving.

## Additional information

- Support: https://adam-1228.github.io/three-doors-of-fate/support/
- Privacy Policy: https://adam-1228.github.io/three-doors-of-fate/privacy/
- Export compliance: the app does not implement its own non-exempt encryption, and `ITSAppUsesNonExemptEncryption` is `false`.
- Review contact: use the actual contact details already present in the authenticated App Store Connect account and verify them immediately before submission.
