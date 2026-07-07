using System;
using Game.Roguelike;
using Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Damage (액티브 전용, 패시브는 미할당)")]
        [SerializeField] private GameObject damageGroup;
        [SerializeField] private TMP_Text currentValueText;
        [SerializeField] private GameObject upgradeMarker;
        [SerializeField] private TMP_Text upgradeValueText;

        [Header("State markers")]
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject[] levelPips;
        [SerializeField] private EventBtn selectButton;

        private SkillCard _card;
        private Action<SkillCard> _onPick;

        private void Awake()
        {
            if (selectButton != null) selectButton.Clicked += HandleClick;
        }

        public void Bind(SkillCard card, Action<SkillCard> onPick)
        {
            _card = card;
            _onPick = onPick;
            SkillDefinition def = card.Definition;

            if (iconImage != null)
            {
                iconImage.sprite = def.Icon;
                iconImage.enabled = def.Icon != null;
            }
            if (nameText != null) nameText.SetText(def.DisplayName);
            if (descriptionText != null) descriptionText.SetText(def.GetDescription(card.Level));
            if (newBadge != null) newBadge.SetActive(card.IsNew);

            BindDamage(card, def);
            SetPips(card.Level, def.MaxLevel);
        }

        private void BindDamage(SkillCard card, SkillDefinition def)
        {
            if (damageGroup == null) return;
            bool showDamage = def.HasBallDamage;
            damageGroup.SetActive(showDamage);
            if (!showDamage) return;

            if (card.IsNew)
            {
                if (currentValueText != null) currentValueText.SetText("{0}", def.GetBallDamage(card.Level));
                if (upgradeMarker != null) upgradeMarker.SetActive(false);
                if (upgradeValueText != null) upgradeValueText.gameObject.SetActive(false);
            }
            else
            {
                if (currentValueText != null) currentValueText.SetText("{0}", def.GetBallDamage(card.Level - 1));
                if (upgradeMarker != null) upgradeMarker.SetActive(true);
                if (upgradeValueText == null) return;
                upgradeValueText.gameObject.SetActive(true);
                upgradeValueText.SetText("{0}", def.GetBallDamage(card.Level));
            }
        }

        private void SetPips(int level, int maxLevel)
        {
            if (levelPips == null) return;
            for (int i = 0; i < levelPips.Length; i++)
            {
                if (levelPips[i] != null) levelPips[i].SetActive(i < level && i < maxLevel);
            }
        }

        private void HandleClick() => _onPick?.Invoke(_card);
    }
}
