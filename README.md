# 통통 디펜스 : 핀볼 마스터 — Stage 1 재구현

PurpleCow 클라이언트 프로그래머 채용 과제. 〈통통 디펜스: 핀볼 마스터〉 1스테이지의 핵심
게임플레이(볼 발사·물리 반사·데미지, 12웨이브, 로그라이크 3택, 승패 처리)를 Unity로 재구현한다.

- **엔진**: Unity 6000.3.10f1 · **타깃**: Android
- **핵심 설계 원칙**: *데미지/스킬/드로우 수학은 Rigidbody를 절대 만지지 않는다* — 볼 이동은
  순수 레이캐스트 모터(등속·터널링 불가·미리보기=실제 시뮬레이션), 데미지는 순서화된 파이프라인.

---

## ⚠️ 구현 현황 (진행 중 — 6단계 로드맵 중 Phase 1 완료)

이 저장소는 개발 중이며, 아래 표가 현재 실제 구현 상태다. 미구현 항목을 완료로 표기하지 않는다.

| 영역 | 상태 | 비고 |
|---|---|---|
| 볼 물리(발사·반사·내부 바운스) | ✅ 구현·실측 | `KinematicRaycastMotor`, solid `BoxCollider2D` 내부 반사 |
| 데미지 파이프라인(6단계, §데미지 공식) | ✅ 구현·실측 | `DamageResolver` + `ModifierRegistry`(빈 상태 검증됨) |
| DI/오브젝트풀/이벤트 허브 | ✅ 구현 | VContainer, `IPool`, `CombatEventHub` |
| 캐릭터 조준 리그(터치) | ✅ 구현·실측 | `AnchorView` 터치 게이트 회전 |
| 궤적 미리보기 | ✅ 구현 | 모터와 동일 수학(`MotorGeometry` 공유) |
| 12웨이브 / 몬스터 스폰 | ⏳ 미구현 | Phase 2 |
| 적 하강·충돌·처치 | 🟡 부분 | 더미 Enemy 처치까지 검증(수직 슬라이스), 정식 스폰 미구현 |
| 로그라이크 3택 / 10스킬 | ⏳ 미구현 | Phase 3~4 |
| 승패 결과 팝업·재시작 | ⏳ 미구현 | Phase 2 |
| 전투 파이프라인 GameScene 배선 | ⏳ 미구현 | 현재 검증은 수직 슬라이스 씬에서 수행 |
| 플레이 영상 / APK | ⏳ 미구현 | 제출 전 산출 |

---

## 1. 실행 방법

1. Unity **6000.3.10f1** 로 이 프로젝트(`Bounce-Defense/`)를 연다.
2. 패키지는 `Packages/manifest.json` 기준 자동 복원(VContainer, Input System 포함).
3. 씬을 연다:
   - `Assets/Project/GameScene.unity` — 본 게임 씬(현재 아레나 벽 + 캐릭터 셋업까지).
   - `Assets/Project/VerticalSlice/Phase1_VerticalSlice.unity` — **코어 루프 실측용**.
     노멀볼 발사 → 벽 바운스 → 더미 적 처치(데미지 파이프라인 경유)가 Play Mode에서 동작.
4. Play. (입력: 드래그로 조준, 놓으면 발사 — 새 Input System 기반, 마우스·터치 통합.)

> 현재 전투 파이프라인(런처/풀/DI 스코프)은 수직 슬라이스 씬에 배선되어 있고, 본 `GameScene`
> 배선은 Phase 2에서 통합한다.

---

## 2. 주요 구현 내용

### 아키텍처

- **논리 레이어(네임스페이스)로 관심사 분리**: `Game.Core`(IRandom/IClock/풀), `Game.Combat`
  (데미지 파이프라인), `Game.Events`(이벤트 허브), `Game.Runtime`(볼/런처/모터/조준),
  `Game.Objects`(캐릭터 뷰), `Game.Skills.*`·`Game.Roguelike`(예정).
- **단일 `Assembly-CSharp`**: 과제 규모에서 asmdef 경계는 이득보다 마찰이 커서 제거하고
  네임스페이스로 레이어를 표현한다. 의존성 방향은 코드 리뷰로 확인(예: 데미지 로직은
  `UnityEngine.Rigidbody`를 참조하지 않음).
- **DI = VContainer**: 단일 `GameLifetimeScope`가 `IRandom`/`IClock`/`IPool`/`DamageResolver`/
  `CombatEventHub`를 배선하고 씬 컴포넌트에 주입.

### 볼 물리 — `KinematicRaycastMotor` (Rigidbody 미사용)

- 매 스텝 `CircleCast` + 잔여거리 소비 루프 + `Vector2.Reflect`. **등속 정확**, **터널링 불가**,
  **궤적 미리보기 = 실제 시뮬레이션**(`TrajectoryPreview`가 모터와 동일한
  `MotorGeometry` 수학 공유 → 미리보기와 실제 궤적이 어긋나지 않음).
