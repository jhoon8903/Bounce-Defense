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
        [Header("Pool")]
        [SerializeField] private PoolConfiguration[] poolConfigs; // EnemyView 등 GamePool 대상
        [SerializeField] private BallConfig[] ballConfigs;         // 볼 타입별 config(각자 프리팹) — BallFactory가 타입별 풀 소유
        [SerializeField] private ImpactConfig[] impactConfigs;     // 볼 타입별 임팩트 파티클 config — CombatVfxController가 타입별 풀 소유
        [SerializeField] private ImpactConfig explosionConfig;     // Last Match 붉은 폭발 파티클 config(#5) — 볼 타입 무관 단일 풀. 미배선 시 폭발 무연출.
        [SerializeField] private ImpactConfig clusterConfig;       // Cluster 분열 수류탄 폭발 config — 볼 타입 무관 단일 풀. 미배선 시 무연출.
        [SerializeField] private ImpactConfig deathConfig;         // 적 사망 돌 깨짐 config(#3) — 볼 타입 무관 단일 풀. 미배선 시 무연출.
        [SerializeField] private ImpactConfig laserConfig;         // Laser 행 빔 파티클 config(#7) — 볼 타입 무관 단일 풀. 미배선 시 빔 무연출.
        [SerializeField] private ImpactConfig bloodConfig;         // 방어선 침범 피 연출 config(#3) — 미배선 시 무연출.
        [SerializeField] private Transform poolRoot;

        [Header("Scene Refs (RegisterComponent로 주입)")]
        [SerializeField] private LaunchController launchController;
        [SerializeField] private CharView charView; // Char 비주얼(조준/반동) — CharController가 IClock 틱으로 구동

        [Header("Grid")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private Transform gridAnchor;        // 씬의 빈 'Grid' 오브젝트 (0,1.27) = 그리드 중심
        [SerializeField] private GridDebugView gridDebugView; // 선택: 기즈모 시각화(같은 오브젝트에 부착 가능)

        [Header("Stage")]
        [SerializeField] private StageDefinition stageDefinition; // 웨이브/베이스HP 데이터

        [Header("Roguelike (Phase 3 카드 드래프트)")]
        [SerializeField] private SkillDatabase skillDatabase;         // 10스킬 풀(액티브5/패시브5)
        [SerializeField] private LevelProgressView levelProgressView; // XP 진행도 바
        [SerializeField] private CardSelectView cardSelectView;       // 3택 카드 패널
        [SerializeField] private int rngSeed = 12345;                 // 시드 RNG(§265 결정론·재현)

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new GameClock()).As<IClock>();
            builder.RegisterInstance(new GamePool(poolConfigs)).As<IPool>();
            builder.Register<ModifierRegistry>(Lifetime.Singleton); // 패시브 데미지 모디파이어 컬렉션(SkillRuntime이 갱신)
            builder.Register<DamageResolver>(Lifetime.Singleton);   // ctor: ModifierRegistry + IRandom 자동주입
            builder.Register<CombatEventHub>(Lifetime.Singleton);
            builder.Register<DamageStats>(Lifetime.Singleton); // 볼(스킬)별 누적 데미지 집계(결과창 DTResult, 가산점)
            builder.Register<HitFeedbackController>(Lifetime.Singleton); // 데미지 숫자(풀) + 적 화이트 플래시
            // 전투 파티클 스포너: 볼 타입별 임팩트 풀 소유(BallFactory와 동일하게 config+poolRoot는 씬 주입 → 팩토리 람다).
            builder.Register<CombatVfxController>(container =>
                new CombatVfxController(impactConfigs, explosionConfig, clusterConfig, deathConfig, laserConfig, bloodConfig, poolRoot ? poolRoot : transform,
                    container.Resolve<IClock>(), container.Resolve<CombatEventHub>()), Lifetime.Singleton);
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
            builder.Register<IBallFactory>(container => new BallFactory(ballConfigs, poolRoot ? poolRoot : transform, container), Lifetime.Singleton);
            builder.Register<BallController>(Lifetime.Singleton);

            // Grid: BallConfig와 동일하게 인스턴스 주입 + 싱글톤 컨트롤러. 풀에서 꺼내는 게 없어 pool.Activate 불필요.
            builder.RegisterInstance(gridConfig != null ? gridConfig : ScriptableObject.CreateInstance<GridConfig>());
            builder.Register<GridController>(Lifetime.Singleton);
            if (gridDebugView) builder.RegisterComponent(gridDebugView);

            if (launchController) builder.RegisterComponent(launchController);

            // Char 비주얼 구동(IClock 틱): 조준 스무딩 + 발사 반동 까딱. 뷰/발사대 미배선 시 스킵(코어 루프 무관).
            if (charView == null) charView = FindFirstObjectByType<CharView>(); // 인스펙터 미할당 폴백(StageHudView 패턴)
            bool charReady = charView != null && launchController != null;
            if (charReady)
            {
                builder.RegisterComponent(charView);
                builder.Register<CharController>(Lifetime.Singleton);
            }

            // Enemy MVC: Ball 스택과 동일 방식(Factory+Controller 싱글톤). per-type 데이터는 EnemyDefinition SO(런타임 주입).
            // 적 = Block 타일(콜라이더 몸체)+위 Mob 비주얼, 단일 EnemyView 풀. GridController에 배치 위임.
            builder.Register<IEnemyFactory, EnemyFactory>(Lifetime.Singleton);
            builder.Register<EnemyController>(Lifetime.Singleton);

            // Stage: 웨이브 진행/베이스HP/승패. StageDefinition 인스턴스 주입(BallConfig/GridConfig와 동일 방식).
            builder.RegisterInstance(stageDefinition != null ? stageDefinition : ScriptableObject.CreateInstance<StageDefinition>());
            builder.Register<StageController>(Lifetime.Singleton);

            // 실패 시퀀스(#4): 베이스 0 → 캐릭터 분리 연출 → Lost. CharDeathView(씬)는 nullable(미배선=즉시 완료).
            CharDeathView charDeathView = FindFirstObjectByType<CharDeathView>();
            builder.Register(resolver => new DefeatSequenceController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), charDeathView), Lifetime.Singleton);

            // 결과 뷰(Daniel UI): 성공 ClearView / 실패 DefeatedView. StateChanged 구독. 씬 nullable.
            ClearView clearView = FindFirstObjectByType<ClearView>();
            DefeatedView defeatedView = FindFirstObjectByType<DefeatedView>();
            builder.Register(resolver => new ResultViewController(
                resolver.Resolve<StageController>(), resolver.Resolve<IClock>(), clearView, defeatedView,
                resolver.Resolve<DamageStats>(), resolver.Resolve<SkillDatabase>()), Lifetime.Singleton);

            // Roguelike(Phase 3): 킬 XP 레벨업 → 3택 카드 드래프트. 순수 로직(로드아웃/드로우/레벨)은 asmdef,
            // 뷰는 씬 컴포넌트(RegisterComponent). 뷰/DB 미배선이면 드래프트 비활성(코어 루프는 그대로 동작).
            builder.RegisterInstance(new SystemRandom(rngSeed)).As<IRandom>(); // 시드 결정론
            builder.Register<PlayerLoadout>(Lifetime.Singleton);
            builder.RegisterInstance(skillDatabase != null ? skillDatabase : ScriptableObject.CreateInstance<SkillDatabase>());
            builder.Register(resolver =>
            {
                StageDefinition s = resolver.Resolve<StageDefinition>();
                return new LevelModel(s.XpPerKill, s.BaseXpToLevel, s.XpGrowthPerLevel, s.MaxLevel);
            }, Lifetime.Singleton);
            builder.Register<CardDrawService>(Lifetime.Singleton);

            // 스킬 런타임: 로드아웃 → 볼 로스터 + 패시브 모디파이어 브리지(드래프트 유무와 무관, 미획득 시 노멀 5).
            builder.Register<SkillRuntime>(Lifetime.Singleton);

            bool draftReady = levelProgressView != null && cardSelectView != null;
            if (draftReady)
            {
                builder.RegisterComponent(levelProgressView);
                builder.RegisterComponent(cardSelectView);
                builder.Register<CardDraftController>(Lifetime.Singleton);
            }

            builder.RegisterBuildCallback(container =>
            {
                IPool pool = container.Resolve<IPool>();
                // 볼은 BallFactory가 타입별 풀 소유 → GamePool은 EnemyView + DamageTextView 담당(BallView 제외, §11-9).
                pool.Activate(poolRoot ? poolRoot : transform, typeof(EnemyView), typeof(DamageTextView));
                container.Resolve<BallController>().Initialize();

                // 스킬 런타임 배선: 로드아웃 구독 + 초기 로스터(노멀 5) 푸시. BallController 초기화 직후.
                container.Resolve<SkillRuntime>().Initialize();

                // 피격 피드백(데미지 숫자 + 화이트 플래시): OnHit + OnTick 구독. 풀 활성화 후.
                container.Resolve<HitFeedbackController>().Initialize();

                // 전투 파티클(타입별 임팩트): OnHit + OnTick 구독. 자체 타입별 풀 소유(GamePool 무관, pool.Activate 불필요).
                container.Resolve<CombatVfxController>().Initialize();
                container.Resolve<DamageStats>(); // 데미지 집계 시작(OnHit 구독)

                // Char 비주얼 구동: OnTick 구독(조준 스무딩 + 발사 반동). 뷰/발사대 배선된 경우에만.
                if (charReady) container.Resolve<CharController>().Initialize();

                // Grid: 원점(씬 Grid 앵커)을 Initialize 전에 주입 — BallController.SetCollectTarget 패턴과 동일.
                GridController grid = container.Resolve<GridController>();
                Vector2 gridOrigin = gridAnchor
                    ? gridAnchor.position
                    : gridConfig != null ? gridConfig.OriginFallback : new Vector2(0f, 1.27f);
                grid.SetOrigin(gridOrigin);
                grid.Initialize();

                // 적 컨트롤러는 그리드 준비 후 초기화(스폰 시 GridController 배치 권한을 사용).
                EnemyController enemyController = container.Resolve<EnemyController>();
                enemyController.Initialize();
                enemyController.SetDefensePoint(launchController ? launchController.Origin : new Vector2(0f, -6.70f)); // 침범 연출 돌진 목표(캐릭터)

                // 스테이지 컨트롤러는 적 컨트롤러 준비 후 초기화(초기화 시 웨이브 스폰이 시작된다).
                StageController stageController = container.Resolve<StageController>();
                stageController.Initialize();
                container.Resolve<DefeatSequenceController>().Initialize(); // 실패 연출 게이트 구독

                // 결과 UI(스펙 §5): HP 바(BaseModel 구독) + 승/패 결과 팝업(StateChanged 구독). 씬에 있으면 배선.
                HpBarView hpBar = UnityEngine.Object.FindFirstObjectByType<HpBarView>();
                if (hpBar) hpBar.Bind(stageController.Base);
                container.Resolve<ResultViewController>().Initialize(); // 결과 뷰(성공 ClearView / 실패 DefeatedView) 구독

                // 조준 입력이 일시정지(드래프트/결과)를 알도록 IClock 주입 — 카드 클릭이 조준으로 새는 버그 방지.
                AimController aim = UnityEngine.Object.FindFirstObjectByType<AimController>();
                if (aim) container.Inject(aim);

                // 카드 드래프트: 킬→XP 구독 + 뷰 바인드. 뷰/DB가 씬에 배선된 경우에만 활성.
                if (draftReady) container.Resolve<CardDraftController>().Initialize();
            });
        }
    }
}
