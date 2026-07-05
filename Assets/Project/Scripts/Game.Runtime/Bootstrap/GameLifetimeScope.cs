using Game.Combat;
using Game.Core.Clock;
using Game.Core.Pool;
using Game.Events;
using Game.Runtime.Combat;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Stage;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Runtime.Bootstrap
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Pool")]
        [SerializeField] private PoolConfiguration[] poolConfigs;
        [SerializeField] private Transform poolRoot;

        [Header("Scene Refs (RegisterComponent로 주입)")]
        [SerializeField] private LaunchController launchController;

        [Header("Grid")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private Transform gridAnchor;        // 씬의 빈 'Grid' 오브젝트 (0,1.27) = 그리드 중심
        [SerializeField] private GridDebugView gridDebugView; // 선택: 기즈모 시각화(같은 오브젝트에 부착 가능)

        [Header("Stage")]
        [SerializeField] private StageDefinition stageDefinition; // 웨이브/베이스HP 데이터

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new GameClock()).As<IClock>();
            builder.RegisterInstance(new GamePool(poolConfigs)).As<IPool>();
            builder.Register<DamageResolver>(Lifetime.Singleton);
            builder.Register<CombatEventHub>(Lifetime.Singleton);
            BallConfig ballConfig = null;
            if (poolConfigs != null)
            {
                for (int i = 0; i < poolConfigs.Length; i++)
                {
                    if (poolConfigs[i] is BallConfig bc) { ballConfig = bc; break; }
                }
            }
            builder.RegisterInstance(ballConfig != null ? ballConfig : ScriptableObject.CreateInstance<BallConfig>());
            builder.Register<IBallFactory, BallFactory>(Lifetime.Singleton);
            builder.Register<BallController>(Lifetime.Singleton);

            // Grid: BallConfig와 동일하게 인스턴스 주입 + 싱글톤 컨트롤러. 풀에서 꺼내는 게 없어 pool.Activate 불필요.
            builder.RegisterInstance(gridConfig != null ? gridConfig : ScriptableObject.CreateInstance<GridConfig>());
            builder.Register<GridController>(Lifetime.Singleton);
            if (gridDebugView) builder.RegisterComponent(gridDebugView);

            if (launchController) builder.RegisterComponent(launchController);

            // Enemy MVC: Ball 스택과 동일 방식(Factory+Controller 싱글톤). per-type 데이터는 EnemyDefinition SO(런타임 주입).
            // 적 = Block 타일(콜라이더 몸체)+위 Mob 비주얼, 단일 EnemyView 풀. GridController에 배치 위임.
            builder.Register<IEnemyFactory, EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Singleton);

            // Stage: 웨이브 진행/베이스HP/승패. StageDefinition 인스턴스 주입(BallConfig/GridConfig와 동일 방식).
            builder.RegisterInstance(stageDefinition != null ? stageDefinition : ScriptableObject.CreateInstance<StageDefinition>());
            builder.Register<StageController>(Lifetime.Singleton);
            builder.RegisterBuildCallback(container =>
            {
                IPool pool = container.Resolve<IPool>();
                pool.Activate(poolRoot ? poolRoot : transform, typeof(BallView), typeof(EnemyView));
                container.Resolve<BallController>().Initialize();

                // Grid: 원점(씬 Grid 앵커)을 Initialize 전에 주입 — BallController.SetCollectTarget 패턴과 동일.
                GridController grid = container.Resolve<GridController>();
                Vector2 gridOrigin = gridAnchor
                    ? (Vector2)gridAnchor.position
                    : (gridConfig != null ? gridConfig.OriginFallback : new Vector2(0f, 1.27f));
                grid.SetOrigin(gridOrigin);
                grid.Initialize();

                // 적 컨트롤러는 그리드 준비 후 초기화(스폰 시 GridController 배치 권한을 사용).
                container.Resolve<EnemyController>().Initialize();

                // 스테이지 컨트롤러는 적 컨트롤러 준비 후 초기화(초기화 시 웨이브 스폰이 시작된다).
                container.Resolve<StageController>().Initialize();

                // 디버그 HUD(씬에 있으면) 주입 — 웨이브/베이스/상태 가시화.
                StageHudView hud = UnityEngine.Object.FindFirstObjectByType<StageHudView>();
                if (hud) container.Inject(hud);
            });
        }
    }
}
