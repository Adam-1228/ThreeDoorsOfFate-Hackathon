# App Review Notes — Version 1.3.0

## Build under review

- Build under review: `1.3.0 (13000)`.
- Bundle ID: `com.adam.threedoorsfate`.
- No account is required. The complete single-player game remains usable if the player declines Game Center authentication or is offline.

## Changes in this build

- Boss attack intent now displays the same final damage value used when the attack resolves, including the existing low-Luck boss modifier.
- Two- to four-card combat and shop offers guarantee an eligible Attack when one is available.
- Treasure Gold is awarded automatically, while the offered card now has explicit Take Card and Skip Card actions.
- Easy and Normal boss combats can replace one non-Attack in a full five-card hand with an Attack from the draw pile once per combat. Hard mode and non-boss combats are unchanged.
- The unreleased `Fifty Fates` achievement was replaced by the hidden `What Did Rerolling Change?` achievement. It completes only after three consecutive explicit rerolls produce the same Luck result during one battle; turn-start rolls do not count.

## Game Center and retained behavior

- The release contains 20 achievements totaling 1,000 points. The review submission contains 12 new Game Center achievements; the other 8 are already live.
- Settings, Progress Log, local saves, iCloud synchronization after Game Center sign-in, and Endless leaderboards retain their existing behavior.
- Progress is always saved locally first. Declining Game Center does not block gameplay.

## Ads, privacy, and release

- Ads remain optional rewarded ads selected by the player, limited to successful relic rewards. There are no automatic, interstitial, or banner ads and no in-app purchases or subscriptions.
- The app uses non-personalized ad requests, disables publisher first-party identification before SDK initialization, and does not request ATT permission.
- `ITSAppUsesNonExemptEncryption` is false. The app uses only platform-provided encryption for Game Center and iCloud transport.
- This version is configured for automatic release immediately after approval, without phased release.
