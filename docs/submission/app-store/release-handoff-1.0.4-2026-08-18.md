# Three Doors of Fate 1.0.4 manual release handoff

Prepared: 2026-08-18 (Asia/Seoul)

## Completion record

- Released item: `Three Doors of Fate` iOS `1.0.4 (8)`
- Release scope: South Korea + EU 27 + United States, 29 countries or regions total
- Final release click: 2026-08-18 08:54:20.503 KST
- Authoritative ASC status: `1.0.4 배포 준비됨`
- ASC status verification: 2026-08-18 08:54:33.735 KST
- Manual release and `모든 사용자에게 즉시 업데이트 출시` were retained.
- DSA remained active, and no metadata, territory, pricing, privacy, age-rating, or unrelated setting was changed.
- The final read-only ASC recheck at 2026-08-18 08:56:05.893 KST still showed `iOS 앱 버전 1.0.4`, build row `8 / 1.0.4`, status `1.0.4 배포 준비됨`, and zero release buttons.
- A fresh public product-page read at 2026-08-18 08:56:20.329 KST loaded without error but still displayed version `1.0.3`; storefront propagation of 1.0.4 was therefore still pending, while the authoritative ASC release state remained successful.
- The new public-page tab was closed; the existing ASC tab remains on the final version-state page.

## Scope and authorization

The user reports that App Review passed and explicitly authorized releasing the approved update. This is a short Chrome-only task. Use Chrome Profile 1 and its existing App Store Connect login. Read `/Users/apple/.codex/AGENTS.md` and the complete `chrome:control-chrome` skill before acting. Reuse an existing Chrome runtime/binding; do not start Unity, Xcode, a simulator, a device install, or a second browser runtime.

Authorized external action: manually release exactly Three Doors of Fate iOS version `1.0.4`, build `1.0.4 (8)`, app ID `6798086296`, to the already configured territories. Browser action-time confirmation rules still apply.

Do not change metadata, screenshots, privacy, age rating, DSA/trader data, territories, pricing, tax/banking, agreements, phased release, pre-order state, Game Center, AdMob, or another version/build.

## Required order

1. Check memory and confirm no Unity/Xcode/high-load process is active. Treat this as a short session, use one Chrome runtime, and keep at most three task tabs.
2. Use only Chrome Profile 1. Confirm the ASC app ID is `6798086296` and the app name is `Three Doors of Fate`.
3. Read the iOS version page and confirm exact version `1.0.4`, selected build `1.0.4 (8)`, status `개발자 출시 대기 중` / `Pending Developer Release`, and manual release. Do not act on version 1.0.3, build 7, or another item.
4. Read availability and DSA only. Expected baseline: exactly 29 available territories (South Korea + EU27 + United States), DSA active. Do not modify either.
5. Confirm the active action is the manual `이 버전 출시` / `Release This Version` control. If the exact state or control differs, stop without guessing.
6. Immediately before the representational/public release click, report the exact version/build, territories, modal effect, and whether release is immediate or Apple indicates a propagation delay. Obtain any browser-policy confirmation required at that boundary.
7. Confirm the release only for `1.0.4 (8)`. Do not modify any accompanying option.
8. Re-read authoritative ASC DOM until the version no longer shows Pending Developer Release and record the exact resulting status and KST timestamp. Success requires `배포 준비됨` / `Ready for Distribution` or a clearly valid release-processing state.
9. Open the public App Store product page read-only and record the visible version if available. Public storefront propagation may lag; do not retry in a tight loop and do not treat a temporary older public version as a failed ASC release.

## Stop conditions

Stop at the first occurrence and report only the evidence plus one next user action:

- Chrome Profile 1 is logged out or blocked by 2FA/CAPTCHA;
- version/build differs from `1.0.4 (8)`;
- status is not Pending Developer Release or the manual release button is absent;
- territory count differs from 29 or DSA is not active;
- a new agreement, legal, tax, banking, territory, privacy, age-rating, or other unrelated decision appears;
- the confirmation dialog contains an unexpected item or effect;
- the release action returns an error or resource threshold is exceeded.

## Success report

Report: exact version/build released; final ASC status; click and verification timestamps in KST; 29 territories/manual-release baseline; public product-page version if visible; and confirmation that no unrelated setting changed.
