# Three Doors of Fate iOS 출시 기준선

확인일: 2026-08-16 (Asia/Seoul)

## 앱 식별

- 제품명: `Three Doors of Fate`
- 개발사 표시: `ADAM`
- iOS Bundle ID: `com.adam.threedoorsfate`
- 버전: `1.0.3`
- 출시 후보 빌드: `7`
- Unity: `6000.4.11f1`
- 최초 가격: 무료
- 기존 공개 앱: `1.0.1 (5)`가 App Store에 공개된 것을 사용자가 확인함
- 이번 제출의 실제 판매 가능 지역: 대한민국 1개 사용 가능, 174개 지역 사용 불가로 제출 직전 읽기 전용 재확인
- DSA 상태: App Store Connect에 거래자 선언이 표시되는 것을 읽기 전용으로 재확인했으며 이 작업에서는 변경하지 않음
- 출시 방식: 심사 승인 후 수동 출시; `1.0.3 (7)`은 2026-08-17 12:23:13 KST 출시 확정
- IAP·구독: 없음

빌드 `1.0.0 (4)`는 최종 앱 번들에 `GADApplicationIdentifier`가 누락되어 Google Mobile Ads 초기화 검증에서 시작 직후 중단됐다. 공개된 `1.0.1 (5)`에서 프로덕션 AdMob 설정과 업로드 전 검증을 복구했다. 이번 `1.0.3 (7)`은 그 릴리스 구성을 유지하면서 한국어/영어 언어 선택, 72장 영문 카드, 전체 UI 현지화와 Game Center 접근점/설정 버튼 겹침 개선을 추가한다.

## 이전 빌드 5 제출 기록

- 서명 Archive: `/private/tmp/tdof-ios-1.0.1-build5/ThreeDoorsOfFate-1.0.1-build5.xcarchive`
- Archive 검증값: `1.0.1 (5)`, `com.adam.threedoorsfate`
- 최종 앱 번들: 프로덕션 `GADApplicationIdentifier`, `GADUUnityVersion`, SKAdNetwork 항목 50개 확인
- 개인정보·수출 규정: `NSUserTrackingUsageDescription` 없음, `ITSAppUsesNonExemptEncryption=false`
- 코드 서명, arm64, 앱 dSYM UUID 일치, root privacy manifest 확인 완료
- App Store Connect 업로드: 2026-08-13 23:40:43 KST 성공
- Resolution Center 회신: 2026-08-13 23:55 KST 전송, 메시지 5개로 증가 및 초안 소멸 확인
- 심사 재제출: 2026-08-13 23:57 KST, `1.0.1 (5)` 및 제출 항목 10개 모두 `심사 대기 중`
- 비차단 경고: `UnityRuntime.framework` dSYM이 Archive에 없어 해당 프레임워크 내부 크래시의 심볼화가 제한될 수 있다.
- 실제 iPhone 설치·실행: 수행하지 않음. 사용자는 이번 업데이트에서 기기 설치 검증을 필수로 두지 않았다.

## 빌드 7 현재 상태

