using Game.Combat;
using Game.Core.Clock;
using Game.Core.Pool;
using Game.Core.Random;
using Game.Events;
using Game.Runtime.Combat;
using Game.Runtime.Grid;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Runtime.Bootstrap
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("RNG")]
        [SerializeField] private int randomSeed = 12345;

        [Header("Pool")]
        [SerializeField] private PoolConfiguration[] poolConfigs;
        [SerializeField] private Transform poolRoot;

        [Header("Scene Refs (RegisterComponent로 주입)")]
        [SerializeField] private LaunchController launchController;
        [SerializeField] private Enemy[] enemies;

        [Header("Grid")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private Transform gridAnchor;        // 씬의 빈 'Grid' 오브젝트 (0,1.27) = 그리드 중심
        [SerializeField] private GridDebugView gridDebugView; // 선택: 기즈모 시각화(같은 오브젝트에 부착 가능)

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new SystemRandom(randomSeed)).As<IRandom>();
            builder.RegisterInstance(new GameClock()).As<IClock>();
            builder.RegisterInstance(new GamePool(poolConfigs)).As<IPool>();
            builder.Register<ModifierRegistry>(Lifetime.Singleton);
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
            for (int i = 0; i < enemies.Length; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy) builder.RegisterComponent(enemy);
            }
            builder.RegisterBuildCallback(container =>
            {
                IPool pool = container.Resolve<IPool>();
                pool.Activate(poolRoot ? poolRoot : transform, typeof(BallView));
                container.Resolve<BallController>().Initialize();

                // Grid: 원점(씬 Grid 앵커)을 Initialize 전에 주입 — BallController.SetCollectTarget 패턴과 동일.
                GridController grid = container.Resolve<GridController>();
                Vector2 gridOrigin = gridAnchor
                    ? (Vector2)gridAnchor.position
                    : (gridConfig != null ? gridConfig.OriginFallback : new Vector2(0f, 1.27f));
                grid.SetOrigin(gridOrigin);
                grid.Initialize();
            });
        }
    }
}
