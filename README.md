# Three Doors of Fate

세 개의 문 중 하나를 고르고, 카드와 주사위로 빚과 운명을 돌파하는 한국어 덱빌딩 로그라이크입니다.

[App Store에서 다운로드](https://apps.apple.com/kr/app/three-doors-of-fate/id6798086296) · [브라우저에서 바로 플레이](https://adam-1228.github.io/ThreeDoorsOfFate-Hackathon/) · [최신 릴리스 보기](https://github.com/Adam-1228/ThreeDoorsOfFate-Hackathon/releases/latest) · [변경 이력](CHANGELOG.md)

![직업 선택](docs/screenshots/class-selection.png)

## v1.4.0 release candidate

현재 개발 브랜치는 대형 업데이트 **운명 재구성**의 v1.4.0 release candidate입니다. 직업별 24장 기본 덱과 총 9개 시작 계약, 모든 난이도의 결정적 이어하기, 명시적 카드 확정, 8개 운명 사건, 적별 행동 패턴과 보스 단계, 6개 심연 변칙, 최근 10회 운명 기록을 포함합니다.

이 후보는 아직 App Store에 제출하거나 공개한 버전이 아닙니다. `1.3.0`의 공개 후 안정성을 충분히 관찰하고 별도 제출 승인을 받은 뒤 빌드·업로드·심사를 진행합니다. 현재 App Store 링크는 공개 중인 정식 버전을 가리킵니다.

## 핵심 특징

- 전투·정예·상점·보물·이벤트·휴식·저주를 품은 세 개의 문
- 공격·방어·특수·저주 카드와 행운 주사위를 이용한 턴제 전투
- 도박사·점술가·추방자 세 직업과 전용 카드·특성
- 카드 조합, 유물·축복·저주 아이템, 난이도별 보스와 무한 기록 모드
- 모든 난이도의 안정 지점 체크포인트 저장과 결정적 이어하기
- 직업별 24장 기본 덱, 9개 시작 계약과 최근 10회 운명 기록
- 한국어·영어를 지원하는 2D UI와 장면별 BGM

![세 개의 문](docs/screenshots/door-selection.png)

![카드 전투](docs/screenshots/combat.png)

## 조작

- 마우스로 버튼, 직업, 문, 카드를 선택합니다.
- 카드를 클릭해 사용하고 `턴 종료` 버튼으로 적의 턴을 진행합니다.
- 카드·문·직업 초상에 포인터를 올리면 추가 정보와 시각 반응을 확인할 수 있습니다.
- 설정 화면에서 음악과 효과음 음량을 조절할 수 있습니다.

## App Store와 브라우저에서 플레이

[App Store](https://apps.apple.com/kr/app/three-doors-of-fate/id6798086296)에서 iPhone용 정식 버전을 다운로드할 수 있습니다.

[GitHub Pages 플레이 링크](https://adam-1228.github.io/ThreeDoorsOfFate-Hackathon/)에서는 설치나 로그인 없이 v1.1.2 WebGL 빌드를 실행할 수 있습니다. 첫 실행은 대용량 데이터를 내려받아 압축 해제하므로 네트워크와 기기 성능에 따라 몇 분 걸릴 수 있습니다.

오프라인 보관용 파일은 [v1.1.2 릴리스](https://github.com/Adam-1228/ThreeDoorsOfFate-Hackathon/releases/tag/v1.1.2)의 `ThreeDoorsOfFate-WebGL-v1.1.2.zip`입니다. 내려받아 압축을 푼 뒤, `index.html`이 있는 폴더를 로컬 HTTP 서버로 제공합니다.

```powershell
python -m http.server 8000
```

브라우저에서 <http://localhost:8000>을 엽니다. 브라우저 보안 정책 때문에 `index.html`을 파일 탐색기에서 직접 여는 방식은 지원하지 않습니다.

브라우저 빌드는 GitHub Release 자산을 GitHub Actions가 검증·추출해 Pages에 배포합니다. 대용량 `WebGL.data.unityweb`은 저장소 커밋에 포함하지 않습니다.

## 소스 열기와 빌드

필수 도구:

- Unity `6000.4.11f1`
- Git LFS 3.x
- WebGL Build Support 또는 원하는 대상 플랫폼의 Unity 모듈

```powershell
git lfs install
git clone https://github.com/Adam-1228/ThreeDoorsOfFate-Hackathon.git
cd ThreeDoorsOfFate-Hackathon
git lfs pull
```

Unity Hub에서 저장소 루트를 열고 `Assets/Scenes/ThreeDoorsPlayable.unity`를 실행합니다.

WebGL 배치 빌드:

```powershell
powershell -ExecutionPolicy Bypass -File tools/build_webgl.ps1
```

Windows 배치 빌드:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.4.11f1\Editor\Unity.exe' `
  -batchmode -nographics -quit -projectPath . `
  -executeMethod ThreeDoorsOfFate.Editor.PlayableGameBuilder.BuildWindowsPlayable
```

## 검증 상태

- 기존 Windows 플레이어 빌드: 성공
- Unity EditMode: 147/147 통과
- Python 저장소 계약 테스트: 10/10 통과 (Apple 플랫폼 7개, 밸런스 3개)
- v1.1.2 WebGL 릴리스 자산 및 GitHub Pages 배포: 공개 중
- 로컬 HTTP/Chrome 플레이: 메인 메뉴 → 난이도·직업 선택 → 도박사 상세 → 세 개 문 선택 화면까지 확인
- macOS 로컬 플레이는 실제 Mac에서 확인했으며, Codex로 연결한 Windows–Mac 양방향 테스트를 수행했습니다.
- iPhone용 정식 버전은 App Store에 공개되어 있습니다. 각 신규 버전의 빌드·심사 상태는 해당 릴리스 기록에 별도로 명시합니다.

## 프로젝트 구조

- `Assets/Art` — 카드, 캐릭터, 문, 배경, UI 원본 에셋
- `Assets/Audio` — 게임 BGM과 효과음
- `Assets/Data` — 카드와 게임 데이터
- `Assets/Scripts` — 카드, 전투, 진행, 저장, UI, 오디오 로직
- `Assets/Editor/PlayableGameBuilder.cs` — 플랫폼별 빌드 진입점
- `Packages`, `ProjectSettings` — Unity 재현 환경
- `tools` — 빌드·검증 보조 스크립트
- `docs/submission` — 해커톤 제출 PDF와 에셋 고지

## 제출 문서

- [게임 소개서 PDF](docs/submission/Three_Doors_of_Fate_게임소개서.pdf)
- [AI 도구·프롬프트 활용 내역 PDF](docs/submission/Three_Doors_of_Fate_AI활용내역서.pdf)
- [에셋 출처와 라이선스](docs/submission/asset-attribution.md)

## AI 활용과 권리

게임의 기획, 코드 작성, 이미지·음악 제작, 테스트 자동화와 문서화 과정에서 AI 도구를 활용했습니다. 모든 결과는 사람의 지시·선택·수정·검증을 거쳤습니다.

프로젝트 소유자는 게임의 원본 아트와 배경 음악을 AI와 함께 직접 제작했으며 공개 저장소와 게임 빌드에 배포하는 것을 승인했습니다. 해당 원본 에셋은 별도 표기가 없는 한 all rights reserved입니다. 임팩트 효과음은 기록된 CC0 원본을 사용하며, 세 글꼴은 SIL OFL 1.1을 따릅니다.

자세한 조건은 [LICENSE](LICENSE), [NOTICE](NOTICE.md), [ASSET_LICENSES](ASSET_LICENSES.md)를 확인하세요.

## English summary

Three Doors of Fate is a Korean-first, single-player deck-building roguelike built with Unity. Choose one of three doors, manage cards, dice, debt and status effects, defeat bosses, and extend the run through an endless record mode. Download the iPhone version from the [App Store](https://apps.apple.com/kr/app/three-doors-of-fate/id6798086296), or play the v1.1.2 WebGL build through GitHub Pages.
