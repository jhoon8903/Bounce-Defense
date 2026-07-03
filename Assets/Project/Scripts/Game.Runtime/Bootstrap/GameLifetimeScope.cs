using Game.Combat;
using Game.Core.Clock;
using Game.Core.Pool;
using Game.Core.Random;
using Game.Events;
using Game.Runtime.Combat;
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
            });
        }
    }
}
