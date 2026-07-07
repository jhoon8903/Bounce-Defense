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
    public sealed class BootstrapContext
    {
        public Transform PoolParent;
        public Vector2 GridOrigin;
        public Vector2 DefensePoint;
        public bool CharReady;
        public bool DraftReady;
    }

    public sealed class GameEntryPoint : IStartable
    {
        private readonly IObjectResolver _resolver;
        private readonly BootstrapContext _ctx;

        public GameEntryPoint(IObjectResolver resolver, BootstrapContext ctx)
        {
            _resolver = resolver;
            _ctx = ctx;
        }

        public void Start()
        {
            IPool pool = _resolver.Resolve<IPool>();
            pool.Activate(_ctx.PoolParent, typeof(EnemyView), typeof(DamageTextView));
            _resolver.Resolve<BallController>().Initialize();
            _resolver.Resolve<SkillRuntime>().Initialize();
            _resolver.Resolve<HitFeedbackController>().Initialize();
            _resolver.Resolve<CombatVfxController>().Initialize();
            _resolver.Resolve<DamageStats>();
            if (_ctx.CharReady) _resolver.Resolve<CharController>().Initialize();

            GridController grid = _resolver.Resolve<GridController>();
            grid.SetOrigin(_ctx.GridOrigin);
            grid.Initialize();

            EnemyController enemy = _resolver.Resolve<EnemyController>();
            enemy.Initialize();
            enemy.SetDefensePoint(_ctx.DefensePoint);

            StageController stage = _resolver.Resolve<StageController>();
            stage.Initialize();
            _resolver.Resolve<DefeatSequenceController>().Initialize();

            HpBarView hpBar = Object.FindFirstObjectByType<HpBarView>();
            if (hpBar) hpBar.Bind(stage.Base);
            _resolver.Resolve<ResultViewController>().Initialize();

            AimController aim = Object.FindFirstObjectByType<AimController>();
            if (aim) _resolver.Inject(aim);

            if (_ctx.DraftReady) _resolver.Resolve<CardDraftController>().Initialize();
        }
    }
}
