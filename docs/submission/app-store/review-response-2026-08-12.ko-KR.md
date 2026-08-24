# App Review 회신 초안 — Guideline 2.1

- 제출 ID: `9ed27979-e9ad-4e2f-b52b-12de5bc46b33`
- 기존 검토 빌드: `1.0.0 (3)`
- 검토 날짜: 2026-08-12
- 검토 기기: iPad Air 11-inch (M3)

## 한국어

안녕하세요.

검토와 안내에 감사드립니다. 이 앱은 사용자를 다른 회사의 앱 또는 웹사이트 전반에서 추적하지 않습니다.

기존 빌드 1.0.0 (3)에는 `NSUserTrackingUsageDescription`이 포함되어 있었지만 실제 App Tracking Transparency 권한 요청 경로가 없었습니다. 이 선언 불일치를 수정한 빌드 1.0.0 (4)를 업로드했습니다.

새 빌드에서는 다음과 같이 변경했습니다.

- `NSUserTrackingUsageDescription`을 제거했습니다.
- App Tracking Transparency 권한 요청 API를 호출하지 않으며 IDFA를 사용하지 않습니다.
- Google Mobile Ads 초기화 전에 퍼블리셔 1자 식별자를 비활성화했습니다.
- 광고 요청을 비개인화 처리로 고정했습니다.
- App Store Connect의 앱 개인정보 보호 정보에서 추적 사용 선언을 제거하고, SDK가 수집할 수 있는 데이터 범주와 용도 공개는 유지했습니다.

광고는 사용자가 미발견 유물을 얻기 위해 직접 선택하는 보상형 광고만 제공됩니다. 광고를 보지 않아도 전체 게임과 로컬 저장을 사용할 수 있습니다.

수정된 빌드 1.0.0 (4)로 검토를 계속해 주시기 바랍니다.

감사합니다.

## English

Hello,

Thank you for your review and guidance. This app does not track users across apps or websites owned by other companies.

Build 1.0.0 (3) included `NSUserTrackingUsageDescription`, but it did not contain an App Tracking Transparency authorization request. We corrected this declaration mismatch in the newly uploaded build 1.0.0 (4).

The new build includes the following changes:

- Removed `NSUserTrackingUsageDescription`.
- Does not call the App Tracking Transparency authorization API or access the IDFA.
- Disables the Google Mobile Ads publisher first-party ID before SDK initialization.
- Applies non-personalized treatment to all ad requests.
- Removes tracking declarations from App Privacy in App Store Connect while continuing to disclose the data categories and purposes that the integrated SDK may collect.

The app only offers user-initiated rewarded ads for obtaining an undiscovered relic. The full game and local save functionality remain available without viewing ads.

Please continue the review using build 1.0.0 (4).

Thank you.
