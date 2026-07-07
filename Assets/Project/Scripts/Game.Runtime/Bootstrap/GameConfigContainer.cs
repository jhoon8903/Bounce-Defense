using Game.Core.Pool;
using Game.Runtime.Combat;
using Game.Runtime.Grid;
using Game.Runtime.Stage;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    [CreateAssetMenu(fileName = "GameConfigContainer", menuName = "Game/Configs/GameConfigContainer")]
    public sealed class GameConfigContainer : ScriptableObject
    {
        [Header("Pool / Ball / VFX")]
        [SerializeField] private PoolConfiguration[] poolConfigs;
        [SerializeField] private BallConfig[] ballConfigs;
        [SerializeField] private ImpactConfig[] impactConfigs;
        [SerializeField] private ImpactConfig explosionConfig;
        [SerializeField] private ImpactConfig clusterConfig;
        [SerializeField] private ImpactConfig deathConfig;
        [SerializeField] private ImpactConfig laserConfig;
        [SerializeField] private ImpactConfig bloodConfig;

        [Header("Grid / Stage")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private StageDefinition stageDefinition;

        [Header("Roguelike")]
        [SerializeField] private SkillDatabase skillDatabase;
        [SerializeField] private int rngSeed = 12345;

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
