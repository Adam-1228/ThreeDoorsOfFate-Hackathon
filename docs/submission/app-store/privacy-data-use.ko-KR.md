# App Store 개인정보 공개 근거

확인일: 2026-08-12

이 문서는 App Store Connect의 개인정보 질문에 답하기 위한 근거다. 최종 답변은 제출할 `.xcarchive` 안의 모든 `PrivacyInfo.xcprivacy`, Google Mobile Ads·User Messaging Platform 버전과 실제 광고 설정을 다시 대조한 뒤 확정한다.

## 제품 데이터 흐름

### 앱 자체

- 캐릭터, 난이도, 카드, 유물, 설정, 점수와 광고 보상 사용 횟수는 기기의 `PlayerPrefs`에 로컬 저장된다.
- 개발자가 운영하는 별도 계정·분석·게임 저장 서버는 없다.
- 앱은 연락처, 사진, 마이크, 건강, 결제 정보를 요청하지 않는다.

### Game Center와 iCloud

- Game Center 인증 성공 후 게임 진행 스냅샷을 `GKSavedGame` 슬롯 `three-doors-progress-v1`로 저장한다.
- 엔드리스 점수와 완료 업적을 Game Center에 보고한다.
- 앱 브리지는 Game Center 별칭, 이메일 또는 Apple Account 자격 증명을 Unity에 전달하지 않는다.
- Apple 안내상 게임 저장 기능이 있는 앱은 `Gameplay Content` 공개를 검토해야 하므로, 클라우드 저장 진행 데이터는 `Gameplay Content / App Functionality / Linked to User`로 선언하는 보수적 초안을 사용한다.
- 로그인 취소·실패 시 클라우드 전송을 시작하지 않고 로컬 저장을 유지한다.

### Google Mobile Ads

앱은 Google Mobile Ads Unity 패키지 `11.2.0`과 사용자가 직접 선택하는 비추적 보상형 광고 한 종류를 사용한다. 광고 SDK 초기화 전에 퍼블리셔 1자 식별자를 비활성화하고 개인 맞춤 광고 처리를 사용하지 않도록 전역 요청 설정을 적용한다. Google 공식 iOS 데이터 공개를 기준으로 SDK가 처리할 수 있는 범주는 다음과 같다.

| App Store 데이터 범주 | 목적 초안 | 사용자 연결 | 추적 사용 초안 |
| --- | --- | --- | --- |
| Gameplay Content | App Functionality | Yes | No |
| Coarse Location | Third-Party Advertising, Developer's Advertising or Marketing, Analytics, App Functionality | Yes | No |
| Device ID | Third-Party Advertising, Developer's Advertising or Marketing, Analytics | Yes | No |
| Advertising Data | Third-Party Advertising, Developer's Advertising or Marketing, Analytics | Yes | No |
| Product Interaction | Third-Party Advertising, Developer's Advertising or Marketing, Analytics, App Functionality | Yes | No |
| Crash Data | Analytics | No | No |
| Performance Data | Third-Party Advertising, Developer's Advertising or Marketing, Analytics, App Functionality | No | No |
| Other Diagnostic Data | Third-Party Advertising, Developer's Advertising or Marketing, Analytics | No | No |

Google 문서가 설명하는 원천 정보는 IP 주소 기반 대략적 위치, 충돌 로그, 진단, 성능 데이터, 기기/광고 식별자, 광고 데이터와 앱·동영상 상호작용이다. 위 목적·연결 값은 App Store의 데이터 수집 공개에서 유지하지만, 이 앱은 해당 데이터를 다른 회사의 앱·웹사이트 데이터와 결합하는 교차 앱 추적에 사용하지 않는다. 선택 기능이나 SDK 버전이 바뀌면 이 표도 갱신한다.

Google Mobile Ads SDK 자체 Privacy Manifest에는 SDK가 지원할 수 있는 데이터 처리 범위가 포함될 수 있다. 앱 수준 정책은 `PublisherFirstPartyIdEnabled = false`, `PublisherPrivacyPersonalizationState = Disabled`, `NSPrivacyTracking = false`이며 App Store Connect의 `Data Used to Track You`에는 어떤 범주도 선언하지 않는다. SDK의 수집 범주 공개와 앱의 추적 여부는 별개로 정확히 답한다.

## 동의와 선택권

- 앱 설정에서 Google UMP 개인정보 옵션을 다시 열 수 있다.
- 앱은 맞춤형 광고와 교차 앱 추적을 사용하지 않으므로 `NSUserTrackingUsageDescription`과 ATT 권한 요청을 포함하지 않는다.
- 개인정보 선택과 광고 사용 여부는 게임과 로컬 저장을 차단하지 않는다.
- 운영 Archive와 App Store Connect에서 실제 추적 사용 여부가 불일치하지 않아야 한다.

## Archive 최종 대조 체크

- 루트 앱, GoogleMobileAds, UserMessagingPlatform, UnityFramework 및 포함된 모든 SDK의 privacy manifest 목록을 저장한다.
- 각 manifest의 `NSPrivacyTracking`, `NSPrivacyCollectedDataTypes`, `NSPrivacyAccessedAPITypes`를 추출한다.
- Google 샘플 광고 ID가 없는지 확인한다.
- ATT 문구가 실제 빌드에 없고 UMP 개인정보 옵션 진입은 유지되는지 확인한다.
- 위 표와 다른 SDK 선언이 하나라도 있으면 App Store 답변을 Archive 기준으로 수정한다.

## 공식 근거

- Apple App Privacy Details: https://developer.apple.com/app-store/app-privacy-details/
- Apple App Privacy reference: https://developer.apple.com/help/app-store-connect/reference/app-information/app-privacy
- Google Mobile Ads iOS data disclosure: https://developers.google.com/admob/ios/privacy/data-disclosure
