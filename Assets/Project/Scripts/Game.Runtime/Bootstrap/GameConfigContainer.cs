using Game.Core.Pool;
using Game.Runtime.Combat;
using Game.Runtime.Grid;
using Game.Runtime.Stage;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    // 데이터 주도 설계의 단일 config 출처(WigglePuzzle MConfigContainer 패턴). 프로젝트 config SO를 한 에셋에 모아
    // GameLifetimeScope가 개별 필드 11개를 드래그하는 대신 컨테이너 1개만 참조한다. 씬 오브젝트 참조(발사대·Char·앵커·뷰)는
    // 프로젝트 에셋에 직렬화 불가라 스코프에 남는다 — configs(데이터)와 scene refs(런타임 배선)의 정당한 분리.
    [CreateAssetMenu(fileName = "GameConfigContainer", menuName = "Game/Configs/GameConfigContainer")]
    public sealed class GameConfigContainer : ScriptableObject
    {
        [Header("Pool / Ball / VFX")]
        [SerializeField] private PoolConfiguration[] poolConfigs; // EnemyView 등 GamePool 대상
        [SerializeField] private BallConfig[] ballConfigs;         // 볼 타입별 config(각자 프리팹)
        [SerializeField] private ImpactConfig[] impactConfigs;     // 볼 타입별 임팩트 파티클 config
        [SerializeField] private ImpactConfig explosionConfig;     // Last Match 붉은 폭발(#5)
        [SerializeField] private ImpactConfig clusterConfig;       // Cluster 분열 수류탄 폭발
        [SerializeField] private ImpactConfig deathConfig;         // 적 사망 돌 깨짐(#3)
        [SerializeField] private ImpactConfig laserConfig;         // Laser 행 빔(#7)
        [SerializeField] private ImpactConfig bloodConfig;         // 방어선 침범 피 연출(#3)

        [Header("Grid / Stage")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private StageDefinition stageDefinition;  // 웨이브/베이스HP 데이터

        [Header("Roguelike")]
        [SerializeField] private SkillDatabase skillDatabase;      // 10스킬 풀(액티브5/패시브5) + 노멀 집계
        [SerializeField] private int rngSeed = 12345;              // 시드 RNG(결정론·재현)

        public PoolConfiguration[] PoolConfigs => poolConfigs;
        public BallConfig[] BallConfigs => ballConfigs;
        public ImpactConfig[] ImpactConfigs => impactConfigs;
        public ImpactConfig ExplosionConfig => explosionConfig;
        public ImpactConfig ClusterConfig => clusterConfig;
        public ImpactConfig DeathConfig => deathConfig;
        public ImpactConfig LaserConfig => laserConfig;
        public ImpactConfig BloodConfig => bloodConfig;
        public GridConfig GridConfig => gridConfig;
        public StageDefinition StageDefinition => stageDefinition;
        public SkillDatabase SkillDatabase => skillDatabase;
        public int RngSeed => rngSeed;
    }
}
