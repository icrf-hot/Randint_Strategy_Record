# JSON 데이터 관리

문구와 기본 스탯의 기준은 이 폴더의 UTF-8 JSON입니다. Scene/Prefab에는 문구 키 또는 데이터 ID를 저장하고, 기존 데이터 ScriptableObject에는 JSON ID를 저장합니다. 이미지·프리팹처럼 Unity 객체인 리소스는 기존 Asset 연결을 유지합니다.

## 수정할 파일

| 파일 | 관리 대상 |
| --- | --- |
| `Localization/ko-KR/ui.json` | UI 항목명·버튼·카드 표기·승인 문구·초기 placeholder |
| `Localization/ko-KR/system.json` | 결과 화면과 네 가지 로딩 문구 |
| `Localization/ko-KR/menu.json` | Title·Lobby·설정/이어하기 안내 및 메뉴 전환 문구 |
| `Localization/ko-KR/maps.json` | 노드 표시명·설명·선택지·타이핑 문구 |
| `Localization/ko-KR/characters.json` | 대원 이름과 소개 |
| `Localization/ko-KR/skills.json` | 스킬 이름과 설명 |
| `Localization/ko-KR/Dialogue/battle_result.json` | 성공·실패 대사 (현재 미작성) |
| `Definitions/operators.json` | 대원 기본 스탯·포지션·직군·스킬 ID 목록 |
| `Definitions/enemies.json` | 적 기본 스탯 |
| `Definitions/skills.json` | 스킬 SP·충전 방식·충전량 |

수치는 기존 Asset 값 그대로 이전했습니다. 비어 있던 작전명·대사·스킬 설명은 `value: ""`, `allowEmpty: true`로 보존했습니다. 빈 작전명은 결과 화면에서 기존 대체 문구를 사용합니다. 현재 언어는 `ko-KR` 하나이며 런타임 언어 전환 기능은 아직 없습니다.

## 등록과 참조

- 모든 파일은 `schemaVersion: 1`을 사용합니다.
- 새 파일은 `manifest.json`의 `files`에 등록합니다. 경로는 `Resources` 기준이며 `.json` 확장자는 생략합니다.
- `kind`는 `text`, `operator`, `enemy`, `skill` 중 하나입니다.
- ID/키는 소문자 영문으로 시작하고 영문 소문자·숫자·밑줄·점만 사용합니다. 예: `saria`, `operator.saria.name`.
- 텍스트 키는 모든 문구 파일 사이에서 유일해야 합니다. 정의 ID는 같은 종류의 모든 파일 사이에서 유일해야 합니다.
- 키를 바꾸면 Scene의 연결 키와 JSON의 참조 키를 함께 변경해야 합니다. 문구만 바꿀 때는 `value`만 수정합니다.
- 누락 키는 빈 문구로 숨기지 않고 오류로 처리합니다. 의도적인 미작성 문구만 `allowEmpty: true`로 허용합니다.
- `requiredTextKeys`는 코드가 직접 사용하는 고정 키를 검증합니다. 고정 키를 코드에 추가하면 이 목록에도 등록합니다.
- 새로운 캐릭터 대사 파일은 `Dialogue/<character-id>.json`으로 만들고 기존 result Presenter의 대사 키를 해당 캐릭터 키로 연결할 수 있습니다. 화자 자동 선택 정책은 별도 기능입니다.

## 스탯과 전투 상태

대원의 스킬 구성은 `operators.json`의 `skillIds`에서만 지정합니다. 각 ID는 `skills.json`에 존재해야 하며, 현재 스킬을 연결하지 않은 대원은 빈 배열을 유지합니다. 별도의 Inspector 스킬 배열을 맞출 필요가 없습니다.

현재 HP·SP·퇴각·재배치 예약·임시 보너스는 JSON에 저장하지 않습니다. 새 전투의 `Operator.Awake()`와 `Enemy.Awake()`는 JSON 기본값을 읽어 각 객체의 현재 상태를 초기화합니다. 전투 결과가 원본 JSON이나 기본 스탯을 변경하지 않습니다.

전투 노드별 적 구성과 공통 전투 공식/연출 시간은 이번 이전에 포함되지 않았습니다. 현재 적 생성 배치와 전투 수치 계산 로직은 기존 코드가 담당합니다.

## 로딩과 검증

`GameData`가 첫 Scene의 `Awake()` 전에 manifest의 모든 파일을 읽고 캐시합니다. 검증을 통과한 Catalog만 공개하므로 일부 파일만 읽힌 상태로 실행하지 않습니다. Play 시작 때 캐시를 초기화하며 Play 중 JSON 수정은 다음 Play 실행부터 반영됩니다.

Unity 메뉴에서 **Tools → Randint → Game Data → Validate JSON and References**를 실행합니다. 빌드 전에도 같은 검증이 자동 실행됩니다.

검사 대상은 JSON 구문·중복 속성·스키마 버전·ID/키 형식·파일 누락·중복 ID/키·필수 문구·스탯 범위·직군 조합·스킬 참조 및 Scene/Prefab/ScriptableObject의 지정 키·ID·TMP 참조입니다. Scene 검사는 Preview Scene을 사용하여 열려 있는 Scene을 저장하거나 덮어쓰지 않습니다.

`LocalizedSceneTexts`는 Scene의 초기 TMP 문구를 한 번 설정합니다. 전투 상태·숫자·결과·타이핑처럼 이후 변하는 내용은 각 담당 코드가 갱신합니다. Console 개발 로그는 번역 대상에 포함하지 않습니다.

Unity Test Runner의 EditMode에서 `GameDataCatalogTests`와 `GameDataSceneTests`를 실행해 로더 검증 및 실제 Scene의 JSON 조회·새 전투 HP 초기화·성공/실패 문구 출력을 검사할 수 있습니다. 글자 모양과 화면 배치는 Game View에서 별도 확인해야 합니다.
