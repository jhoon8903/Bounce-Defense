using Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // 로드아웃 슬롯 1칸(스펙 §47 Active4/Passive2). 자식 직접 serialized ref — 런타임 Find 금지.
    // SkillLoadoutView가 SkillSlotView[]로 소유하고 인덱스로 채운다.
    public sealed class SkillSlotView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private GameObject levelGroup; // 레벨 표시 컨테이너(빈 슬롯이면 숨김)
        [SerializeField] private TMP_Text levelText;

        public void SetSkill(SkillDefinition def, int level)
        {
            if (icon != null) { icon.sprite = def.Icon; icon.enabled = def.Icon != null; }
            if (levelGroup != null) levelGroup.SetActive(true);
            if (levelText != null) levelText.SetText("x {0}", level); // 무할당(숫자 오버로드)
        }

        public void SetEmpty()
        {
            if (icon != null) icon.enabled = false;
            if (levelGroup != null) levelGroup.SetActive(false);
        }
    }
}
