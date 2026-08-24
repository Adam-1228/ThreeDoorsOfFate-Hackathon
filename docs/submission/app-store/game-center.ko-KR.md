# Game Center 구성표

확인일: 2026-08-09

App Store Connect에서 앱 버전 `1.0.0`의 Game Center를 활성화하고 아래 구성요소를 같은 제출에 포함한다. TestFlight와 출시 빌드는 같은 Game Center 서버 환경을 사용하므로 처리 완료 빌드에서 먼저 검증한다.

## 리더보드

- Reference Name: `Three Doors Endless Record`
- Leaderboard ID: `com.adam.threedoorsfate.leaderboard.endless`
- Type: Classic
- Score Format: Integer
- Score Submission Type: Best Score
- Sort Order: High to Low
- Score Range: `0` ~ `2147483647`
- Default: Yes
- Korean Display Name: `끝없는 문의 기록`
- Korean Description: `한 번의 엔드리스 도전에서 도달한 최고 기록입니다.`
- Suffix: 단수·복수 모두 `점`

## 업적

각 업적은 한 번만 달성할 수 있고 100점이다. 총점은 800점으로 Apple의 업적당 100점·앱당 1000점 한도 안에 있으며, 후속 업데이트에 사용할 200점을 남긴다.

| Achievement ID | Reference / 표시 이름 | 달성 전 설명 | 달성 후 설명 | Hidden |
| --- | --- | --- | --- | --- |
| `com.adam.threedoorsfate.achievement.hard_unlocked` | Hard Unlocked / `심연으로 가는 문` | `어려움 난이도를 해금하세요.` | `어려움 난이도를 해금했습니다.` | No |
| `com.adam.threedoorsfate.achievement.true_ending.gambler` | Gambler True Ending / `도박사의 진실` | `도박사의 숨겨진 결말을 찾으세요.` | `도박사의 진엔딩을 발견했습니다.` | Yes |
| `com.adam.threedoorsfate.achievement.true_ending.oracle` | Oracle True Ending / `예언가의 진실` | `예언가의 숨겨진 결말을 찾으세요.` | `예언가의 진엔딩을 발견했습니다.` | Yes |
| `com.adam.threedoorsfate.achievement.true_ending.exile` | Exile True Ending / `추방자의 진실` | `추방자의 숨겨진 결말을 찾으세요.` | `추방자의 진엔딩을 발견했습니다.` | Yes |
| `com.adam.threedoorsfate.achievement.abyss_collector` | Abyss Collector / `심연의 수집가` | `한 캐릭터로 유물·축복·저주 30종을 모두 발견하세요.` | `한 운명이 심연에 숨은 모든 계약을 수집했습니다.` | No |
| `com.adam.threedoorsfate.achievement.build.gambler_high_roll` | Gambler High Roll / `운명을 건 판돈` | `도박사의 판돈 단검·판돈 방패·판 뒤집기를 한 덱에 모으세요.` | `모든 것을 건 판돈이 운명을 뒤집었습니다.` | No |
| `com.adam.threedoorsfate.achievement.build.oracle_rift_engine` | Oracle Rift Engine / `세 문의 예언` | `예언가의 별자리 절단·예견된 방벽·세 문의 징조를 한 덱에 모으세요.` | `세 문의 징조가 하나의 예언으로 이어졌습니다.` | No |
| `com.adam.threedoorsfate.achievement.build.exile_last_oath` | Exile Last Oath / `끊어진 맹세` | `추방자의 사슬 처형·추방의 맹세·낙인 정화를 한 덱에 모으세요.` | `끊어진 맹세가 마지막 심판으로 완성됐습니다.` | No |

코드의 Game Center 보고 목록과 게임 내 업적 화면은 위 8개를 사용하며 총점은 800점이다. `심연의 수집가`는 캐릭터별 발견 기록을 서로 합치지 않고, 한 캐릭터가 30종을 모두 발견했을 때만 달성한다.

## 이미지

Apple 제출용 이미지는 1024×1024 불투명 RGB PNG로 준비한다. 신규 4개 업적은 앱 아이콘의 흑석·금장·청록색 다크 판타지 팔레트를 기준으로 다시 생성한 아래 제출 전용 이미지를 사용한다.

- Abyss Collector: `Builds/AppStore/GameCenterAchievements/achievement_abyss_collector_submission_v2.png`
- Gambler High Roll: `Builds/AppStore/GameCenterAchievements/achievement_gambler_high_roll_submission_v2.png`
- Oracle Rift Engine: `Builds/AppStore/GameCenterAchievements/achievement_oracle_rift_engine_submission_v2.png`
- Exile Last Oath: `Builds/AppStore/GameCenterAchievements/achievement_exile_last_oath_submission_v2.png`

기존 True Ending 3개는 로컬화와 규격 검증이 끝난 `Builds/AppStore/GameCenterAchievements/achievement_existing_app_icon.png`를 공통 제출 이미지로 사용한다. `Hard Unlocked`는 App Store Connect에서 이미 심사 준비됨 상태인 기존 이미지를 유지한다. 게임 런타임에서 사용하는 `Assets/Resources/Achievements/achievement_*.png`는 변경하지 않는다.

## 저장과 entitlement

- iCloud container: `iCloud.com.adam.threedoorsfate`
- Saved game name: `three-doors-progress-v1`
- Required entitlements: Game Center, iCloud CloudDocuments, ubiquity container와 iCloud container 배열
- Game Center 로그인 실패·취소 시 로컬 저장을 유지한다.

## 제출 전 확인

- 리더보드와 8개 업적을 앱 버전 `1.0.0`에 연결한다.
- 업적·리더보드 상태를 Ready for Review로 만들고 앱 버전과 함께 제출한다.
- TestFlight에서 인증, 점수 보고, 어려움 해금 업적과 클라우드 저장을 확인한다.

## App Store Connect 제출 초안 상태

2026-08-09 기준 기존 iOS 제출 초안에 아래 10개 항목을 추가했다.

- iOS 앱 `1.0.0 (3)`
- Game Center 순위표 `Three Doors Endless Record`
- Game Center 목표 달성 8개: `Hard Unlocked`, `Abyss Collector`, `Gambler True Ending`, `Oracle True Ending`, `Exile True Ending`, `Gambler High Roll`, `Oracle Rift Engine`, `Exile Last Oath`

App Store Connect에서 `제출 준비된 항목(10개)`와 활성화된 `심사를 위해 제출` 버튼을 확인했다. 실제 심사 제출 버튼은 누르지 않았다.

공식 근거:

- https://developer.apple.com/help/app-store-connect/reference/game-center/leaderboards/
- https://developer.apple.com/help/app-store-connect/reference/game-center/achievements
- https://developer.apple.com/help/app-store-connect/configure-game-center/overview-of-testing-game-center
