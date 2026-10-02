---
aliases: [Dorothy_Skill_1]
tags: [character/skill]
implementation: partial
updated: 2026-10-02
---

# Dorothy Skill 1

> [!partial] 🟡 부분 구현
> SkillData 자산은 있으나 이름·설명·효과가 비어 있고 대원에게 연결되지 않았다.

| 항목 | 값 |
|---|---|
| Asset | `Dorothy_Skill_1.asset` (JSON ID `dorothy_skill_1`) |
| 표시 이름 | 미입력 |
| 설명 | 미입력 |
| Max SP | 2 |
| 충전 타입 | Natural |
| 1회 충전량 | 1 |
| 대원 연결 | 없음 |
| 실제 효과 | 미구현 |

## 완료 조건

- [ ] 이름과 설명 결정
- [ ] [[캐릭터/아군/도로시 프랭크스|도로시 프랭크스]]에 해당하는 `operators.json`의 `skillIds`에 연결
- [ ] 자연 회복 이벤트 호출 지점 연결
- [ ] 실제 `SkillEffect` 구현
- [ ] 스킬 발동 UI와 입력 흐름 구현

