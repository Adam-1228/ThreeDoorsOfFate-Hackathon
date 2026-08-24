# App Review Notes — Version 1.0.4

Three Doors of Fate is a solo card RPG. No account is required, and there are no in-app purchases or subscriptions.

## This submission

- Build under review: `1.0.4 (8)`.
- The game remains fully playable in Korean and English, including all 72 card names, rules text, and localized full-card images.
- Version 1.0.4 completes the remaining English localization for card synergies and Game Over screens.
- Combat HUD information is separated into readable player vitals, run progress, and resource groups.
- Rest and Event choices now show current HP, Gold, and Debt plus the projected result before selection.
- How to Play includes an isolated hand-flow practice that demonstrates retaining unused cards, drawing only to the five-card hand limit, and not recycling discarded cards.
- Normal three-door choices avoid duplicates and always include at least one non-combat route. Required progression battles remain unchanged.
- Successful treasure rewards display a localized, read-only card preview before continuing.
- Achievements are shown four per page with readable progress values and page navigation.
- Game Over shows run context and provides Same Run, Class Select, and Main Menu actions.
- The gear-shaped Settings control uses a larger, separate icon area so its Korean and English labels remain readable.
- This update does not change saved-progress keys, Game Center identifiers, privacy behavior, rewarded-ad behavior, or distribution settings.

## Language and modified-surface review path

1. Launch the app. No account is required.
2. On the main menu, select the gear-shaped Settings control, then choose Korean or English.
3. Open How to Play and navigate through the guide to the interactive hand-flow practice.
4. Start a run, choose a difficulty and character, and review the three-door choices.
5. Enter combat to inspect the separated HUD groups and the localized card-synergy list.
6. Enter a Rest or Event room to inspect current and projected state information.
7. Obtain a treasure card to inspect its localized card preview.
8. Open Achievements from the main menu and use Previous/Next to review both pages.
9. End a run to inspect the localized Game Over summary and its three navigation actions.

Changing the language in Settings does not reset the current run or saved progress.

## Login and saving

- The Apple Game Center system authentication screen may appear at launch.
- Game Center sign-in is optional and is used only for iCloud save synchronization, achievements, and leaderboards.
- If sign-in is canceled or the device is offline, local saving and the full game remain available.
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
