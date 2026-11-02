---
tags: [system/map, system/scene, authoring]
status: implemented
updated: 2026-11-02
---

# 층별 Scene 관리

## 현재 구조

1~5층을 독립 Unity Scene으로 관리한다. 기존에는 `Map_1F`를 열고 `MapRunState.MapFloor`에 따라 2~5층 노드를 실행 중 생성했다. 이제 노드·좌표·종류·연결·선택지·표시 문구를 각 Scene에 저장한다. `CampaignMapFlow`는 층 진행·사건·엔딩 UI만 담당하며 노드를 재생성하거나 배치를 덮어쓰지 않는다.

| 층 | Scene | 노드 수 | 마지막 노드 | Build index |
|---|---|---:|---|---:|
| 1 | `Assets/Scenes/Map_1F.unity` | 8 | 상점 | 1 |
| 2 | `Assets/Scenes/Map_2F.unity` | 27 | 디펜더 | 6 |
| 3 | `Assets/Scenes/Map_3F.unity` | 27 | 중간 보스 | 7 |
| 4 | `Assets/Scenes/Map_4F.unity` | 64 | 상층 접속 | 8 |
| 5 | `Assets/Scenes/Map_5F.unity` | 64 | 최종 보스 | 9 |

Title(0), Playing_Scene(2), Shop(3), Lobby(4), Preparation(5)를 포함한 기존 0~5 인덱스를 유지하고 새 Scene을 뒤에 추가했다. 6·7층과 히든 루트는 추가하지 않았다.

## 편집 방법

1. 원하는 `Map_nF.unity`를 연다. Play Mode 없이도 해당 층의 실제 노드들이 Hierarchy에 존재한다.
2. 2~5층은 `FloorN_Node번호_종류` 오브젝트를 개별 편집한다. 좌표·외형·씬 오브젝트 배치·배경·카메라·연결을 층별로 수정할 수 있다.
3. `MapNode.connectedNodes`에 다음 노드를 지정한다. `CampaignConnections`는 Scene의 연결 참조를 읽고 노드 이동에 맞춰 선을 갱신한다. 1층의 기존 연결 표현은 유지했다.
4. `MapPlayerPositionFeedback.startNode`는 그 층의 Start 노드를 가리킨다. `CampaignMapFlow.floorNumber`는 Scene 층 번호와 같아야 한다.
5. 노드 ID, Event 여부, 끝 노드의 게임 규칙은 저장 검증·사건 배정·캠페인 정의와도 연결되어 있다. **위치/연결을 편집하는 것과 ID/종류/노드 수 규칙을 바꾸는 것을 구분한다.** 후자를 바꿀 때는 `CampaignDefinition`, `EncounterCatalog.IsEventNode`, 저장 바인딩/검증도 함께 갱신해야 한다. 현재 ID 1~8/2001~2027/3001~3027/4001~4064/5001~5064와 끝 노드 규칙을 유지한다.

`MapNode`의 작성된 제목·설명을 직렬화해 씬 재로드 후에도 유지한다. 1층은 기존 로컬라이즈 키와 체스말 표시 분류를 유지한다. 공통 스크립트·머티리얼·UITheme 등 공유 Asset을 수정하면 여러 층에 영향을 줄 수 있다.

## Scene 경로와 전환

경로의 기준은 `Assets/Resources/Campaign/scenes.json`이다. `CampaignScenes.PathFor(floor)`와 `CurrentPath`를 새 게임·층 이동·전투 복귀·상점 복귀·저장에 사용한다.

- 새 게임: 준비 화면 → 1층 Scene.
- 층 완료: 다음 층 Scene을 요청하고 활성화 직전에 MapFloor와 현재 노드를 다음 층 시작점으로 변경한다.
- 전투/상점 복귀: 현재 MapFloor의 Scene으로 복귀한다. 전투에서 사출된 판은 기존처럼 로비로 간다.
- 이어하기: 검증·마이그레이션된 저장 파일의 목적 Scene을 열고 활성화 전에 준비 상태를 적용한다. 로비에 남아 있던 이전 메모리의 층 번호로 목적지를 정하지 않는다.
- 각 층 Scene을 직접 Play하면 새 판은 해당 층 Start에서 초기화할 수 있다. 이미 현재 노드가 있는 판의 층 번호는 Scene을 열었다는 이유로 조용히 덮어쓰지 않는다. 이 초기화 경로는 코드/데이터 검사만 했으며 이번 작업에서 Play 검증하지 않았다.

