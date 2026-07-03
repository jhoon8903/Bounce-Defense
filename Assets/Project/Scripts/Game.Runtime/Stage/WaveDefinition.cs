using System;
using System.Collections.Generic;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Runtime.Stage
{
    // 한 웨이브의 스폰 정의(개발플랜 §216 spawnTable). 그룹별 적 종류×수를 순서대로 스폰.
    [CreateAssetMenu(fileName = "WaveDef", menuName = "Game/Configs/WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [Serializable]
        public struct SpawnGroup
        {
            public EnemyDefinition enemy;
            [Min(1)] public int count;
        }

        [SerializeField] private SpawnGroup[] groups;
        [SerializeField] [Min(0.05f)] private float spawnInterval = 0.8f; // 스폰 간격(초)

        public float SpawnInterval => spawnInterval;

        // 이 웨이브의 총 스폰 수 = 전멸 판정 기준(§217 killsRequired=전멸 수).
        public int TotalCount
        {
            get
            {
                int n = 0;
                if (groups != null)
                    for (int i = 0; i < groups.Length; i++)
                        if (groups[i].enemy != null) n += Mathf.Max(0, groups[i].count);
                return n;
            }
        }

        // 스폰 순서대로 EnemyDefinition을 큐에 펼친다.
        public void BuildSpawnQueue(List<EnemyDefinition> buffer)
        {
            buffer.Clear();
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++)
            {
                SpawnGroup g = groups[i];
                if (g.enemy == null) continue;
                for (int k = 0; k < g.count; k++) buffer.Add(g.enemy);
            }
        }
    }
}
