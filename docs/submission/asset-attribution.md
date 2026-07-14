# Three Doors of Fate — 에셋 출처와 라이선스 감사

최종 감사일: 2026-07-14

이 문서는 저장소 안의 출처 기록, 에셋 내장 메타데이터, 그리고 프로젝트 소유자가 2026-07-14 현재 대화에서 직접 확인한 제작·배포 권한을 구분해 기록한다. 소유자는 게임의 내부 아트와 배경 음악을 AI 도구와 함께 직접 제작했으며 공개 저장소와 게임 빌드에 배포하는 것을 승인했다. 이 확인은 소유자의 권리 진술이며, AI 서비스 약관이나 국가별 저작권 성립 여부에 대한 독립적인 법률 의견은 아니다.

## 소유자 확인 원본 아트와 음악

| 에셋 그룹 | 대표 경로 | 제작·권리 근거 | 공개 상태 |
|---|---|---|---|
| 카드·캐릭터·문·배경·UI 아트 | `Assets/Art/**/*.png` | 제작 매니페스트와 AI 보조 제작 기록; 프로젝트 소유자의 직접 제작·배포 승인 확인 | owner-confirmed / all rights reserved |
| 배경 음악 5곡 | `Assets/Audio/Music/*.mp3` | 게임 내 트랙 연결 기록; 프로젝트 소유자의 AI 보조 제작·배포 승인 확인 | owner-confirmed / all rights reserved |

`all rights reserved`는 별도 오픈 라이선스를 부여하지 않는다는 프로젝트 표기다. 공개 저장소 열람과 배포된 게임의 정상적인 이용을 넘어 원본 아트·음악을 별도로 복제·재배포·재판매할 권리를 제3자에게 부여한다는 뜻이 아니다. 생성에 사용한 AI 서비스의 약관과 관할 지역의 법적 판단은 최종 배포 책임자가 별도로 준수해야 한다.

## 확인된 CC0 효과음

`Assets/Audio/SFX/Impact/*.wav`의 최종 클립은 아래 CC0 소스를 편집·레이어링·필터링·정규화해 제작했다. 저장소의 `Assets/Audio/SFX/Impact/SOURCES.md`에 같은 URL과 제작 메모가 기록되어 있다. 최종 파일은 게임 내 레이어링을 고려해 헤드룸을 남긴 mono 48 kHz PCM WAV다.

| 원본 묶음 | 기록된 URL | 저장소 근거 | 상태 |
|---|---|---|---|
| Kenney Impact Sounds | <https://www.kenney.nl/assets/impact-sounds> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |
| OpenGameArt Stone Door | <https://opengameart.org/content/stone-door> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |
| OpenGameArt Metal Impact Sounds | <https://opengameart.org/content/metal-impact-sounds> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |
| OpenGameArt Swishes Sound Pack | <https://opengameart.org/content/swishes-sound-pack> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |
| OpenGameArt 3 Dark Magic Spells | <https://opengameart.org/content/3-dark-magic-spells> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |
| OpenGameArt 100 CC0 SFX #2 | <https://opengameart.org/content/100-cc0-sfx-2> | `Assets/Audio/SFX/Impact/SOURCES.md` | verified-CC0 |

CC0 상태는 프로젝트가 기록한 원본 출처 문서를 기준으로 한 감사 결과다. 개별 원본 페이지가 변경될 수 있으므로 공개 릴리스 전 링크와 다운로드 파일의 라이선스 메타데이터를 다시 보관하는 것이 좋다.

## 확인된 SIL OFL 1.1 글꼴

세 TTF 파일의 name table에 SIL Open Font License, Version 1.1 전문 안내와 라이선스 URL이 내장되어 있음을 확인했다.

| 파일 | 내장 메타데이터 근거 | 상태 |
|---|---|---|
| `Assets/Fonts/GowunBatang-Bold.ttf` | name ID 13: SIL Open Font License 1.1, name ID 14: OFL URL | verified-OFL-1.1 |
| `Assets/Fonts/GowunBatang-Regular.ttf` | name ID 13: SIL Open Font License 1.1, name ID 14: OFL URL | verified-OFL-1.1 |
| `Assets/Fonts/NotoSansKR-VF.ttf` | name ID 13: SIL Open Font License 1.1, name ID 14: OFL URL | verified-OFL-1.1 |

글꼴은 OFL 1.1 조건에 따라 게임과 함께 배포한다. 원본 파일의 저작권·라이선스 메타데이터를 유지하고, 글꼴 파일을 수정하거나 별도로 재배포할 때는 OFL 1.1의 고지·예약 글꼴명 등 적용 조건을 다시 확인해야 한다.

## 플레이 스크린샷과 오래된 프리뷰

상위 제작 폴더의 오래된 시안·프리뷰 전체는 공개 대상이 아니다. 최종 공개 저장소와 제출 자료에는 실제 게임 동작을 설명하기 위해 선별하고 검토한 플레이 스크린샷만 포함한다. 선별 스크린샷에 표시되는 원본 게임 아트와 음악에 대한 공개 배포 권한은 위 소유자 확인에 포함되지만, 작업 중 생성된 미채택 시안이나 무관한 프리뷰까지 공개 승인되었다는 뜻은 아니다.

## 근거 해석

- 아트 매니페스트에는 이미지 생성 도구 사용, 해상도 정규화, 텍스트 없는 카드 일러스트 제작 흐름이 기록되어 있다. 제작 기록만으로 권리를 추정하지 않고, 이번 감사에서는 프로젝트 소유자의 직접 제작·공개 배포 승인 진술을 별도 근거로 사용했다.
- 통일 카드 매니페스트는 48개 이미지의 규격과 구성을 증명하며, 권리 확인은 소유자의 별도 진술에서 나온다.
- `.meta` 파일과 Unity 임포트 설정은 기술적 사용 상태만 보여 주며 저작권이나 라이선스를 증명하지 않는다.
- 글꼴은 파일명으로 추정하지 않고 각 TTF에 내장된 name ID 13과 14의 OFL 1.1 메타데이터를 확인했다.
- 임팩트 효과음은 `Assets/Audio/SFX/Impact/SOURCES.md`의 원본 URL과 CC0 제작 메모를 근거로 분리했다.

## 릴리스 판단

공개 릴리스에 포함할 에셋은 세 근거로 구분한다. 원본 아트와 배경 음악은 프로젝트 소유자의 직접 제작·배포 승인에 따라 `owner-confirmed / all rights reserved`, 임팩트 효과음은 기록된 CC0 원본에 따라 `verified-CC0`, 세 글꼴은 내장 메타데이터에 따라 `verified-OFL-1.1`이다. 오래된 프리뷰 전체는 제외하고 선별·검토한 플레이 스크린샷만 게시한다. 최종 배포 담당자는 릴리스 전에 이 구분, CC0 출처 링크, OFL 고지 유지, 공개 파일 목록을 다시 확인해야 한다.
