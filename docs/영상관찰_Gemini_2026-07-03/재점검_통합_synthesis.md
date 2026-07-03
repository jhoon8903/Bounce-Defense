> **[사용자 결정 반영 2026-07-03]** 아래는 6차원 교차검토 워크플로의 원본 통합안이다. §4는 D-5 트리아지(스파인 우선)를 권고하나, **사용자는 "전체 스코프 도전"을 선택**했다. 따라서 §4의 "자를 수 있음/컷" 항목은 *컷이 아니라 시도 대상*이며, 그 순서는 **시간 부족 시 방어할 컷 순서(안전망)**로만 참고한다. 확정 결정: ①스코프=전체 도전(단 버림용 APK 조기 빌드는 디리스킹으로 유지) ②카드=킬 기반 XP 레벨업 ③보드=9열 유지 ④풀=데이터주도 단일 풀. 이 결정들의 개발플랜 반영본은 `docs/개발플랜.md §11-11` 참조.
>
> **검증 상태:** 이 문서의 최우선 주장(볼→데미지 배선 끊김·빌드씬 경로 깨짐·은행가 반올림)은 리드가 실제 코드/파일로 직접 재검증했다(Resolve 호출자 0·new HitContext 0·BallController 데미지참조 0 / 빌드설정 guid 8c9c…≠실제 GameScene guid 0f5d… / DamageStages.cs:59 Mathf.RoundToInt). 전부 사실.

# 개발기획 재점검 결과 (2026-07-03, Gemini 영상관찰 반영)

## 1. 한눈에 — 재점검 결론

- **코어 방향은 유지, 단 한 곳이 죽어 있다.** 실시간 연속 하강(턴게이트 폐기, §11-9) 결정은 GROUND TRUTH와 정합하며 옳다. 볼 물리·그리드·데미지 "수학 코어"·DI·이벤트 허브 골격도 재사용 가능하게 잘 깔렸다. **그러나 §11-9 볼 MVC 재작성에서 볼→데미지 연결선이 통째로 끊겼다** — `DamageResolver.Resolve` 호출자 0개, `new HitContext` 0건. 지금 살아있는 볼은 적을 때려도 0 데미지다. 이게 최우선 결함이다.
- **가장 큰 리스크는 시간이다.** 냉정히 계산하면 남은 스코프(플랜 §6 자체 tight 추정 7~9.5일)가 실질 가용일(7/4~7/7 + 반나절 ≈ 4~4.5일)의 **1.6~2.4배**다. 게다가 APK를 한 번도 안 빌드했고 빌드설정이 깨져 있으며(잘못된 씬 경로), 멀티풀 블로커·EditMode 테스트 부재는 이 추정에 반영조차 안 됐다. **지금 자르지 않으면 7/7에 "빌드가 반쯤 깨진 채" 늦은 서프라이즈를 맞는다.**
- **결론: 방향 수정 아님, 실행 트리아지가 핵심.** (1) 볼→데미지 심 복원 → (2) 승패 스파인(웨이브·베이스HP·결과팝업·재시작) 완결 → (3) 버림용 APK 조기 빌드 → (4) 스킬은 계열 커버 6~7개로 트리아지 → (5) 게임필은 고빈도 저비용(색트레일·데미지숫자·히트플래시) 우선. 히트스톱은 실게임에 없으니 뺀다.

---

## 2. Gemini 영상관찰이 바꾼 것 (플랜 §2 대비)

정적 시각 관찰만 채택(시간·메카닉 구조 추론은 GROUND TRUTH 우선으로 무시).

