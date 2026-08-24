# Three Doors of Fate — App Store 재제출 인계

## 목표

App Store Connect 앱 `Three Doors of Fate`의 빌드 `1.0.0 (4)`를 선택하고, 비추적 개인정보 공개와 심사 메모를 반영한 뒤 Apple Resolution Center에 회신하고 심사에 재제출한다.

## 식별자

- App Store Connect 앱 ID: `6798086296`
- 번들 ID: `com.adam.threedoorsfate`
- 제출 ID: `9ed27979-e9ad-4e2f-b52b-12de5bc46b33`
- 기존 거절 빌드: `1.0.0 (3)`
- 새 심사 대상 빌드: `1.0.0 (4)`
- Chrome 프로필: `Profile 1`

## 현재 확정된 빌드 증거

- 업로드 로그: `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon/Builds/Logs/app-store-release-att-20260812/testflight-upload-build4.log`
- 로그에 `Progress 100%: Upload succeeded.`와 `TESTFLIGHT_UPLOAD_SUCCEEDED`가 기록되어 있다.
- App Store Connect에서 확인된 빌드 4 내부 ID: `357aaae9-0da2-46a4-bc3f-f584bb3ff5c8`
- 앱 경로: `/private/tmp/tdof-att-review/release-build-4/ThreeDoorsOfFate-correct-team.xcarchive/Products/Applications/ThreeDoorsofFate.app`
- `CFBundleShortVersionString`은 `1.0.0`, `CFBundleVersion`은 `4`이다.
- `NSUserTrackingUsageDescription`은 없다.
- 앱 서명 검증은 통과했다.

## Apple 지적과 수정 설명

빌드 3에는 `NSUserTrackingUsageDescription`이 있었지만 ATT 권한 요청이 없어 선언과 동작이 불일치했다. 앱은 교차 앱 추적을 하지 않으므로 빌드 4에서 해당 설명 키를 제거했고 ATT API와 IDFA를 사용하지 않는다. Google Mobile Ads 초기화 전에 퍼블리셔 1자 식별자와 광고 개인화를 비활성화하고 보상형 광고 요청은 비개인화 처리한다.

## 로컬 원문

- App Review 메모: `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/review-notes.ko-KR.md`
- Apple 회신문 한국어·영어: `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/review-response-2026-08-12.ko-KR.md`
- 개인정보 공개 근거: `/Users/apple/Documents/game/ThreeDoorsOfFate-Hackathon/docs/submission/app-store/privacy-data-use.ko-KR.md`

## 승인된 순차 작업

사용자는 2026-08-12에 수정 및 재제출 완료까지 진행하도록 명시적으로 승인했다. 다만 브라우저 도구가 외부 상태 변경 직전 별도 확인을 요구하면 그 안전 절차를 따른다.

1. macOS 메모리와 고부하 프로세스를 확인한다. Unity/Xcode/기기 배포가 실행 중이거나 시스템 여유 메모리가 25% 미만이면 Chrome 제어를 시작하지 않고 증거를 보고한다.
2. 짧은 작업의 세션 기록이 250 MiB 미만임을 확인한 뒤에만 Chrome Profile 1의 로그인 상태를 사용한다.
3. App Store Connect의 앱 `6798086296`, iOS 버전 `1.0.0` 화면에서 기존 빌드 3 대신 처리 완료된 빌드 4를 선택하고 저장한다.
4. 앱 개인정보 보호 화면에서 SDK가 수집할 수 있는 데이터 범주와 목적 공개는 유지하되 `Data Used to Track You` 또는 각 데이터 범주의 `Used for Tracking` 선언을 모두 해제하고 게시한다.
5. App Review 메모를 `review-notes.ko-KR.md` 내용과 일치하도록 반영한다. 앱은 로그인 없이도 사용 가능하므로 `Sign-in required`는 `No`를 유지한다.
6. Resolution Center에서 `review-response-2026-08-12.ko-KR.md`의 영어 회신을 보내 빌드 4로 심사를 계속해 달라고 알린다.
7. 빌드 4를 심사에 재제출한다.
8. 최종 상태가 `Waiting for Review`, `심사 대기 중` 또는 그 이후의 유효한 심사 상태로 바뀌었는지 확인하고, 확인 시각과 근거를 보고한다.

## 중단 조건

- 빌드 4가 처리 완료 상태가 아니거나 선택 목록에 보이지 않는다.
- 개인정보 공개에서 추적 선언을 해제할 권한이 없다.
- 새 계약, 세금, 은행, 암호화, 수출 규정 또는 광고 네트워크 질문이 나타나 기존 승인 범위를 벗어난다.
- Apple 화면의 앱 ID·번들 ID·버전이 위 식별자와 다르다.
- 제출 전 필수 항목이 새로 누락됐으며 사실을 로컬 근거로 확정할 수 없다.
- 브라우저 제어 런타임이 CPU 80% 이상을 10초 넘게 유지하거나 RSS 750 MiB 이상이 된다.

중단 조건이 발생하면 추정으로 입력하거나 다른 설정을 바꾸지 말고 첫 차단 증거와 다음 한 가지 사용자 조치만 보고한다.

## 명시적 비범위

- Unity를 실행하지 않는다.
- Xcode 빌드나 새 바이너리 업로드를 하지 않는다.
- iPhone 설치를 하지 않는다.
- 가격, 배포 국가, 수동·자동 출시 방식을 변경하지 않는다.
- 앱 삭제, 제출 취소 또는 기존 빌드 삭제를 하지 않는다.