- 소스 버전: `1.0.3`, iOS build `7`, Bundle ID `com.adam.threedoorsfate`
- Unity 현지화 검증: 완료
- 영어 카드: PNG 72장, `.meta` 72개, 런타임 manifest 72개
- Python 소스/정적 계약: 100/100 통과 (영문 App Store 메타데이터·연령 등급 회귀 검사 포함)
- 선택 UI SFX 계약: 6/6 통과
- Unity EditMode: 145/145 통과
- Unity PlayMode: 2/2 통과
- 영문 런타임 캡처: 5장, 플레이 방법 다중 해상도 캡처: 15장
- iOS Support, Xcode 26.6, iPhoneOS 26.5 SDK, CocoaPods 1.16.2 확인
- App Store 배포 프로파일: `com.adam.threedoorsfate`, 2027-07-17까지 유효
- 빌드 전 가용 공간: 프로젝트 전용 과거 Xcode Build 캐시 정리 후 약 16 GiB
- 사용자 재승인 후 Unity iOS 내보내기와 Xcode 무서명 device compile 완료: `BUILD SUCCEEDED`
- 서명 Archive: `/Users/apple/LocalProjects/Builds/iOS/ThreeDoorsOfFate.xcarchive` (약 1.7 GiB)
- Archive 결과: `ARCHIVE SUCCEEDED`, `com.adam.threedoorsfate`, `1.0.3 (7)`, arm64와 코드 서명 검증 통과
- 최종 앱 번들: 프로덕션 `GADApplicationIdentifier` 일치, `NSUserTrackingUsageDescription` 없음, `ITSAppUsesNonExemptEncryption=false`, SKAdNetwork 50개
- 권한: Game Center와 iCloud `CloudDocuments`, `iCloud.com.adam.threedoorsfate` 확인
- 개인정보 매니페스트: 앱·UnityFramework·UnityRuntime·Google Mobile Ads·UMP 위치에서 5개 확인
- App Store Connect 업로드: 2026-08-16 01:58:40 KST `Upload succeeded`, `Uploaded package is processing`, `EXPORT SUCCEEDED`
- 배포 내보내기 로그에서 `iOS Team Store Provisioning Profile: com.adam.threedoorsfate` 선택 확인
- 비차단 경고: `UnityRuntime.framework` dSYM 누락으로 해당 프레임워크 내부 크래시 심볼화가 제한될 수 있다.
- 실제 iPhone 설치·실행: 수행하지 않음. 사용자는 이번 업데이트에서 기기 설치 검증을 필수로 두지 않았다.
- App Store Connect 빌드 처리 완료 후 `1.0.3 (7)`만 버전 페이지에 선택·저장·재확인했다.
- 영어(미국) 이름 `Three Doors of Fate`, 부제 `Dark Fantasy Card RPG`, 버전 메타데이터 8개 필드와 한/영 새로운 기능을 저장 후 원문과 재대조했다.
- 영문 App Review 메모 3,663자를 저장 후 원문과 재대조했고, 로그인 필수는 `No`, 기존 심사 연락처는 변경하지 않았다.
- 과거 반려 조건 재확인: `Simulated Gambling=None`, App Privacy의 추적 표시 0건.
- 심사 제출: 2026-08-16 12:46 KST, iOS `1.0.3`, 제출 항목 1개.
- 제출 후 상태: `심사 대기 중`; 최종 DOM 재확인 2026-08-16 12:47:03 KST.
- Resolution Center 회신: 빌드 7 관련 새 메시지가 없어 전송하지 않음.
- 심사 승인 후 수동 출시: 2026-08-17 12:23:13.118 KST, 대한민국 1개 지역에 대해 `이 버전 출시` 확정.
- 출시 후 App Store Connect 상태: `1.0.3 배포 준비됨` (`Ready for Distribution`), 빌드 행 `7 / 1.0.3`, 출시 버튼 없음; 2026-08-17 12:24:47.439 KST 최종 DOM 재확인.
- 승인된 한국어·영어 새로운 기능 문구는 변경하지 않았으며, 지역·가격·DSA·개인정보·연령 등급·스크린샷·점진적 출시·다른 버전도 변경하지 않음.

## 광고 동작

- 사용자가 캐릭터 확정 화면에서 직접 선택하는 보상형 광고만 사용한다.
- 광고 보상이 실제로 완료되고 미발견 유물 지급이 성공한 경우에만 횟수를 차감한다.
- 한 캐릭터당 로컬 달력 기준 하루 3회다.
- Easy에서는 Easy 유물, Normal에서는 Easy·Normal 유물, Hard에서는 모든 난이도 유물이 후보가 된다.
- 이미 발견한 유물과 현재 난이도에서 허용되지 않는 유물은 후보에서 제외한다.
- 받을 수 있는 미발견 유물이 없거나 일일 횟수가 끝나면 광고를 실행할 수 없다.
- 전면 광고, 자동 광고, 배너 광고, 광고 제거 결제는 첫 버전에 없다.
- 개발 빌드는 Google 테스트 광고 값을 사용할 수 있지만, 출시 Archive는 운영 값만 허용하고 Google 샘플 값이면 빌드를 실패시킨다.

근거 소스: `AdsReleaseConfiguration.cs`, `RewardedRelicPolicy.cs`, `RewardedRelicDailyLimitStore.cs`, `ThreeDoorsGameController.RewardedAds.cs`, `MobileAdsService.cs`.

## 로그인과 저장