| 항목 | 기존 플랜 | Gemini 관찰(정적) | 조치 |
|---|---|---|---|
| 베이스HP | 300 (계획값) | **300** (사신 하단 초록바 숫자, 결과화면 'Remaining HP 100%') | **일치 확정.** divergence 없음 — 값은 그대로, 구현만 남음 |
| 카드 트리거 | 웨이브 전멸=1카드 | **킬→XP게이지→레벨업**(Lv2~Lv12 약 11회, 웨이브 중간에도) | ⚑ 결정 필요(§6). 둘 다 스펙 호환, 페이싱 체감 다름 |
| 적 HP 표기 | 개별 HP바 | **몸체에 숫자**로 표시, 피격 시 숫자 차감 | ⚑ 결정 필요 — 숫자 채택 권장, EnemyView를 Observable HP 구독으로 저비용 전환 가능하게 |
| 보드 열 수 | 구현 9열 (리서치 ~7·8) | **~8열** | ⚑ 결정 필요(지금이 최저 변경비용) |
| 볼 트레일 | "볼 트레일"(뭉뚱그림) | **타입별 색 분리**(노멀=흰/레이저=청록/파이어=주황/고스트=보라/클러스터=녹색) 6색 확정 | 색을 BallConfig 데이터화 — 거의 0공수, 가독성 즉시 상승 |
| 충돌 피드백 | 없음 | 충돌점 **흰 원형 스파크** '팡'(스쿼시/스트레치는 **없음**) | 스파크 추가, 변형 애니는 넣지 말 것 |
| 적 사망 | SFX만 | **흰 플래시→고스트 잔상→회색 파편**(사망원인별 이펙트 중첩) | OnKill 풀드 사망VFX 명세 추가 |
| 히트스톱 | 가산점 주스팩에 포함 | **의도적 배제/최소화**(다수 볼이 수십 타→넣으면 늘어짐) | **주스팩에서 제거**(대형스킬 킬 1회 옵션만) |
| 화면흔들림 | 규율 없이 나열 | **일반 히트 무셰이크**, 레이저·폭발 등 대형 AoE에만 짧게 | OnKill 대형스킬만 게이팅 |
| 데미지 숫자 | "백/주황"만 | 팝→상승→페이드 커브, 폭발=크고 붉음, 소스별 색 | 커브+크리 크기/색 구분 명세 |
| 결과화면 별점 | 가산점 소재 언급 | 'Stage Clear'+**별3**+'Remaining HP 100%' | 잔여HP%→별점 매핑(베이스HP 구현 시 저비용 가산점) |
| 실게임 잉여 스킬 | 스펙 우선 폐기 | Iron Boots·Tortoise Shell 등 다수(스펙 5종보다 큼) + 카드표기≠실뎀 | **스펙 우선 유지 옳음.** 레이저 카드 7행/11볼이 우리 이중데미지 모델을 오히려 검증 |
| 단검 전/후면 | 스펙대로 조건부 채택 | 관찰된 자수정 카드=**'Crit +10%' 무조건형**(전면 문구 없음), 에메랄드 미관찰 | **스펙 우선 유지.** README에 "영상 빌드엔 단검 조건 미확인→작성자 결정"으로 정직 기술 |

---

## 3. 반드시 수정할 것 (severity순)

### 🔴 [critical] 볼→데미지 심이 죽어있다 — 파이프라인 미연결
`BallController.OnFixedTick`(라인 91-123)은 바닥수집·OOB·바운스캡만 처리하고, 모터가 정확히 세팅하는 `result.HitEnemy/HitBlock`(`KinematicRaycastMotor.cs:103-113`)을 **완전히 무시**한다. 결과: `DamageResolver.Resolve` 호출자 0, `new HitContext` 0건, BallController 생성자는 resolver도 hub도 안 받음(라인 66). collider→IDamageable 조회 메커니즘도 없음. **이게 없으면 킬→킬카운트→웨이브 진행→베이스HP 실패의 전 사슬이 작동 안 한다.** '데미지 파이프라인 골격 완료'는 구 모놀리식 Ball 시절 호출이고 지금은 단절 상태다.