모든 전환은 기존 `TransitionManager`를 사용하며 입력 차단·단말 와이프·오디오 페이드 규칙을 유지한다.

## 저장 호환

저장 DTO는 v5다. `mapScenePath`와 `mapFloor`의 일치를 검사하고, v5에서 서로 다른 층의 Scene 경로는 거절한다.

v1/v2는 기존처럼 1층으로 복원한다. v3/v4의 2~5층 파일에 공용 `Map_1F` 경로가 저장되어 있어도 저장된 MapFloor를 기준으로 올바른 `Map_nF`로 준비 복원한다. v4 사건 원장과 검증료 결제/수령 상태는 보존한다. 읽기만으로 원본 파일을 바꾸지 않으며 다음 명시적 저장은 v5 경로를 사용한다. 같은 판의 종료 슬롯을 갱신할 때도 해당 층 경로로 정규화한다.

## 생성 도구와 확장

`Tools > Randint > Campaign > Create Missing Floor Scenes`는 등록된 Scene 중 없는 층만 생성한다. 기존 2~5층 Scene은 재생성하지 않으므로 개별 편집을 보존한다. 1층에는 공통 층 컨트롤러와 단일 사건 접속 선택을 유지한다. 이 도구는 기존 맵 템플릿과 `Campaign/floors.json`을 초기 배치에 사용하며 게임 실행 중에는 호출하지 않는다.

향후 n층을 추가하려면 Scene 경로 등록, 층 데이터·끝 노드/엔딩 규칙, 저장의 허용 층/노드 검사와 사건 티어 범위를 함께 확장한다. 현재 진행·저장·사건은 1~5층 범위다. Scene 파일만 복사해서 6·7층을 실제 루트로 열지는 않는다. 일반적인 Scene 편집 후 이 생성 도구를 다시 실행할 필요는 없다.

## 담당 파일과 검증

- `Map/Script/CampaignScenes.cs`, `Resources/Campaign/scenes.json`: 층별 Scene 경로.
- `CampaignMapFlow`, `CampaignMapConnections`, `MapNode`: 진행 UI·연결 렌더링·직렬화된 노드.
- `Editor/CampaignSceneAuthoring.cs`: Scene 생성/등록.
- `Editor/CampaignSceneVerification.cs`: Edit Mode 자산·참조·저장 경로 검사.
- `PreparedRunRestore`, `RunSaveBindings`, `RunSaveService`, `MenuSceneLoader`, `BattleToMapSceneLoader`, `ShopSceneRouter`: 저장·목적 Scene 선택.

2026-11-02 Unity 6000.3.11f1 C# 컴파일 통과. Edit Mode에서 5개 Scene의 노드 수·고유 ID·내부 연결·Start 참조·층 컨트롤러·문구/선택 직렬화·누락 스크립트 없음·선 Mesh·Build Settings 인덱스를 검사했다. v5 파일 왕복, v1~v4 공용 맵 파일의 목적지 호환, 잘못된 층/경로 거절과 기존 층/유물·2,500개 사건 패 회귀도 통과했다. 임시 저장소를 사용하고 원래 Scene 구성을 복원했다.

이번 작업에서는 Play Mode·게임플레이·화면 캡처 검증을 수행하지 않았다. Scene 저작과 C# 컴파일/데이터 검사를 실제 화면 전환·카메라·전투·전체 런 검증으로 해석하지 않는다. 다음 실제 플레이 확인 때는 상층 전투/상점 복귀, 상층 이어하기, 직접 Scene 시작과 씬별 시각 배치를 확인한다.
