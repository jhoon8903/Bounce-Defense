using Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class SkillSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject levelGroup;
        [SerializeField] private TMP_Text levelText;

        public void SetSkill(SkillDefinition def, int level)
        {
            if (icon != null)
            {
                icon.sprite = def.Icon;
                icon.enabled = def.Icon != null;
            }
            if (levelGroup != null) levelGroup.SetActive(true);
            if (levelText != null) levelText.SetText("x {0}", level);
        }

        public void SetEmpty()
        {
            if (icon != null) icon.enabled = false;
            if (levelGroup != null) levelGroup.SetActive(false);
        }
    }
}