- **아레나 벽 = 단일 solid `BoxCollider2D` 내부 반사**: solid 박스는 레이캐스트로 내부를 볼 수
  없어(내부 캐스트가 거리 0을 반환) `Physics2D.OverlapPoint`로 "공이 박스 안에 있음"만 판별하고,
  안쪽 AABB(반지름만큼 수축)까지의 거리를 해석적으로 계산해 축 반전으로 반사한다. 4벽 밀폐.
  4각도 × 900~1200스텝 실측: 전부 아레나 안에 갇히고 속도 일정, 탈출 0회.

### 데미지 파이프라인 — `DamageResolver` (순서화·개방-폐쇄)

- 6단계: **Base → Additive% → 치명타 확률(roll) → 치명타 데미지(×1.5) → 반올림(최종만) → Apply**.
- 과제 기본값 반영: 노멀 볼 데미지 **8**, 기본 치명타 확률 **0%**, 치명타 데미지율 **+50%(×1.5)**.
- 스킬은 `DamageResolver`를 수정하지 않고 `ModifierRegistry`에 `IDamageModifier`를 **등록만**
  하여 확장(개방-폐쇄). 스킬 0개인 현재는 파이프라인이 "빈 상태"로 통과함을 실측(Additive%=0,
  크리 0%)으로 검증 — 스킬 추가 시 이 지점에 얹힌다.
- 이벤트는 `CombatEventHub`(OnLaunch/WallBounce/Hit/Kill) 옵저버로 발행 → 사운드/연출/집계가
  데미지 로직과 분리.

### 캐릭터 조준 — `AnchorView` (모바일 터치)

- 리그: `Char`(Body, 고정) → `AnchorView`(피벗) → `Head`/`Staff`. Anchor만 Z 회전하고 자식이
  따라 돌아 조준 방향을 바라본다.
- **모바일 규칙**: 각은 **누르는 중에만** 갱신(호버 추적 아님), 손을 떼면 마지막 각 유지.
  발사와 동일한 상단 호(15°~165°) 클램프 → 캐릭터가 실제 발사 방향과 항상 일치.

---

## 3. 가산점 구현

> 과제의 "구현 제외" 항목(튜토리얼·배속·보스·자동조준·선택지 다시뽑기·융합)은 구현하지 않는다.

- **반사 궤적 미리보기**(`TrajectoryPreview`): 조준 중 실제 바운스 경로를 실시간 표시. 모터와
  **동일 수학**을 공유해 미리보기와 실제 궤적이 픽셀 단위로 일치 — 조준 게임필의 핵심.
- (예정) 스킬·게임필·연출 등 추가 항목은 구현하는 대로 이 절에 상세 기재한다.

---

## 4. AI 활용 여부

이 프로젝트는 **Claude Code (Claude Opus) + MCP for Unity**를 적극 활용해 개발했다.

- **활용 방식**: 아키텍처·설계 결정 검토, C# 구현, Unity 에디터 자동 조작(씬/컴포넌트/프리팹),
  그리고 **`execute_code`로 에디터 안에서 물리·수학을 직접 실측 검증**(예: 벽 내부 반사가
  실제로 공을 가두는지 수백 스텝 시뮬레이션, 조준 각 계산 검증).
- **판단·결정은 개발자가 수행**: 벽 충돌 방식(박스 유지 + 모터 수정), 아레나 밀폐, asmdef 제거
  등 되돌리기 어려운 결정은 트레이드오프를 놓고 개발자가 선택했다.
- **검증 원칙**: 컴파일/콘솔 클린 확인에 그치지 않고 Play Mode·`execute_code`로 실제 동작을
  관측해 확인(정적 검사 통과 ≠ 동작 보장).

---

## 부록 — 폴더 구조

```
Assets/Project/
  Scripts/
    Game.Core/        IRandom·IClock·오브젝트풀·Observer
    Game.Combat/      HitContext·DamageResolver(6단계)·ModifierRegistry
    Game.Events/      CombatEventHub
    Game.Runtime/     Motor(KinematicRaycastMotor·TrajectoryPreview·MotorGeometry)·Ball·Launcher·Aim·GameLifetimeScope
    Game.Objects/     캐릭터 뷰(AnchorView 등)
    Game.Skills.*/    (예정) 스킬 데이터·모듈
    Game.Roguelike/   (예정) 로드아웃·카드 드래프트
  GameScene.unity          본 게임 씬
  VerticalSlice/           코어 루프 실측 씬
  Prefabs/ · Configs/
docs/개발플랜.md           설계 계획 + 세션 개발 로그(§11)
```

설계 근거·결정 이력·세션별 진행은 `docs/개발플랜.md`에 상세 기록되어 있다.
