---
tags: [data/json, implementation]
status: active
updated: 2026-10-02
---

# JSON 데이터 관리

> [!implemented] 🟢 데이터 이전 및 참조 연결
> `OnlyAI` / `acf7c46`에서 문구와 대원·적·스킬 기본 정의가 JSON으로 이전되었다. 이 문서는 정적 코드·씬·Asset 대조 결과이며 Unity 검증 메뉴·Test Runner·컴파일·빌드·플레이 통과 기록은 아니다.

## 데이터의 기준 위치

루트는 `Assets/Resources/GameData`다. `manifest.json`의 `schemaVersion: 1`, `locale: ko-KR`, 파일 목록과 필수 문구 키를 기준으로 Catalog를 구성한다. 현재 등록 파일은 문구 6개, 기본 정의 3개다. Resources 경로에는 확장자를 넣지 않는다.

| 파일 | 역할 |
|---|---|
| `Definitions/operators.json` | 대원 ID·표시명 키·포지션·직군·타깃 필요 여부·기본 능력치·skillIds |
| `Definitions/enemies.json` | 적 ID·표시명 키·기본 능력치 |
| `Definitions/skills.json` | 스킬 ID·이름/설명 키·최대 SP·충전 방식/양 |
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
- 맵 연결, 적 스폰 수·위치, 카드 수·계산식, 연출 시간, 현재 런 상태까지 모두 JSON으로 옮긴 것은 아니다. 해당 씬·코드도 계속 확인한다.

## 빈 데이터와 미연결 항목

- 대원 3명의 `skillIds`는 현재 모두 빈 배열이다. `dorothy_skill_1` 정의는 존재하지만 자동 연결되지 않는다.
- 스킬 이름·설명, 노드 표시명, 결과 대사에는 의도적으로 빈 문자열을 허용한 항목이 있다. 허용된 빈 값과 누락된 키는 다르다.
- 결과 화면의 작전명 대체 문구는 현재 `NaN 작전`이다.
- SP 충전/발동 모델은 존재하지만 전투 이벤트·입력·실제 효과와의 연결은 미완성이다.
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

> [!warning] 실행 미확인
> 이번 Wiki 업데이트에서는 Unity 프로젝트를 읽기만 했다. 검증 코드와 테스트 파일의 존재를 테스트 성공으로 기록하지 않는다. Unity 검증 메뉴·Test Runner·컴파일·빌드·플레이 검증 결과는 별도로 기록해야 한다.

우선순위는 [[TODO LIST/코드 TODO LIST|코드 TODO LIST]]를 따른다. 일반 라운드 보너스 초기화 누락을 먼저 수정·확인하고, 데이터 검증 메뉴와 테스트 및 성공/실패·퇴각·맵 왕복 QA를 진행한다.

## 관련 문서

- [[00 프로젝트/구현 현황|구현 현황]]
- [[02 데이터/현재 데이터 카탈로그|현재 데이터 카탈로그]]
- [[01 시스템/대원·적·스킬|대원·적·스킬]]
- [[01 시스템/씬 전환 및 로딩|씬 전환 및 로딩]]