- 앱 시작 시 iOS 네이티브 GameKit 인증을 요청한다.
- Game Center 인증 성공 후 `GKSavedGame` 이름 `three-doors-progress-v1`로 클라우드 진행을 동기화한다.
- Game Center 인증 성공 후 엔드리스 점수와 완료 업적을 보고한다.
- Game Center 로그인 실패·취소·오프라인·iCloud 오류가 있어도 로컬 `PlayerPrefs` 진행은 유지되고 플레이를 막지 않는다.
- 클라우드 오류 또는 충돌 처리 중 로컬 저장을 삭제하지 않는다.
- 따라서 로그인은 클라우드 저장에 필요하지만 로컬 저장에는 필수가 아니다.

## Game Center 구성값

- 리더보드: `com.adam.threedoorsfate.leaderboard.endless`
- 업적: `com.adam.threedoorsfate.achievement.hard_unlocked`
- 업적: `com.adam.threedoorsfate.achievement.true_ending.gambler`
- 업적: `com.adam.threedoorsfate.achievement.true_ending.oracle`
- 업적: `com.adam.threedoorsfate.achievement.true_ending.exile`
- 업적: `com.adam.threedoorsfate.achievement.abyss_collector`
- 업적: `com.adam.threedoorsfate.achievement.build.gambler_high_roll`
- 업적: `com.adam.threedoorsfate.achievement.build.oracle_rift_engine`
- 업적: `com.adam.threedoorsfate.achievement.build.exile_last_oath`
- iCloud 컨테이너: `iCloud.com.adam.threedoorsfate`
- 저장 슬롯: `three-doors-progress-v1`

이 식별자는 App Store Connect의 실제 제품 앱에서 동일하게 생성·활성화되어야 한다.

## 제출 범위와 중단 조건

- DSA 판매자 확인 상태와 현재 판매 가능 지역은 App Store Connect에서 읽기 전용으로 먼저 확인한다.
- EU 지역 확대는 DSA 상태가 승인된 것으로 직접 확인된 경우에만 진행한다. 확인되지 않으면 기존 지역을 추정 변경하지 않는다.
- 중국 본토와 베트남은 별도 사용자 승인 및 필수 규제 정보 없이 추가하지 않는다.
- 대한민국 관련 사업자·게임 등록이 완료됐다고 이 문서에서 주장하지 않는다.
- Apple이 한국 공개를 위해 GRAC 번호, 사업자 정보 또는 별도 법적 서류를 필수 입력으로 요구하면 해당 화면에서 제출 또는 공개를 중단한다.
- Apple 2FA, CAPTCHA, 실제 연락처·법적 정보는 계정 소유자가 직접 확인한다. 값을 추측하지 않는다.
- 운영 AdMob 식별자, Apple Team ID와 계정 개인정보는 이 문서나 검증 로그에 기록하지 않는다.

## 기준선 검증

- Python 소스/정적 계약: 100개 통과, 0개 실패 (심사 제출 후 2026-08-16 12:49 KST 최종 재실행)
- 선택 UI SFX 계약: 6개 통과, 0개 실패
- Unity EditMode 전체 검사: 145개 통과, 0개 실패
- Unity PlayMode 전체 검사: 2개 통과, 0개 실패
- 세부 증거: `/Users/apple/Documents/game/ThreeDoorsOfFate-1.0.3-evidence/verification-summary-20260815.md`
- Git 상태: 이 복원 프로젝트에는 `.git` 저장소가 없다.
- 최종 iOS Archive 기준선과 해시는 Archive 생성 후 별도 기록한다.
- 최종 iOS Archive 검증: `1.0.3 (7)`, 번들 ID, 프로덕션 AdMob, ATT 키 부재, 수출 규정, Game Center/iCloud entitlement, privacy manifest 통과
- 심사 제출 후 Archive 읽기 전용 재검증: 코드 서명 유효, `ITSAppUsesNonExemptEncryption=false`, ATT 설명 키 부재, SKAdNetwork 50개 (2026-08-16 12:49 KST)
- App Store Connect 전송: 2026-08-16 01:58:40 KST 업로드 성공.
- App Store Connect 최종 제출: `1.0.3 (7)` 및 제출 항목 1개, 2026-08-16 12:46 KST 제출; 12:47:03 KST `심사 대기 중` 재확인.
- App Store Connect 수동 출시: 2026-08-17 12:23:13.118 KST 확정; 12:24:47.439 KST `1.0.3 배포 준비됨` 재확인.
