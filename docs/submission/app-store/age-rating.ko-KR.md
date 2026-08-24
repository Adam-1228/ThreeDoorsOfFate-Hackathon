# App Store 연령 등급 응답 근거

확인일: 2026-08-16

아래 값은 현재 소스, 실제 iPhone 화면과 Apple의 2026년 연령 등급 정의를 대조한 초안이다. App Store Connect가 계산한 글로벌·대한민국 등급을 저장 전에 다시 확인한다.

## In-App Controls

- Parental Controls: No
- Age Assurance: No

## Capabilities

- Unrestricted Web Access: No
- User-Generated Content: No
- Messaging and Chat: No
- Social Media: No
- Social Media Disabled for Users Under 13: No
- Advertising: Yes — 사용자가 직접 선택하는 보상형 동영상 광고

## Mature Themes

- Profanity or Crude Humor: None
- Horror/Fear Themes: Frequent — 게임 전체가 어두운 동굴, 해골, 저주, 피와 죽음의 문구를 반복적으로 사용한다.
- Alcohol, Tobacco, or Drug Use or References: None

## Sexuality or Nudity

- Mature or Suggestive Themes: None
- Sexual Content or Nudity: None
- Graphic Sexual Content and Nudity: None

## Violence

- Cartoon or Fantasy Violence: Frequent — 카드 전투, 피해, 출혈, 저주와 보스 전투가 핵심 반복 플레이에 포함된다.
- Realistic Violence: Infrequent — 실사풍 인물·적 그림이 있지만 전투는 카드·수치·효과 중심이며 상세한 현실 상해를 묘사하지 않는다.
- Prolonged Graphic or Sadistic Realistic Violence: None
- Guns or Other Weapons: Frequent — 단검, 검, 방패와 처형·절단 명칭의 전투 카드가 반복해서 등장한다.

## Chance-Based Activities

- Gambling: No — 현실 화폐 또는 현실 화폐로 교환 가능한 재화를 베팅하지 않는다.
- Simulated Gambling: None — 게임에는 카지노 게임을 모사하거나 재화·아이템을 베팅해 우연한 결과로 승패를 정하는 기능이 없다. 도박사·판돈·행운이라는 판타지 표현과 전투 주사위의 무작위 판정은 덱 전투 규칙이며, 포커·슬롯·룰렛 같은 도박 활동을 플레이하지 않는다. 이전 `Infrequent` 응답은 개인 개발자 제출 제한을 일으켰고, Apple 심사에서 해소된 현재 App Store Connect 값인 `None`을 유지한다.
- Contests: Infrequent — Game Center 엔드리스 점수 리더보드가 있다.
- Loot Boxes: No — 광고 보상은 구매하는 가상 상자가 아니며 IAP·유료 무작위 아이템이 없다.

## 예상 결과와 중단 조건

- 예상 대한민국 등급: 12+ 범위. 빈번한 공포 테마와 비현실적 폭력·무기 응답을 기준으로 한 예상이며 최종 등급은 App Store Connect 계산값이 우선한다.
- Made for Kids: 선택하지 않음.
- Override to Higher Age Rating: Not Applicable.
- Apple이 대한민국 19+, Casino 하위 카테고리 17+, 또는 GRAC Rating Classification Number 필수 상태를 표시하면 심사 제출 또는 한국 공개를 중단한다.
- `Simulated Gambling`이 `None`이 아닌 값으로 바뀌어 있으면 저장·제출하지 않고 중단한다. 그 밖의 Sexual Content or Nudity, Alcohol/Tobacco/Drug Reference, Realistic Violence 응답도 현재 소스 근거 없이 변경하지 않는다.

공식 근거: https://developer.apple.com/help/app-store-connect/reference/app-information/age-ratings-values-and-definitions
