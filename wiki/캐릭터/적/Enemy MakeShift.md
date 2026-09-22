---
aliases: [Enemy_MakeShift]
tags: [character/enemy]
implementation: implemented
updated: 2026-09-22
---

# Enemy MakeShift

> [!implemented] 🟢 구현
> 적 생성, 타기팅, 피격, 속도 기반 행동과 사망 처리가 전투 루프에 연결되어 있다.

| 항목 | 프리팹 직렬화 값 |
|---|---:|
| 프리팹 | `Enemy_MakeShift.prefab` |
| HP | 2000 |
| 공격력 | 200 |
| 방어력 | 10 |
| 마법 저항 | 15 |
| 공격 속도 | 1 |

## 현재 행동

- 자신의 턴에 물리 공격을 한 번 수행한다.
- 생존한 Front → Middle → Back 순서로 첫 대상을 공격한다.
- HP가 0이 되면 GameObject가 파괴된다.
- 고유 AI, 스킬, 상태 효과, 보상 데이터는 없다.

