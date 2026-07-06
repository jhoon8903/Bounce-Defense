using System.Collections.Generic;
using Game.Runtime.Combat;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    // 결과창(ClearView) 스킬별 데미지 목록 팝입기(가산점). Daniel이 만든 컨테이너 + 행 템플릿을 받아,
    // DamageStats를 데미지 내림차순으로 채운다. 아이콘/이름은 SkillDatabase에서 EffectKind로 조회(수동 매핑 없음).
    public sealed class DTResultView : MonoBehaviour
    {
        [SerializeField] private Transform rowContainer;      // 행 부모(VerticalLayoutGroup 권장)
        [SerializeField] private DTResultRow rowTemplate;     // 행 템플릿(복제됨 — 비활성 권장)

        private readonly List<DTResultRow> _rows = new();
        private readonly List<KeyValuePair<SkillEffectKind, long>> _sorted = new();

        // 결과 표시 시 호출(ClearView). 데미지 내림차순 행 생성. 아이콘/이름은 주입된 SkillDatabase에서 EffectKind로 조회(수동 매핑 없음).
        public void Populate(DamageStats stats, SkillDatabase db)
        {
            Clear();
            if (stats == null || rowContainer == null || rowTemplate == null) return;
            rowTemplate.gameObject.SetActive(false);
            _sorted.Clear();
            foreach (KeyValuePair<SkillEffectKind, long> kv in stats.ByKind)
            {
                if (kv.Value > 0) _sorted.Add(kv);
            }
            _sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < _sorted.Count; i++)
            {
                SkillEffectKind kind = _sorted[i].Key;
                SkillDefinition def = db != null ? db.FindByEffectKind(kind) : null;
                Sprite icon = def != null ? def.Icon : null;
                DTResultRow row = Instantiate(rowTemplate, rowContainer);
                row.gameObject.SetActive(true);
                row.Bind(icon, _sorted[i].Value);
                _rows.Add(row);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null) Destroy(_rows[i].gameObject);
            }
            _rows.Clear();
        }
    }
}
