using System;
using System.Collections.Generic;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Runtime.Stage
{
    [CreateAssetMenu(fileName = "WaveDef", menuName = "Game/Configs/WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [Serializable]
        public struct Placement
        {
            public EnemyDefinition enemy;
            public int col;
            public int row;
            public int group;
        }

        [SerializeField] private Placement[] placements;

        public int PlacementCount
        {
            get
            {
                int n = 0;
                if (placements != null)
                    for (int i = 0; i < placements.Length; i++)
                    {
                        if (placements[i].enemy != null) n++;
                    }
                return n;
            }
        }

        public void BuildPlacements(List<Placement> buffer)
        {
            buffer.Clear();
            if (placements == null) return;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i].enemy != null) buffer.Add(placements[i]);
            }
        }

        public int GroupCount
        {
            get
            {
                int max = -1;
                if (placements == null) return max + 1;
                for (int i = 0; i < placements.Length; i++)
                {
                    if (placements[i].enemy != null && placements[i].group > max) max = placements[i].group;
                }
                return max + 1;
            }
        }

        public void BuildGroup(int group, List<Placement> buffer)
        {
            buffer.Clear();
            if (placements == null) return;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i].enemy != null && placements[i].group == group) buffer.Add(placements[i]);
            }
        }

#if UNITY_EDITOR
        public Placement[] EditorPlacements
        {
            get => placements;
            set => placements = value;
        }
#endif
    }
}
