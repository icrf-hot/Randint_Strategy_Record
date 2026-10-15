---
tags: [data/json, implementation]
status: active
updated: 2026-10-15
---

# JSON 데이터 관리

> [!implemented] 🟢 데이터 이전 및 참조 연결
> OnlyAI의 JSON 구조를 main에 병합하고 현재 Cache·진행/보상·상점·Healer/SP/카드/Front 규칙을 확장 이전했다. 컴파일/참조/회귀 검증, EditMode 18개 및 타이틀/전투/상점 제한 경로 검증 통과. 전체 빌드·시각 QA는 별도다.

## 데이터의 기준 위치

루트는 `Assets/Resources/GameData`다. manifest schemaVersion 1, locale ko-KR을 유지한다. 현재 문구 8개·정의 6개 테이블을 등록하며 Resources 경로에는 확장자를 넣지 않는다.

| 파일 | 역할 |
|---|---|
| `Definitions/operators.json` | 대원 ID·표시명 키·포지션·직군·타깃 필요 여부·기본 능력치·skillIds |
| `Definitions/enemies.json` | 적 ID·표시명 키·기본 능력치 |
| `Definitions/skills.json` | 스킬 ID·이름/설명 키·최대 SP·충전 방식/양 |
| `Definitions/caches.json` | 7종 Cache 가격·고정 능력치·보상 +50%·문구 키 |
| `Definitions/run_rules.json` | Packet/Layer/EXP·Healer 배율·공격당 자연 SP·카드 생성/Front 기본 배율 |
| `Definitions/shops.json` | Packet 재화와 판매 Cache ID 목록 |
| `Localization/ko-KR/menu.json` | 타이틀·로비 문구 |
| `Localization/ko-KR/shop.json` | 상품/구매 상태·상점·진행 HUD 문구 |
| `Localization/ko-KR/ui.json` | 카드·전투 승인·UI 문구 |
| `Localization/ko-KR/system.json` | 로딩·성공/실패 결과·작전명 대체 문구 |
| `Localization/ko-KR/maps.json` | 노드 표시명·설명·선택지 문구 |
| `Localization/ko-KR/characters.json` | 대원·적 이름 |
| `Localization/ko-KR/skills.json` | 스킬 이름·설명 |
| `Localization/ko-KR/Dialogue/battle_result.json` | 성공/실패 결과 대사 |

기본 수치는 [[02 데이터/현재 데이터 카탈로그|현재 데이터 카탈로그]]와 캐릭터 문서를 참조한다. Scene/Prefab/ScriptableObject의 직렬화 ID와 JSON 정의를 함께 확인해야 한다.

## 런타임 조회와 소유권

- `GameData`는 첫 씬 로드 전에 Catalog를 준비하고 캐시한다. SubsystemRegistration에서 캐시를 초기화한다.
- `GameDataCatalog`는 manifest의 파일을 모두 읽고 검증한 뒤 텍스트·대원·적·스킬 조회를 제공한다.
- `OperatorData`·`SkillData` Asset은 `dataId`를 보관한다. 기본 능력치·이름·스킬 정의는 JSON 조회 결과다.
- 적 프리팹은 `dataId: makeshift`로 기본 정의를 조회한다.
- `Operator.Awake()`에서 대원의 JSON `skillIds`를 읽어 `OperatorSkill` 인스턴스를 만든다. 현재 HP와 스킬 SP는 각 전투 인스턴스의 상태이며 JSON에 저장하지 않는다.
- 맵 노드/선택지·로딩·결과 화면은 문구 키를 보관한다. `LocalizedSceneTexts`는 씬의 TMP 참조와 문구 키를 연결한다.
- 현재 locale은 한국어로 고정되어 있다. 다른 언어 데이터·런타임 언어 전환 UI·저장 기능은 구현된 것으로 보지 않는다.
- 카드 수(6장), 숫자 범위(현재 1~10), 특수 후보 범위(1~5), Front 기본 배율과 Healer ×100은 run_rules.json으로 이전했다. 맵 객체 연결·스폰 위치·시각/연출 값과 계산 제어 흐름은 Scene/코드에 유지한다.
- Cache/RunProgressionSettings/ShopCatalogData는 definitionId로 JSON을 읽고 기존 Asset은 Scene/아이콘 참조를 유지한다. 연결된 정의가 수치의 기준이며 HP/SP/잔액/구매 이력은 기본 정의에 기록하지 않는다.

## 빈 데이터와 미연결 항목

- 대원 3명의 `skillIds`는 현재 모두 빈 배열이다. `dorothy_skill_1` 정의는 존재하지만 자동 연결되지 않는다.
- 스킬 이름·설명, 노드 표시명, 결과 대사에는 의도적으로 빈 문자열을 허용한 항목이 있다. 허용된 빈 값과 누락된 키는 다르다.
- 결과 화면의 작전명 대체 문구는 현재 `NaN 작전`이다.
- 자연 SP는 실제 아군/적 공격 이벤트에 연결했다. 현재 대원 skillIds는 비어 있으므로 배정된 스킬이 있어야 실제로 충전된다. 상세 효과/사용 입력은 미구현이다.
- JSON 이전은 밸런스 확정이나 결과 대사 완성을 의미하지 않는다.

## 검증 계층

`JsonSyntaxGuard`와 Catalog는 잘못된 JSON 구문·중복 속성, 스키마 버전, 키/ID 형식·중복, 기본 수치 범위, 대원 포지션/직군 조합, 스킬 참조, 필수 문구와 누락 파일 등을 검사한다. 누락된 키를 임의의 문구로 대체하지 않는다. 빈 문자열은 명시적인 허용 설정에 따라 구분한다.

`Assets/Editor/GameDataValidationMenu.cs`에는 다음 진입점이 있다.

- Unity 메뉴: `Tools → Randint → Game Data → Validate JSON and References`
- 배치 검증: `ValidateBatch`
- 빌드 전 검증 훅: `IPreprocessBuildWithReport`

검증 메뉴는 JSON뿐 아니라 Scene/Prefab/ScriptableObject의 키·ID, 누락 스크립트, 주요 UI/대원/결과 화면 참조, 맵 노드 ID 중복, 전투 씬의 Build Settings 등록 등을 확인한다.

## 테스트와 다음 확인

`Assets/Tests/EditMode/GameData`에 `GameDataCatalogTests`와 `GameDataSceneTests`가 추가되어 있다. JSON 오류·참조·허용된 빈 값·실제 Resources 조회 및 인스턴스별 SP, 실제 씬의 대원/적/맵 노드, 기본 HP와 결과 화면·시간 배율 등을 검증하는 코드가 있다. 일부 씬 테스트는 Play Mode에 진입한다.

> [!info] 검증 범위
> 2026-10-15 GameDataCatalogTests 18개, MergedJsonVerification의 현재 게임 규칙 회귀, Title/Lobby·Cache 전투·JSON Shop 제한 경로를 통과했다. 전체 빌드와 모든 씬 테스트/실제 포인터·스크린샷 QA를 통과했다는 뜻은 아니다.

현재 우선순위는 [[TODO LIST/TODO LIST|통합 TODO]]를 따른다. OnlyAI에서 가져온 코드/기획 TODO는 과거 참고이며 최신 사용자 답변과 병합 검증 기록을 우선한다.

## 관련 문서

- [[00 프로젝트/구현 현황|구현 현황]]
- [[02 데이터/현재 데이터 카탈로그|현재 데이터 카탈로그]]
- [[01 시스템/대원·적·스킬|대원·적·스킬]]
- [[01 시스템/씬 전환 및 로딩|씬 전환 및 로딩]]
