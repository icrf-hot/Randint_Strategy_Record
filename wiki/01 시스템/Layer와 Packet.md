---
tags: [system/progression, economy/packet, system/layer]
status: active
implementation: implemented
updated: 2026-10-13
---

# Layer와 Packet

## 확정 규칙

| 항목 | 값 |
|---|---:|
| 새 런 Packet | 30 |
| 시작 Layer / 경험치 | 1 / 0 |
| 전투 승리 Packet | 8 |
| 전투 승리 경험치 | 12 |
| 첫 Layer 요구 경험치 | 16 |
| 이후 요구 경험치 성장 | 1.5배 복리 |
| Layer 한 단계 상승 시 추가 Packet | 10 |

Packet·Layer·경험치는 정수다. 요구 경험치는 `ceil(16 × 1.5^(현재 Layer - 1))`이며 16, 24, 36, 54, 81, 122, 183, 274 순이다. 복리 결과는 항상 짝수/정수가 아니므로 소수는 올림한다. 매 단계에서 반올림한 값을 다시 곱하지 않고 원래 복리식을 계산한다.

경험치가 요구치 이상이면 레벨업한다. 사용한 요구치를 빼고 나머지는 이월하며, 한 번에 여러 단계가 오르면 단계마다 10 Packet을 준다. 현재 Layer 보너스는 재화만 있고 능력치/기타 이점은 없다. 패배에는 승리 보상을 지급하지 않는다.

## 구현

- `RunProgressionSettings`: Inspector 설정 Asset. `Assets/Shop/Data/RunProgression.asset`에 확정 수치를 저장한다.
- `PacketWallet`: 잔액 조회, `TryAdd`, `TrySpend`, `CanAfford`, `Clear`. 음수·잔액 부족·정수 범위 초과 실패 시 잔액을 유지한다.
- `RunProgression`: Layer, 잔여 경험치, Packet 지갑과 승리 보상 처리. 보상 전체가 가능한지 검사한 뒤 상태를 반영한다.
- `MapRunState.Progression` / `Packets`: 정적 런 상태. 씬 전환 중 유지하며 디스크 저장은 아니다.
- Title은 `StartNewRun(settings)`으로 기존 런을 비우고 30 Packet을 지급한다. 맵/전투 씬 직접 실행도 `EnsureStarted(settings)`으로 한 번 초기화한다. 맵 복귀마다 30 Packet을 다시 주지 않는다.
- `BattleExecuteManager.FinishBattle(Success)`에서 보상을 한 번 지급한다. 기존 `isFinishingBattle` 중복 종료 차단을 사용한다. Failure 분기는 지급하지 않는다.
- `RunProgressionHUD`가 맵/전투 오른쪽 위에 Packet, Layer, 현재/요구 EXP를 TMP로 표시한다. 값이 변할 때만 텍스트를 갱신하며 로딩/결과 화면 동안 숨긴다. 입력 Raycast를 받지 않는다. 개별 승리 보상은 Debug 로그로 확인하며 결과 화면 보상 내역 표시는 아직 없다.

## 검증 상태

- `RunProgressionVerification.Run`: 컴파일 및 지갑 경계값, 복리 올림, 초기 지급, 두 번 승리, 정확한 요구치, 다중 레벨업, 오버플로 실패 시 상태 보존, 새 런 초기화 통과.
- 예: 두 번 승리 후 Layer 2 / 잔여 경험치 8 / Packet 56.
- Title·성공·실패 실제 씬 연결의 제한 플레이 검증 모두 종료 코드 0. 새 런 30 Packet, 승리 후 38 Packet/EXP 12 유지, 패배 후 30 Packet/EXP 0을 확인했다.
- `CodexRunHUD.log`: 맵/전투 한 번 왕복으로 TMP 내용·폰트 참조·입력 비차단·승리 후 표시값 검증 통과. 해상도별 픽셀/겹침 시각 QA는 미실시다.

## 남은 작업

- Cache 구매·보유·강화 연결, Packet·EXP 획득량 버프 적용
- 결과 화면 보상 내역 표시와 해상도별 HUD 시각 QA
- 지급/경험치/보유 Cache의 디스크 저장과 이어하기 정책
- Cache 지속 범위 및 직군/세부직군 모델 확정

## 관련 문서

- [[01 시스템/상점 시스템|Cache 상점]]
- [[01 시스템/전투 시스템|전투 결과]]
- [[TODO LIST/TODO LIST|TODO]]
