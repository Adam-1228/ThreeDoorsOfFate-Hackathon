# App Review 회신 — Guideline 2.1(a)

- 제출 ID: `9ed27979-e9ad-4e2f-b52b-12de5bc46b33`
- 반려 빌드: `1.0.0 (4)`
- 새 빌드: `1.0.1 (5)`
- 검토 날짜: 2026-08-13
- 검토 기기: iPhone 17 Pro Max, iPad Air 11-inch (M3)

## 한국어

안녕하세요.

검토와 충돌 로그를 보내 주셔서 감사합니다. 첨부된 두 로그를 분석한 결과, 앱 시작 중 Google Mobile Ads SDK의 `GADApplicationVerifyPublisherInitializedCorrectly` 검증에서 `SIGABRT`가 발생한 것을 확인했습니다.

원인은 Unity 프로젝트에는 프로덕션 AdMob 앱 ID가 설정되어 있었지만, 빌드 1.0.0 (4)의 최종 앱 번들에 `GADApplicationIdentifier`가 포함되지 않은 것이었습니다.

새 빌드 1.0.1 (5)에서 다음과 같이 수정했습니다.

- 최종 앱 번들에 프로덕션 `GADApplicationIdentifier`를 포함했습니다.
- `GADUUnityVersion`과 Google Mobile Ads가 제공하는 SKAdNetwork 항목 50개를 포함했습니다.
- 업로드 전 최종 아카이브에서 이 값들을 검증하고, 누락되거나 소스 설정과 다르면 릴리스를 중단하도록 빌드 검증을 추가했습니다.
- `NSUserTrackingUsageDescription`은 계속 포함하지 않으며, ATT 권한을 요청하거나 IDFA를 사용하지 않습니다.

최종 아카이브의 버전 `1.0.1 (5)`, 광고 SDK 설정, 코드 서명과 dSYM 일치를 확인한 뒤 App Store Connect에 업로드했습니다.

빌드 1.0.1 (5)로 검토를 계속해 주시기 바랍니다.

감사합니다.

## English

Hello,

Thank you for your review and for providing the crash logs. We analyzed both attached logs and confirmed that the app aborted during launch in the Google Mobile Ads SDK check `GADApplicationVerifyPublisherInitializedCorrectly`.

The production AdMob app ID was configured in the Unity project, but `GADApplicationIdentifier` was missing from the final app bundle in build 1.0.0 (4).

We corrected this in build 1.0.1 (5):

- Added the production `GADApplicationIdentifier` to the final app bundle.
- Included `GADUUnityVersion` and all 50 SKAdNetwork entries supplied by Google Mobile Ads.
- Added a pre-upload release check that stops the release if these final-archive values are missing or do not match the source configuration.
- `NSUserTrackingUsageDescription` remains absent, and the app does not request ATT authorization or access the IDFA.

Before uploading to App Store Connect, we verified the final archive version `1.0.1 (5)`, the ads SDK configuration, the code signature, and the dSYM match.

Please continue the review using build 1.0.1 (5).

Thank you.
