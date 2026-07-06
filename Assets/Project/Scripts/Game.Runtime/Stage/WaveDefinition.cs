using System;
using System.Collections.Generic;
using Game.Runtime.Enemy;
using UnityEngine;

namespace Game.Runtime.Stage
{
    // 한 웨이브 = Scene View 에디터로 손배치한 몹 목록(§216 개정: spawnTable(종류×수) → 셀 배치도).
    // 각 배치 = EnemyDefinition + 셀 좌표(col,row). 웨이브 시작 시 전부 스폰되며 낙하 캐스케이드로 등장.
    [CreateAssetMenu(fileName = "WaveDef", menuName = "Game/Configs/WaveDefinition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [Serializable]
        public struct Placement
        {
            public EnemyDefinition enemy;
            public int col;
            public int row;
            public int group; // 서브웨이브 그룹(0=1-1, 1=1-2 …). 같은 웨이브 내 상단행이 비면 순차 스폰되는 단위.
        }

        [SerializeField] private Placement[] placements;

        // 유효 배치 수 = 전멸 판정 기준(§217).
        public int PlacementCount
        {
            get
            {
                int n = 0;
                if (placements != null)
                    for (int i = 0; i < placements.Length; i++)
                        if (placements[i].enemy != null) n++;
                return n;
            }
        }

        // 유효(enemy != null) 배치를 순서대로 버퍼에 채운다. 순서 = 캐스케이드 인덱스(등장 지연).
        public void BuildPlacements(List<Placement> buffer)
        {
            buffer.Clear();
            if (placements == null) return;
            for (int i = 0; i < placements.Length; i++)
                if (placements[i].enemy != null) buffer.Add(placements[i]);
        }

        // 서브웨이브 그룹 수(최대 group 인덱스+1). group 미지정(전부 0)이면 1 = 기존 단일 스폰과 동일.
        public int GroupCount
        {
            get
            {
                int max = -1;
                if (placements != null)
                    for (int i = 0; i < placements.Length; i++)
                        if (placements[i].enemy != null && placements[i].group > max) max = placements[i].group;
                return max + 1;
            }
        }

        // 지정 그룹의 유효 배치만 버퍼에 채운다(StageController가 그룹별 순차 스폰에 사용).
        public void BuildGroup(int group, List<Placement> buffer)
        {
            buffer.Clear();
            if (placements == null) return;
            for (int i = 0; i < placements.Length; i++)
                if (placements[i].enemy != null && placements[i].group == group) buffer.Add(placements[i]);
        }

#if UNITY_EDITOR
        // 에디터 전용 배치 목록 접근(WaveEditorWindow가 읽고 쓴다). 런타임 코드는 BuildPlacements만 사용.
        public Placement[] EditorPlacements
        {
            get => placements;
            set => placements = value;
        }
#endif
    }
}
