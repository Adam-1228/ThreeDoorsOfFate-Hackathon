# App Store 1.0.1 재제출 결과

확인 기준: 2026-08-13 23:59:06 KST

## 제출 결과

- 앱: Three Doors of Fate (`6798086296`)
- 제출 ID: `9ed27979-e9ad-4e2f-b52b-12de5bc46b33`
- 제출 버전: `1.0.1 (5)`
- 업로드 성공: 2026-08-13 23:40:43 KST
- Resolution Center 회신 전송: 2026-08-13 23:55 KST
- 심사 재제출: 2026-08-13 23:57 KST
- 최종 App Store Connect 상태: 제출 전체와 앱 버전 항목 모두 `심사 대기 중`

## 원인과 개선

빌드 `1.0.0 (4)`의 두 충돌 로그는 앱 시작 중 Google Mobile Ads의 `GADApplicationVerifyPublisherInitializedCorrectly`에서 발생한 `SIGABRT`를 공통으로 가리켰다. 최종 앱 번들에 `GADApplicationIdentifier`가 누락된 것이 직접 원인이었다.

빌드 `1.0.1 (5)`는 최종 아카이브에 프로덕션 `GADApplicationIdentifier`, `GADUUnityVersion`, Google Mobile Ads SKAdNetwork 항목 50개를 포함한다. 릴리스 도구는 이 값이 없거나 소스 구성과 다르면 업로드 전에 실패한다.

## 재검증 증거

- Python 테스트: 54개 통과, 0개 실패
- 셸 스크립트 구문 및 제출 메타데이터 JSON 검증 통과
- 최종 아카이브: `1.0.1 (5)`, arm64, 코드 서명 유효
- `NSUserTrackingUsageDescription` 없음
- `ITSAppUsesNonExemptEncryption=false`
- 앱 실행 파일과 앱 dSYM UUID 일치
- App Privacy: 게시 상태이며 제품 페이지에 추적 사용 표시 없음
- 연령 등급 6단계: `가상 도박`은 `없음`, 현금성 `도박`은 `아니요`
- Resolution Center: 메시지 4개에서 5개로 증가했고 새 1.0.1 회신이 전송 메시지로 표시되며 기존 중복 초안이 사라짐

## 비차단 제한

- 페어링된 iPhone은 개발자 모드가 켜져 있으나 로컬 네트워크 터널이 끊겨 있어 이번 작업에서 실제 설치·첫 실행은 확인하지 못했다.
- 업로드는 성공했지만 `UnityRuntime.framework` dSYM 누락 경고가 남아 해당 프레임워크 내부의 향후 충돌 심볼화가 제한될 수 있다. 앱 실행 파일의 dSYM은 일치한다.