→ **Phase 2 착수 전 최우선.** BallController에 DamageResolver+CombatEventHub 주입 → HitEnemy/HitBlock 시 collider→IDamageable 확정 → HitContext(Direct, sourceType, target, GetDamage(level), canCrit=true, hitPoint, hitNormal) 생성 → Resolve → `hub.RaiseHit`. 볼별 히트 dedup HashSet으로 한 바운스=한 히트 보장(`_ignoredThisStep`과 정합). **먼저 볼→적 1처치가 실제 HP 감소하는 수직슬라이스로 검증**한 뒤 스폰/웨이브로 진행.

### 🔴 [high] CombatEventHub 4개 이벤트 중 3개가 안 울린다
`RaiseHit/RaiseLaunch/RaiseWallBounce` 호출자 0(유일 발화=`Enemy.RaiseKill`). 모터는 벽반사를 감지해 `BallController.RegisterBounce`로 카운트까지 하지만(라인 116) `RaiseWallBounce`는 안 부른다. → 레이저·클러스터(OnHit)·마법거울(OnWallBounce 볼별 무장)이 **얹을 자리는 있으나 불을 켤 소스가 없다.** 위 심 복원과 동시에 세 이벤트를 BallController에서 발화하도록 배선. OnKill만 살아있어 '마지막 성냥'을 Phase4 첫 스킬 레퍼런스로 삼으면 나머지 훅 패턴이 복제됨.

### 🔴 [high] 스킬 확장 전에 HitContext를 한 번에 뚫어라 (재작업 방지)
현재 HitContext에 **볼 인스턴스 ID 없음**(마법거울 볼별 무장/소비 매칭 불가 — BallWallBounceInfo엔 BallInstanceId 있으나 전파 안 됨), **전/후면 정보 없음**(자수정/에메랄드 단검 크리 조건 평가 불가). 크리 기본값 0%라 단검이 유일한 크리 소스인데 그 발동조건을 파이프라인이 표현 못 한다. → **스킬 10개 작성 직전 1회로** `int SourceBallId` 추가(필수). 전/후면은 `HitNormal.y` 부호로 단순화(전면=y<0/후면=y>0), 필요시 HitSide enum 편의필드. 플랜 line73의 forward+콘각은 폐기. 지금 안 뚫으면 스킬 붙일 때 코어 HitContext를 뜯게 된다.

### 🔴 [critical] APK 미빌드 + 빌드설정 깨짐
EditorBuildSettings가 **존재하지 않는** `Assets/Scenes/GameScene.unity`(enabled:0)를 가리킨다 — 실제 씬은 `Assets/Project/GameScene.unity`. 지금 빌드하면 빈/틀린 씬이 나온다. IL2CPP 매니지드 스트리핑이 SO/인터페이스로 런타임 생성되는 IDamageModifier/IBallModule를 '미사용'으로 오판해 스트립할 위험, 새로 임포트된 JMO Cartoon FX·Layer Lab 서드파티도 AOT 이슈원. **스킬 다 얹은 7/7에 처음 발견하면 회복 불가.** → 다음 24시간 내 '현재 상태' 버림용 IL2CPP APK를 실기기까지 1회 관통. 빌드씬을 실제 경로로 교정, link.xml로 ModifierRegistry/모듈 계열 Preserve.

### 🔴 [high] 3택 규칙·10스킬·상태이상 서브시스템 코드 0
Game.Roguelike/Game.Skills 폴더 자체가 없음(SkillDefinition/CardDrawService/PlayerLoadout/IBallModule/StatusEffect 전부 0건). 리뷰어 3순위 축(스펙 기능 정확성)이 통째로 미착수. **강점:** 데미지 공식(6단계)·10스킬 수치·3택 5규칙은 플랜 문서에 스펙과 1:1 정확히 담겨 있음(21/24/27, 25/37/50 등 전부 일치 확인). 원가는 '수치 입력'이 아니라 Burn 독립타이머 스택·Freeze·Cluster 재귀·Laser 행조회 **서브시스템 5~6개**다.

