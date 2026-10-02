# Title / Lobby 구현

## 실행과 수정

`Assets/Scenes/Title.unity`를 열고 Play하면 내부 데이터 접근 버튼으로 Lobby에 진입합니다. Build Settings도 Title → Lobby → Map_1F → Playing_Scene 순서로 등록했습니다.

UI는 저장된 Scene의 Canvas/TMP/단색 Image입니다. 런타임에 새 UI를 생성하지 않으므로 Hierarchy와 Inspector에서 위치·크기·색상을 직접 수정할 수 있습니다. 이미지 아이콘은 아직 사용하지 않습니다. Canvas 기준 해상도는 1280×720이며 CanvasScaler가 화면 크기에 맞춥니다.

표시 문구는 `Assets/Resources/GameData/Localization/ko-KR/menu.json`에서 수정합니다. `MenuCanvas`의 TMP는 Play 시작 시 `LocalizedSceneTexts`가 JSON으로 채웁니다. Scene 편집 화면에서 TMP가 빈 것은 의도된 동작입니다. 숫자/문구를 Scene에 직접 입력해도 Play에서 JSON 값으로 교체됩니다.

## 담당 코드

- `TitleSceneController`: 진입 버튼 → Lobby.
- `LobbySceneController`: 설정/이어하기 모달 열기·닫기, 새 게임 → Map. 모달이 열려 있을 때 배경 버튼과 새 게임을 차단합니다.
- `MenuSceneLoader`: 독립된 전환 Canvas를 통한 페이드, 중복 로드 차단, 비동기 씬 로드. 시간 배율과 무관하게 페이드가 진행되며 다음 씬 진입 전 정상 배율을 복원합니다.
- `Assets/Editor/FrontEndSceneBuilder.cs`: 초기 씬을 생성한 Editor 도구. **Tools → Randint → Front End → Rebuild Title and Lobby Scenes**는 두 씬의 수동 UI 변경을 덮어쓰므로 필요할 때만 사용합니다. 런타임에는 호출되지 않습니다.

## 의도적으로 미구현한 정책

설정은 안내 창만 존재합니다. 설정 항목/적용 방식은 확정되지 않아 실제 환경 설정을 추가하지 않았습니다.

이어하기 폴더는 디스크 저장 기능이 미구현임을 안내합니다. `MapRunState`의 임시 메모리 위치를 저장 파일처럼 표시하거나 이어하기로 사용하지 않습니다.

새 게임은 `MapRunState.Reset()`만 호출하고 기존 Map의 `startNode`로 시작합니다. 대원/적은 기존 전투 초기화 로직을 따릅니다. 어떤 JSON 정의나 디스크 세이브도 생성·삭제·덮어쓰지 않습니다. 추후 재화·완료 노드 등 런 상태가 추가되면 이 초기화 경로를 확장해야 합니다.

Map의 창 모드/로비 복귀, Battle 일시정지, 디스크 저장 정책은 이번 Title/Lobby 구현에 포함하지 않았습니다.

## 검증

**Tools → Randint → Game Data → Validate JSON and References**에서 메뉴 문구 키, TMP 및 컨트롤러 참조, 목적지 Scene의 활성 Build Settings 등록을 함께 검사합니다.

EditMode의 `FrontEndSceneTests`는 실제 PlayMode로 진입해 Button.onClick으로 Title→Lobby, 두 모달의 입력 차단/닫기, 새 게임 초기화 및 Map 시작 노드 복원을 확인합니다. 기존 `GameDataCatalogTests`와 `GameDataSceneTests`도 함께 실행할 수 있습니다.
