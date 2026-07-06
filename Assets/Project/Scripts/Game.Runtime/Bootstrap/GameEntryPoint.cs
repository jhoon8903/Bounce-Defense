using Game.Core.Pool;
using Game.Runtime.Combat;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Progression;
using Game.Runtime.Skills;
using Game.Runtime.Stage;
using Game.Runtime.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Runtime.Bootstrap
{
    // GameLifetimeScope가 배선 중 계산한 씬 파생값(초기화에 필요)을 GameEntryPoint로 넘기는 전달체.
    // 씬 오브젝트/컴포넌트 자체가 아니라 '값'만 담으므로 순수 C#(직렬화 불필요).
    public sealed class BootstrapContext
    {
        public Transform PoolParent;
        public Vector2 GridOrigin;
        public Vector2 DefensePoint;
        public bool CharReady;
        public bool DraftReady;
    }

    // 순서 의존 초기화(생명주기)를 조립 루트(GameLifetimeScope)에서 분리 — WigglePuzzle GameEntryPoint 패턴.
    // VContainer가 컨테이너 빌드 후 Start()를 1회 호출한다. 배선(Configure) ≠ 초기화(여기).
    public sealed class GameEntryPoint : IStartable
    {
        private readonly IObjectResolver _resolver;
        private readonly BootstrapContext _ctx;

        public GameEntryPoint(IObjectResolver resolver, BootstrapContext ctx)
        {
            _resolver = resolver;
            _ctx = ctx;
        }

        // 풀 활성 → 볼/스킬/피드백/VFX → 그리드(원점) → 적(방어점) → 스테이지(웨이브 시작) → 결과 UI → 조준/드래프트.
        public void Start()
        {
            IPool pool = _resolver.Resolve<IPool>();
            // 볼은 BallFactory가 타입별 풀 소유 → GamePool은 EnemyView + DamageTextView 담당(BallView 제외, §11-9).
            pool.Activate(_ctx.PoolParent, typeof(EnemyView), typeof(DamageTextView));
            _resolver.Resolve<BallController>().Initialize();
            _resolver.Resolve<SkillRuntime>().Initialize();     // 로드아웃 구독 + 초기 로스터(노멀 5). BallController 직후.
            _resolver.Resolve<HitFeedbackController>().Initialize(); // 데미지 숫자 + 화이트 플래시(OnHit+OnTick 구독)
            _resolver.Resolve<CombatVfxController>().Initialize();   // 타입별 임팩트(자체 풀 소유). OnHit+OnTick 구독.
            _resolver.Resolve<DamageStats>();                        // 데미지 집계 시작(OnHit 구독)
            if (_ctx.CharReady) _resolver.Resolve<CharController>().Initialize(); // 조준 스무딩 + 발사 반동

            // 그리드: 원점(씬 앵커 파생)을 Initialize 전에 주입.
            GridController grid = _resolver.Resolve<GridController>();
            grid.SetOrigin(_ctx.GridOrigin);
            grid.Initialize();

            // 적: 그리드 준비 후 초기화(스폰 시 GridController 배치 권한 사용).
            EnemyController enemy = _resolver.Resolve<EnemyController>();
            enemy.Initialize();
            enemy.SetDefensePoint(_ctx.DefensePoint); // 침범 연출 돌진 목표(캐릭터)

            // 스테이지: 적 준비 후 초기화(초기화 시 웨이브 스폰 시작).
            StageController stage = _resolver.Resolve<StageController>();
            stage.Initialize();
            _resolver.Resolve<DefeatSequenceController>().Initialize(); // 실패 연출 게이트 구독

            // 결과 UI(스펙 §5): HP 바(BaseModel 구독) + 승/패 팝업(StateChanged 구독). 씬에 있으면 배선.
            HpBarView hpBar = Object.FindFirstObjectByType<HpBarView>();
            if (hpBar) hpBar.Bind(stage.Base);
            _resolver.Resolve<ResultViewController>().Initialize();

            // 조준 입력이 일시정지(드래프트/결과)를 알도록 IClock 주입 — 카드 클릭이 조준으로 새는 버그 방지.
            AimController aim = Object.FindFirstObjectByType<AimController>();
            if (aim) _resolver.Inject(aim);

            if (_ctx.DraftReady) _resolver.Resolve<CardDraftController>().Initialize(); // 킬→XP 구독 + 뷰 바인드
        }
    }
}
