using Game.Combat;
using Game.Core.Clock;
using Game.Core.Pool;
using Game.Core.Random;
using Game.Events;
using Game.Roguelike;
using Game.Runtime.Combat;
using Game.Runtime.Enemy;
using Game.Runtime.Grid;
using Game.Runtime.Progression;
using Game.Runtime.Skills;
using Game.Runtime.Stage;
using Game.Runtime.UI;
using Game.Skills;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Runtime.Bootstrap
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Data (단일 config 컨테이너 — 데이터 주도)")]
        [SerializeField] private GameConfigContainer config;

        [Header("Scene Refs (씬 오브젝트라 컨테이너에 못 담음)")]
        [SerializeField] private Transform poolRoot;
        [SerializeField] private LaunchController launchController;
        [SerializeField] private CharView charView;
        [SerializeField] private Transform gridAnchor;
        [SerializeField] private GridDebugView gridDebugView;
        [SerializeField] private LevelProgressView levelProgressView;
        [SerializeField] private CardSelectView cardSelectView;

        protected override void Configure(IContainerBuilder builder)
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfigContainer>();
            Transform poolParent = poolRoot ? poolRoot : transform;
            if (charView == null) charView = FindFirstObjectByType<CharView>();
            bool charReady = charView != null && launchController != null;
            bool draftReady = levelProgressView != null && cardSelectView != null;

            ConfigureCore(builder, poolParent);
            ConfigureBall(builder, poolParent);
            ConfigureGridAndChar(builder, charReady);
            ConfigureEnemy(builder);
            ConfigureStageAndResult(builder);
            ConfigureRoguelike(builder, draftReady);

            Vector2 gridOrigin = gridAnchor
                ? gridAnchor.position
                : config.GridConfig != null ? config.GridConfig.OriginFallback : new Vector2(0f, 1.27f);
            builder.RegisterInstance(new BootstrapContext
            {
                PoolParent = poolParent,
                GridOrigin = gridOrigin,
                DefensePoint = launchController ? launchController.Origin : new Vector2(0f, -6.70f),
                CharReady = charReady,
                DraftReady = draftReady,
            });
            builder.RegisterEntryPoint<GameEntryPoint>();
        }

        private void ConfigureCore(IContainerBuilder builder, Transform poolParent)
        {
            builder.RegisterInstance(new GameClock()).As<IClock>();
            builder.RegisterInstance(new GamePool(config.PoolConfigs)).As<IPool>();
            builder.Register<ModifierRegistry>(Lifetime.Singleton);
            builder.Register<DamageResolver>(Lifetime.Singleton);
            builder.Register<CombatEventHub>(Lifetime.Singleton);
            builder.Register<DamageStats>(Lifetime.Singleton);
            builder.Register<HitFeedbackController>(Lifetime.Singleton);
            builder.Register<CombatVfxController>(container =>
                new CombatVfxController(config.ImpactConfigs, config.ExplosionConfig, config.ClusterConfig, config.DeathConfig, config.LaserConfig, config.BloodConfig,
                    poolParent, container.Resolve<IClock>(), container.Resolve<CombatEventHub>()), Lifetime.Singleton);
        }

        private void ConfigureBall(IContainerBuilder builder, Transform poolParent)
        {
            BallConfig[] ballConfigs = config.BallConfigs;
            BallConfig normalConfig = null;
            if (ballConfigs != null)
            {
                for (int i = 0; i < ballConfigs.Length; i++)
                {
                    if (ballConfigs[i] == null) continue;
                    if (normalConfig == null) normalConfig = ballConfigs[i];
                    if (ballConfigs[i].SourceType != BallSourceType.Normal) continue;
                    normalConfig = ballConfigs[i]; break;
                }
            }
            builder.RegisterInstance(normalConfig != null ? normalConfig : ScriptableObject.CreateInstance<BallConfig>());
            builder.Register<IBallFactory>(container => new BallFactory(ballConfigs, poolParent, container), Lifetime.Singleton);
            builder.Register<BallController>(Lifetime.Singleton);
            if (launchController) builder.RegisterComponent(launchController);
        }

        private void ConfigureGridAndChar(IContainerBuilder builder, bool charReady)
        {
            builder.RegisterInstance(config.GridConfig != null ? config.GridConfig : ScriptableObject.CreateInstance<GridConfig>());
            builder.Register<GridController>(Lifetime.Singleton);
            if (gridDebugView) builder.RegisterComponent(gridDebugView);
            if (charReady)
            {
                builder.RegisterComponent(charView);
                builder.Register<CharController>(Lifetime.Singleton);
            }
        }

        private void ConfigureEnemy(IContainerBuilder builder)
        {
            builder.Register<IEnemyFactory, EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Singleton);
        }

        private void ConfigureStageAndResult(IContainerBuilder builder)
        {
            builder.RegisterInstance(config.StageDefinition != null ? config.StageDefinition : ScriptableObject.CreateInstance<StageDefinition>());
            builder.Register<StageController>(Lifetime.Singleton);

            CharDeathView charDeathView = FindFirstObjectByType<CharDeathView>();
            builder.Register(resolver => new DefeatSequenceController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), charDeathView), Lifetime.Singleton);

            ClearView clearView = FindFirstObjectByType<ClearView>();
            DefeatedView defeatedView = FindFirstObjectByType<DefeatedView>();
            builder.Register(resolver => new ResultViewController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), clearView, defeatedView,
                resolver.Resolve<DamageStats>(), resolver.Resolve<SkillDatabase>()), Lifetime.Singleton);
        }

        private void ConfigureRoguelike(IContainerBuilder builder, bool draftReady)
        {
            builder.RegisterInstance(new SystemRandom(UnityEngine.Random.Range(1, int.MaxValue))).As<IRandom>();
            builder.Register<PlayerLoadout>(Lifetime.Singleton);
            builder.RegisterInstance(config.SkillDatabase != null ? config.SkillDatabase : ScriptableObject.CreateInstance<SkillDatabase>());
            builder.Register(resolver =>
            {
                StageDefinition s = resolver.Resolve<StageDefinition>();
                return new LevelModel(s.XpPerKill, s.BaseXpToLevel, s.XpGrowthPerLevel, s.MaxLevel);
            }, Lifetime.Singleton);
            builder.Register<CardDrawService>(Lifetime.Singleton);
            builder.Register<SkillRuntime>(Lifetime.Singleton);

            if (draftReady)
            {
                builder.RegisterComponent(levelProgressView);
                builder.RegisterComponent(cardSelectView);
                builder.Register<CardDraftController>(Lifetime.Singleton);
            }
        }
    }
}
