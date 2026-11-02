# Title / Lobby / Preparation 구현

## 실행과 수정

`Assets/Scenes/Title.unity`에서 Lobby에 진입합니다. 기존 Build Settings 인덱스를 유지하고 Preparation(5)을 추가했습니다: Title(0), Map_1F(1), Playing_Scene(2), Shop(3), Lobby(4), Preparation(5). 새 게임 흐름은 Title → Lobby → Preparation → 작전 시작 → Map → Playing입니다. 계속하기는 Lobby에서 저장을 복원해 Map으로 바로 갑니다.

기본 메뉴 UI는 저장된 Scene의 Canvas/TMP/단색 Image입니다. 슬롯 그리드와 Map 저장 선택 모달은 런타임에 생성합니다. 기본 메뉴는 Hierarchy/Inspector에서, 슬롯 배치는 SaveSlotBrowser/SaveSlotWindow/MapSaveControls에서 수정합니다. Canvas 기준 해상도는 1280×720이며 CanvasScaler가 화면 크기에 맞춥니다.

표시 문구는 `Assets/Resources/GameData/Localization/ko-KR/menu.json`에서 수정합니다. `MenuCanvas`의 TMP는 Play 시작 시 `LocalizedSceneTexts`가 JSON으로 채웁니다. Scene 편집 화면에서 TMP가 빈 것은 의도된 동작입니다. 숫자/문구를 Scene에 직접 입력해도 Play에서 JSON 값으로 교체됩니다.

## 담당 코드

- `TitleSceneController`: 진입 버튼 → Lobby.
- `LobbySceneController`: 설정/이어하기 모달 열기·닫기, 새 게임 → Preparation. 모달이 열려 있을 때 배경 버튼과 새 게임을 차단합니다.
- `PreparationSceneController`: Front 사리아/Middle 도로시/Back 조이스 무어 고정 편성, 변경 불가 TMP 경고, 데이터 손실률 0단계, 작전 시작/로비 복귀. 준비 화면 진입·취소는 현재 판을 초기화하지 않습니다.
- `MenuSceneLoader`: 독립된 전환 Canvas를 통한 페이드, 중복 로드 차단, 비동기 씬 로드. 시간 배율과 무관하게 페이드가 진행되며 다음 씬 진입 전 정상 배율을 복원합니다.
- `Assets/Editor/FrontEndSceneBuilder.cs`: 초기 씬을 생성한 Editor 도구. **Tools → Randint → Front End → Rebuild Title and Lobby Scenes**는 두 씬의 수동 UI 변경을 덮어쓰므로 필요할 때만 사용합니다. 런타임에는 호출되지 않습니다.

## 저장과 미확정 정책

설정은 안내 창만 존재합니다. 설정 항목/적용 방식은 확정되지 않아 실제 환경 설정을 추가하지 않았습니다.

이어하기 폴더는 `SaveSlotWindow`와 공통 `SaveSlotBrowser`가 페이지당 10개 슬롯을 표시합니다. <, >로 이동하고 +로 10개짜리 페이지를 추가하며 빈 페이지도 유지합니다. 선택한 슬롯의 정보를 보고 계속하기/삭제합니다. 삭제는 두 번 확인하며 슬롯·페이지 변경이나 창 닫기는 확인을 취소합니다.

준비 화면의 작전 시작에서만 `MapRunState.StartNewRun(progressionSettings)`으로 Packet 30, Layer 1/EXP 0, 빈 Cache 보유/구매 이력, 두 사용자 HP 100%와 맵 위치를 초기화합니다. Title·Lobby·Preparation 방문과 준비 취소만으로는 초기화하지 않습니다. 기존 디스크 슬롯도 다음 명시적 저장 전까지 보존합니다.

Map은 좌측 상단 설정 버튼만 상시 표시합니다. 클릭하면 화면 전체에 50% 검정 오버레이와 저장/저장 후 나가기/닫기가 나타납니다. 저장 명령은 기존 슬롯 선택 창으로 이어지며 취소/저장 완료 시 설정으로 돌아옵니다. 저장 후 나가기는 성공 시에만 Lobby로 갑니다. 기존 기록 덮어쓰기는 두 번 확인합니다. `Application.persistentDataPath/Saves/`에서 1번은 기존 `run.json`, 이후는 `slot_0011.json` 같은 파일이고 페이지 수는 `pages.json`입니다. 전투·전환 중 저장 금지는 유지합니다.

## 검증

**Tools → Randint → Game Data → Validate JSON and References**에서 메뉴 문구 키, TMP 및 컨트롤러 참조, 목적지 Scene의 활성 Build Settings 등록을 함께 검사합니다.

EditMode의 `FrontEndSceneTests`는 실제 PlayMode로 진입해 Button.onClick으로 Title→Lobby, 두 모달의 입력 차단/닫기, 새 게임 초기화 및 Map 시작 노드 복원을 확인합니다. 기존 `GameDataCatalogTests`와 `GameDataSceneTests`도 함께 실행할 수 있습니다.

2026-11-01부터 일반 작업 검증은 컴파일을 기본으로 합니다. 이번 준비 씬 작업에서는 기존 테스트 경로만 갱신했으며 Play/런타임 테스트를 실행하지 않았습니다. MCP/Pipeline로 컴파일 완료/실패 없음/오류 없음과 Play 중지 상태를 확인합니다. `PreparationSceneBuilder.Create`는 Editor API로 새 씬을 생성하고 기존 씬은 덮어쓰지 않습니다. 준비 문구는 `preparation.json`, 기존 캐릭터 이미지/이름은 기존 Asset/JSON을 사용합니다.


2026-11-02: 맵은 Map_1F~Map_5F 독립 Scene입니다. 기존 빌드 인덱스는 유지하고 2~5층은 6~9에 추가했습니다. CampaignScenes/Resources/Campaign/scenes.json이 층 이동·전투/상점 복귀·이어하기 경로를 결정합니다. 저장 v5는 이전 v1~v4 공용 맵 경로를 해당 층 Scene으로 복원합니다. 층별 노드와 연결은 Scene에 저장하며 런타임 생성은 하지 않습니다.