### 🟠 [medium] Mathf.RoundToInt = 은행가 반올림 (문서/테스트 불일치 지뢰)
`RoundDamageStage`가 round-half-to-even을 쓴다. 아이스L1 32.5→코드 32(직관 33), 레이저L1 크리 16.5→16(17), 클러스터L1 크리 40.5→40(41). README에 데미지표+손검산을 실을 때 사람이 half-up으로 계산하면 **버그처럼 보인다.** → 코드/README표/테스트 기대값을 **동일 함수로 산출**해 불일치 0. 정책 택일(현행 유지 명기 또는 `Floor(x+0.5)`)은 ⚑결정(§6).

---

## 4. D-5 실행계획 (크리티컬 패스)

가용: 7/4~7/7 4일 + 7/8 오전 반나절. **"전부 견고하게"는 산술적으로 불가능.** 아래 순서로 승패 스파인을 먼저 완결하고, 스킬/게임필을 시간이 허락하는 만큼 얹는다.

### 반드시 완성 (필수구현 = 컷 금지)
| # | 작업 | 러프공수 | 비고 |
|---|---|---|---|
| P0 | **볼→데미지 심 복원 + 3이벤트 발화 + HitContext 확장(SourceBallId)** | 0.5일 | §3 최우선. 이후 전부의 전제 |
| P0.5 | **버림용 IL2CPP APK 실기기 1회 관통** | 0.5일 | 빌드씬 교정+link.xml. 표면 작을 때 |
| P1 | **Enemy MVC 승격**(Model:Observable+IDamageable / View:HP숫자+히트플래시 / Controller:IClock 하강+factory+pool) | 1일 | 정적 Enemy[] 배선 폐기. 상태 컨테이너(List<StatusInstance>)를 **지금** 심어둠 |
| P2 | **웨이브 시스템**(EnemyDefinition/WaveDefinition/StageDefinition SO + WaveController가 TrySpawnTop에 큐 흘림) + **베이스HP 모델·HUD·방어선 판정** + **결과팝업(성공/실패)·재시작** | 1.5일 | 승패 스파인. 방어선=적 월드Y 임계 통과 감지(연속 하강). Block_NxM 프리팹에 콜라이더+Enemy 얹기 |
| P3 | **3택 드래프트 코어**(CardDrawService 4규칙+캡4/2+<3폴백, 순수 C#) + 3카드 UI | 1일 | 시드 EditMode 테스트로 잠금 |

### 자를 수 있음 (가산점 — 시간순 방어)
- **스킬 트리아지:** 10개 전부 SO 데이터 등록(스펙값, 쌈), 견고함은 **계열 커버 6~7개** — Warm Tin/Magic Mirror/Amethyst/Emerald(순수 모디파이어=쌈)+Fire/Ghost/Laser 우선. Ice·Cluster·Last Match 엣지는 '가장 단순한 정답'. README에 패밀리별 성숙도 정직 기술. (리뷰어 우선순위상 스펙정확성은 아키텍처보다 **아래** — 11번째 스킬=SO1+모듈1 패턴 증명이 10개 갈아넣기보다 강한 신호)
- **게임필(§5):** 색트레일·데미지숫자·히트플래시·스파크·사망VFX (JuiceController 단일 구독자). 히어로 스킬 VFX 1개.
- **최소 SFX 6종**(CC0) — 컷 1순위 아님, 무음 영상은 미완성으로 읽힘.
- **EditMode 테스트:** 풀커버리지 포기, **최고신호 2클래스만**(DamageResolver 6단계·반올림 / CardDrawService 4규칙).

### 컷 (명시적 포기, README '알려진 한계'에 정직 기재)
결정론 재현영상, 셰이크 정교화, 카드/결과 트랜지션 폴리시 대부분, 잉여 스킬, 디버그 패널(단 검증 원가 회수용이면 P3에서 얇게).

> **검증 규율:** 각 Phase 최종 검증은 MCP 헤드리스 펌프가 아니라 **Daniel이 에디터 포커스로 직접 플레이하는 human-in-loop 1회**로 게이트(비포커스 시 Update 정지 → UI흐름·실시간 상태틱에 취약). APK 나오는 즉시 실기기 볼5+더미적 60초 연속 플레이로 프레임/터널링 확인.

---

## 5. 게임필 재현 우선순위 (1순위 평가축)

Gemini 정적 관찰 기준. **핵심 아키텍처:** CombatEventHub(4이벤트)+GamePool이 이미 DI 싱글톤이라 **JuiceController 단일 구독자 하나로 코어 0수정 전 주스 구현** → 게임필(1순위)+개방-폐쇄 아키텍처(2순위)+폴리시(5순위) 동시 달성. README 핵심 서사.

| # | 연출 | 임팩트 | 공수 | 근거 |
|---|---|---|---|---|
| 1 | **타입별 색 트레일**(6볼 고유색) | 높음(수십 볼 가독성·속도감) | **거의 0**(프리팹 TrailRenderer+BallConfig 색) | GeminiB 13-14/C 102-107 |
| 2 | **데미지 숫자**(팝→상승→페이드, 크리=색+크기) | 높음(손맛+크리 시스템 가시증명) | 낮음(풀드) | GeminiB 22-25/C 97-101 |
| 3 | **히트플래시**(피격 적 흰 번쩍) | 높음 | 낮음 | OnHit 구독 |
| 4 | **충돌 스파크**(흰 원형 팡, 스쿼시 없음) | 중 | 낮음(풀드 1샷) | GeminiB 16-17 |
| 5 | **적 사망 VFX**(흰 플래시→고스트→회색 파편) + **히어로 스킬 1개**(레이저 관통빔 또는 폭발) | 중~높음(영상 와우샷) | 중(폭발은 Cartoon FX 프리팹 재사용 저비용) | GeminiB 38/44-45, Top5#2 |

**규율(반드시):** 히트스톱=**넣지 않음**(대형킬 옵션만). 화면흔들림=일반 히트 제외, **대형 AoE 킬에만** 짧게(Cinemachine 미설치→카메라 transform 직접 셰이크). 트윈수단=DOTween 설치 말고 **IClock/UpdateLoop 기반 미니 트윈 헬퍼**(일시정지 인지, GameSpeed=0에 자동 정지).

---

## 6. ⚑ 사용자가 결정할 것

### 결정 A — 카드 트리거: 킬 기반 XP 레벨업 vs 웨이브 전멸=1카드
실게임은 킬→XP게이지→레벨업(영상 Lv2~12 약 11회, 웨이브 중간에도 빠르게). 플랜은 전멸=1카드로 모델링. **둘 다 스펙("처치 수 충족 시 3택") 준수** — 페이싱 체감만 다름.
- **질문:** 리뷰어가 레퍼런스 영상 대비 '잦은 드래프트 카덴스'를 기대한다고 보나, 아니면 단순·확실한 웨이브당 1카드가 D-5에 더 안전한가?
- **되돌리기 비용:** 낮음(둘 다 killsRequired/XP임계 데이터). **지금 아무거나 고르고 README에 해석 명시하면 됨.** 과분석 불필요.

### 결정 B — 반올림 정책: 은행가(현행) vs half-up
`Mathf.RoundToInt`는 .5를 짝수로 보냄. half-up과 몇몇 크리 조합에서 1 차이.
- **질문:** README에 데미지표+손검산을 실을 때 리뷰어가 손으로 계산할 값과 어긋나면 버그로 보인다. 어느 쪽이든 정당하나 **관건은 정답이 아니라 코드/README/테스트 3자 일관성.**
- **되돌리기 비용:** 낮음(한 줄 헬퍼 교체). **권고: 그냥 하나 정하고 3자를 동일 함수로 산출.** 5분 결정.

### 결정 C — 보드 열 수: 9(현행) vs 8(관찰)
구현 9열, 씬 벽도 9폭으로 자기일관. 관찰·스토어는 ~8열.
- **질문:** 픽셀 충실도(리뷰어가 영상 대조)를 중시하나, 이미 배선된 9열 유지가 나은가?
- **되돌리기 비용:** **지금은 쌈**(GridConfig+벽+디버그뷰만), 웨이브 스폰테이블이 열좌표 잡으면 급증. **Phase 2 착수 전에 결론낼 것.** 8열로 낮추거나, 9열 유지+README에 '작성자 설계=9열' 명시.

### 결정 D — GamePool 키잉 (아키텍처, §7과 연동)
볼 6종이 전부 BallView 타입이라 typeof 1키로 충돌(_pools=1 실측). **이 결정은 Phase4 스킬볼이 아니라 Phase2 적 스폰(EnemyView 다수)에서 먼저 강제된다.**
- **선택지:** (C, 권고) **데이터주도 단일 풀** — 볼=BallView 1풀+타입별 데이터 SO, 적=EnemyView 1풀+적데이터 SO. 실게임상 모든 볼이 같은 원형이라 정합, GamePool 무수정. / (A) 볼마다 빈 View 서브클래스 = 무행동 6클래스 냄새이나 프레임워크 무수정 폴백. / (B) poolName 문자열 키 재작성 = WigglePuzzle 프레임워크 코어 훼손+Get<T>() 타입안전 파괴, MEMORY '다니엘 기존작 따르기'에 역행 → **비권고.**
- **되돌리기 비용:** 잘못 고르면(A) 6서브클래스+프리팹 재작업 반나절. **C를 지금 확정.**

---

## 7. 아키텍처 조치 (Phase 2 착수 전)

**미리 손볼 것 (지금 안 하면 Phase4에서 코어를 뜯게 됨):**
1. **볼→데미지 심 복원 + 3이벤트 발화 + HitContext.SourceBallId 추가** (§3 P0). 전/후면은 HitNormal.y 부호로 단순화.
2. **GamePool 키잉을 옵션 C로 확정** (결정 D). Phase2 적 스폰이 이걸 즉시 강제.
3. **EnemyModel에 상태 컨테이너(List<StatusInstance>)를 지금 심기.** Phase4 번/냉동에서 EnemyModel을 안 뜯으려면 필수. 냉동 슬로우용 statusMultiplier 자리를 하강 속도에 남겨둠.
4. **빌드씬 경로 교정 + link.xml Preserve** (APK 조기빌드 전제).
5. **테스트 asmdef 1개 재생성**(§11-7에서 삭제됨) — 최고신호 2클래스만.

**그대로 진행 (강점, 손대지 말 것):**
- DI 스캐폴드(GameLifetimeScope): SO등록+싱글톤+빌드콜백 패턴이 웨이브/드래프트/스킬 서비스를 자연 흡수. StageDefinition+WaveController+CardDrawService(IRandom 이미 등록)+PlayerLoadout+SkillDatabase가 동일 형태로 붙음.
- IClock.GameSpeed=0 일시정지: BallController가 GameDeltaTime을 받으므로 '레벨업 시 게임 정지'가 클록 설계만으로 대응됨. 웨이브/드래프트/상태 컨트롤러 모두 이 규약 따르게 할 것.
- 데미지 수학 코어(Base→Additive%→Crit→Round→Apply)와 ModifierRegistry 확장 심: 가산%·크리% 계열 4종은 진짜 register-only.

**Phase4 직전 리팩터(Phase2를 막지는 않음):**
- BallController의 4개 병렬 딕셔너리(model/view/motor/collecting)를 단일 `BallInstance` 객체로 통합(+hitDedup/mirrorBuffer/sourceType). Spawn에 BallSourceType+config 파라미터 추가(현재 Normal 하드코딩).

**결정론 재현영상을 가산점으로 밀 경우에만:** GameDeltaTime=Time.deltaTime*gameSpeed라 물리가 가변 dt → 프레임레이트 따라 궤적/충돌순서·RNG 롤 순서가 달라짐. 물리/데미지 틱을 fixedDeltaTime 고정스텝으로. **단순 데모면 현상 유지(되돌리기 쉬움).** — 이건 '재현성 셀링' 목표 강도에 달린 선택이라 컷 후보.