using System.Collections.Generic;
using Game.Runtime.Combat;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    public sealed class DTResultView : MonoBehaviour
    {
        [SerializeField] private Transform rowContainer;
        [SerializeField] private DTResultRow rowTemplate;

        private readonly List<DTResultRow> _rows = new();
        private readonly List<KeyValuePair<SkillEffectKind, long>> _sorted = new();

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
