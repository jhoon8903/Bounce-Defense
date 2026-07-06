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
    // 조립 루트(배선 전용): config(데이터)는 GameConfigContainer, 씬 오브젝트는 필드로 받아 도메인별 Configure*로 배선한다.
    // 순서 의존 초기화(생명주기)는 GameEntryPoint(IStartable)로 분리 — 이 클래스는 '무엇을 등록하나'만 안다.
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [Header("Data (단일 config 컨테이너 — 데이터 주도)")]
        [SerializeField] private GameConfigContainer config; // 전 config SO의 단일 출처(WigglePuzzle MConfigContainer 패턴)

        [Header("Scene Refs (씬 오브젝트라 컨테이너에 못 담음)")]
        [SerializeField] private Transform poolRoot;
        [SerializeField] private LaunchController launchController;
        [SerializeField] private CharView charView;           // Char 비주얼(조준/반동) — CharController가 IClock 틱 구동
        [SerializeField] private Transform gridAnchor;        // 씬의 빈 'Grid' 오브젝트 (0,1.27) = 그리드 중심
        [SerializeField] private GridDebugView gridDebugView; // 선택: 기즈모 시각화
        [SerializeField] private LevelProgressView levelProgressView; // XP 진행도 바
        [SerializeField] private CardSelectView cardSelectView;       // 3택 카드 패널

        protected override void Configure(IContainerBuilder builder)
        {
            if (config == null) config = ScriptableObject.CreateInstance<GameConfigContainer>(); // 미배선 방어(접근자는 null/빈배열, 하위 가드 처리)
            Transform poolParent = poolRoot ? poolRoot : transform;
            if (charView == null) charView = FindFirstObjectByType<CharView>(); // 인스펙터 미할당 폴백
            bool charReady = charView != null && launchController != null;
            bool draftReady = levelProgressView != null && cardSelectView != null;

            ConfigureCore(builder, poolParent);
            ConfigureBall(builder, poolParent);
            ConfigureGridAndChar(builder, charReady);
            ConfigureEnemy(builder);
            ConfigureStageAndResult(builder);
            ConfigureRoguelike(builder, draftReady);

            // 초기화 오케스트레이션은 GameEntryPoint(IStartable)로 분리 — 배선(Configure) ≠ 생명주기. 씬 파생값만 BootstrapContext로 전달.
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

        // 코어 서비스 + 전투 피드백: 클럭·풀·데미지 파이프라인·이벤트 허브·집계·피격 피드백·전투 VFX.
        private void ConfigureCore(IContainerBuilder builder, Transform poolParent)
        {
            builder.RegisterInstance(new GameClock()).As<IClock>();
            builder.RegisterInstance(new GamePool(config.PoolConfigs)).As<IPool>();
            builder.Register<ModifierRegistry>(Lifetime.Singleton); // 패시브 데미지 모디파이어 컬렉션(SkillRuntime이 갱신)
            builder.Register<DamageResolver>(Lifetime.Singleton);   // ctor: ModifierRegistry + IRandom 자동주입
            builder.Register<CombatEventHub>(Lifetime.Singleton);
            builder.Register<DamageStats>(Lifetime.Singleton);          // 볼(스킬)별 누적 데미지 집계(결과창 DTResult, 가산점)
            builder.Register<HitFeedbackController>(Lifetime.Singleton); // 데미지 숫자(풀) + 적 화이트 플래시
            // 전투 파티클 스포너: 볼 타입별 임팩트 풀 소유. config+poolParent는 씬 주입 → 팩토리 람다.
            builder.Register<CombatVfxController>(container =>
                new CombatVfxController(config.ImpactConfigs, config.ExplosionConfig, config.ClusterConfig, config.DeathConfig, config.LaserConfig, config.BloodConfig,
                    poolParent, container.Resolve<IClock>(), container.Resolve<CombatEventHub>()), Lifetime.Singleton);
        }

        // 볼: 노멀 config 결정 → 인스턴스 주입 + 타입별 풀 소유 팩토리 + 컨트롤러. 발사대(씬) 등록.
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

        // 그리드 + Char 비주얼: GridConfig 인스턴스 주입 + 컨트롤러(풀에서 꺼내는 게 없어 pool.Activate 불필요).
        // Char는 뷰/발사대 배선된 경우에만 IClock 틱 구동(조준 스무딩 + 발사 반동). 미배선이면 스킵(코어 루프 무관).
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

        // 적 MVC: Ball 스택과 동일 방식(Factory+Controller 싱글톤). per-type 데이터는 EnemyDefinition SO(런타임 주입).
        // 적 = Block 타일(콜라이더 몸체)+위 Mob 비주얼, 단일 EnemyView 풀. 배치는 GridController에 위임.
        private void ConfigureEnemy(IContainerBuilder builder)
        {
            builder.Register<IEnemyFactory, EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Singleton);
        }

        // 스테이지(웨이브 진행/베이스HP/승패) + 실패 시퀀스(#4) + 결과 뷰(Daniel UI, 씬 nullable).
        private void ConfigureStageAndResult(IContainerBuilder builder)
        {
            builder.RegisterInstance(config.StageDefinition != null ? config.StageDefinition : ScriptableObject.CreateInstance<StageDefinition>());
            builder.Register<StageController>(Lifetime.Singleton);

            // 베이스 0 → 캐릭터 분리 연출 → Lost. CharDeathView(씬)는 nullable(미배선=즉시 완료).
            CharDeathView charDeathView = FindFirstObjectByType<CharDeathView>();
            builder.Register(resolver => new DefeatSequenceController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), charDeathView), Lifetime.Singleton);

            // 성공 ClearView / 실패 DefeatedView. StateChanged 구독. 씬 nullable.
            ClearView clearView = FindFirstObjectByType<ClearView>();
            DefeatedView defeatedView = FindFirstObjectByType<DefeatedView>();
            builder.Register(resolver => new ResultViewController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), clearView, defeatedView,
                resolver.Resolve<DamageStats>(), resolver.Resolve<SkillDatabase>()), Lifetime.Singleton);
        }

        // 로그라이크(Phase 3): 킬 XP 레벨업 → 3택 카드 드래프트. 순수 로직(로드아웃/드로우/레벨)은 asmdef,
        // 뷰는 씬 컴포넌트(RegisterComponent). 뷰/DB 미배선이면 드래프트 비활성(코어 루프는 그대로 동작).
        private void ConfigureRoguelike(IContainerBuilder builder, bool draftReady)
        {
            builder.RegisterInstance(new SystemRandom(config.RngSeed)).As<IRandom>(); // 시드 결정론(§265 재현)
            builder.Register<PlayerLoadout>(Lifetime.Singleton);
            builder.RegisterInstance(config.SkillDatabase != null ? config.SkillDatabase : ScriptableObject.CreateInstance<SkillDatabase>());
            builder.Register(resolver =>
            {
                StageDefinition s = resolver.Resolve<StageDefinition>();
                return new LevelModel(s.XpPerKill, s.BaseXpToLevel, s.XpGrowthPerLevel, s.MaxLevel);
            }, Lifetime.Singleton);
            builder.Register<CardDrawService>(Lifetime.Singleton);
            // 로드아웃 → 볼 로스터 + 패시브 모디파이어 브리지(드래프트 유무와 무관, 미획득 시 노멀 5).
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
