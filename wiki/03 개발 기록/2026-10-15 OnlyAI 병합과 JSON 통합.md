---
tags: [development/merge, data/json, ui/lobby]
status: complete
updated: 2026-10-15
---

# 2026-10-15 OnlyAI 병합과 JSON 통합

## 병합 기준

- 기존 main 기준 c0b3d60에 origin/OnlyAI의 acf7c46·01a8723을 실제 merge한다. 처음에는 Map/Battle 파일 보존 방향이었으나, 사용자 추가 요청으로 현재 동작을 유지하며 JSON 참조도 통합했다.
- OnlyAI의 Title/Lobby Scene·컨트롤러·메뉴 JSON과 데이터 파서/검증/테스트를 가져왔다. 기존 UI를 새로 설계하거나 재생성하지 않았다.
- 현재 main의 Cache/실제 HP/Healer/자연 SP/보상/Shop/HUD/ThirdBoss enum을 유지하고 데이터 공급원만 JSON으로 연결했다.
- 사용자 미커밋 폰트·Obsidian·wiki 변경은 별도 stash로 보존하고 병합 후 복원한다. 복구용 stash는 유지한다.

## 실행 흐름

Title 표시 → 내부 데이터 접근 버튼 → Lobby → 새 게임 → Map. 자동 맵 진입은 해제했다.

- 메뉴 새 게임에서만 Packet 30, Layer 1/EXP 0, 빈 Cache/구매 이력, 시작 노드로 초기화한다. Title/Lobby 방문은 기존 판을 바꾸지 않는다.
- 설정/이어하기는 안내 모달이며 저장 파일을 읽는 실제 이어하기는 아직 미구현이다.
- Build Settings 인덱스는 기존 Title0/Map1/Battle2/Shop3을 유지하고 Lobby4를 추가한다.

## JSON 기준 위치

Assets/Resources/GameData/manifest.json이 기본 정의·문구 테이블을 등록한다.

| 파일 | 현재 역할 |
|---|---|
| Definitions/operators.json | 대원 기본 스탯·직군·타깃·skillIds |
| Definitions/enemies.json | 적 기본 스탯 |
| Definitions/skills.json | 최대 SP·충전 타입/양 |
| Definitions/caches.json | 7종 이름/설명 키·가격·고정 능력치·보상 +50% |
| Definitions/run_rules.json | 시작/승리/Layer 재화·EXP/요구치, Healer ×100, 공격당 SP, 카드 6장/1~10/특수 범위1~5, Front 배율 |
| Definitions/shops.json | Packet 재화 ID와 고정 판매 Cache ID 목록 |
| Localization/ko-KR/menu.json | 타이틀·로비 문구 |
| Localization/ko-KR/shop.json | 상품/구매 상태·상점·진행 HUD 문구 |
| 기존 ui/system/maps/characters/skills/Dialogue JSON | 카드·승인·로딩·맵·대원·스킬·결과 문구 |

- 기존 Scene/Prefab/SO는 ID와 시각 요소·객체 참조를 유지한다. Cache/진행/SO의 정의가 연결된 경우 수치는 JSON이 기준이다. 정의 ID가 없는 임시 테스트 객체만 이전 직렬화 값을 사용한다.
- HP/퇴각/SP·Packet 잔액·구매 이력 같은 현재 판 상태를 JSON 기본 정의에 쓰지 않는다. 저장 시스템과 데이터 정의 JSON은 별개다.
- 씬 구조/노드 객체 연결·연출/레이아웃·수식 구현 전체를 JSON으로 덤프한 것은 아니다. 게임 규칙의 제어 흐름은 코드에 남는다.

## 유지·검증한 현재 규칙

- 7종 가격 12/16/12/8/20/32/24, 승리 기본8 Packet/12 EXP, 시작30, Layer16×1.5 요구치·상승10.
- 보상 Cache는 승리 재화/EXP +50% 반올림. Layer 상승 추가 Packet과 요구치를 바꾸지 않는다.
- 원본 기본 HP와 Cache 계산 후 HP 분리, 라운드 초기화 시 Cache 유지, 퇴각은 전투 Scene 한정.
- Healer (a+b)×100, 실제 아군/적 공격마다 활성 아군 자연 SP, 취소·방어·회복 제외와 최대 SP 상한.

## 검증 결과

- CodexMergedJsonFinal.log: JSON/Scene/Prefab/Asset ID, 구매·가격·보상, EXP 경계값, HP·퇴각/재배치, Healer·SP, 카드 싱글턴, 맵8개/enum 숫자 회귀 검증 통과.
- CodexMergedJsonTests.xml: GameDataCatalogTests 18개 통과. 잘못된 JSON·중복/참조·빈 값과 Cache 가격/상점 참조/진행 증가율 거절 포함.
- CodexFrontEndMergedPlay.log: Title 유지→Lobby/모달 차단→새 게임→Map 초기화 제한 플레이 통과.
- CodexMergedBattlePlay.log: Cache 보유 맵→전투→승리→맵, 기존 보너스·노드·시간 복원 통과.
- CodexMergedShopPlay.log: JSON 7종 목록/가격/문구, 구매/거절→Map 노드8→상점 재입장 유지 통과.
- 실제 포인터 입력·해상도별 스크린샷 QA·전체 빌드·미선택 전체 Test Runner 스위트는 실행하지 않았다.

## 다음 작업

저장/이어하기와 종료 판 처리, 무결성·정합성 상세, 상위 곱연산 Cache·실제 스킬 효과는 TODO에 남는다. 이전 OnlyAI 기획 문서는 참고 자료이며 최신 사용자 답변과 통합 TODO를 우선한다.

관련: [[TODO LIST/TODO LIST|통합 TODO]], [[02 데이터/JSON 데이터 관리|JSON 관리]], [[01 시스템/상점 시스템|상점]]
