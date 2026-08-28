# App Review Notes — Version 1.4.0

## Build under review

- Build under review: `1.4.0 (14000)`.
- Bundle ID: `com.adam.threedoorsfate`.
- Status in this repository: `release candidate`; the owner approved upload and App Review submission on 2026-08-28.
- No account is required. The complete single-player game remains usable if the player declines Game Center authentication or is offline.

## Changes in this build

- Each class now starts from a deterministic 24-card deck and offers three starting choices, for nine class contracts total.
- Card use, reward selection, shop purchases, treasure rewards, and deck removal share one top-layer inspection panel and require an explicit confirmation.
- Stable checkpoints now support all four difficulties. Confirmed doors, encounters, events, shops, rewards, and run randomness restore without rerolling.
- Eight data-driven fate events, distinct behavior bindings for 20 standard enemies and four bosses, boss phases, and six risk/reward Abyss mutations add encounter variety.
- Fate History stores the latest ten completed runs locally and shows class, contract, difficulty, progress, outcome, rewards, and a comic epithet.

## Suggested review path

1. Start a run, select any class, review the three contract descriptions, and confirm one.
2. Open a card in combat or a shop and verify that inspection alone does not use or purchase it; press the explicit action button to commit.
3. Return to a door screen, leave and relaunch the app, then continue the run to verify the same pending choices are restored.
4. Complete or lose a run, return to the main menu, and open Fate History to inspect its local record.

## Compatibility, Game Center, and privacy

- Existing local and iCloud progression is retained. Version 1 checkpoints migrate without deleting the source progress when restoration fails.
- The app continues to use the existing 20 achievements totaling 1,000 points. This version adds no Game Center achievement IDs or points.
- Settings, Fate History, local saves, iCloud synchronization after optional Game Center sign-in, and the Endless leaderboard remain available.
- Ads remain optional rewarded ads selected by the player. There are no automatic, interstitial, or banner ads, and no in-app purchases or subscriptions.
- The app uses non-personalized ad requests, disables publisher first-party identification before SDK initialization, and does not request ATT permission.
- `ITSAppUsesNonExemptEncryption` is false. The app uses only platform-provided encryption for Game Center and iCloud transport.
- This version is configured for automatic release immediately after approval, without phased release.
