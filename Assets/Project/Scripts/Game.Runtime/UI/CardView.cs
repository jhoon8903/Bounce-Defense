using System;
using Game.Roguelike;
using Game.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // 카드 1장(스펙 §47). 실제 템플릿(ActiveCard/PassiveCard)에 맞춤:
    //  이름 · 아이콘 · 한 줄 설명 · 레벨핍(1/2/3) · NEW 뱃지 · ★볼데미지(액티브: 현재→업그레이드).
    //  클릭 → onPick. CardSelectView가 카테고리별로 이 컴포넌트가 붙은 템플릿을 복제해 사용.
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;          // 헤더 스킬 아이콘
        [SerializeField] private TMP_Text nameText;        // Inner/Name/SkillName
        [SerializeField] private TMP_Text descriptionText; // Inner/Status/Desc

        [Header("Damage (액티브 전용, 패시브는 미할당)")]
        [SerializeField] private GameObject damageGroup;     // Inner/Status/StatusProperty
        [SerializeField] private TMP_Text currentValueText;  // CurrentProperty (현재/신규 볼데미지)
        [SerializeField] private GameObject upgradeMarker;   // UpgradeMarker (업그레이드 카드만)
        [SerializeField] private TMP_Text upgradeValueText;  // UpgradeProperty (상승 후 볼데미지)

        [Header("State markers")]
        [SerializeField] private GameObject newBadge;        // NewMarker (신규 카드만)
        [SerializeField] private GameObject[] levelPips;     // SkillLevel/LevelN/LevelMarker, 채운 개수 = 제안 레벨
        [SerializeField] private EventBtn selectButton;      // 카드 루트 EventBtn(throttle+전역락, 동시선택 차단)

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

        // 액티브: 신규면 현재값만, 업그레이드면 "현재→상승" 표시. 패시브: damageGroup 미할당 → 자동 생략.
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
                if (upgradeValueText != null)
                {
                    upgradeValueText.gameObject.SetActive(true);
                    upgradeValueText.SetText("{0}", def.GetBallDamage(card.Level));
                }
            }
        }

        private void SetPips(int level, int maxLevel)
        {
            if (levelPips == null) return;
            for (int i = 0; i < levelPips.Length; i++)
                if (levelPips[i] != null) levelPips[i].SetActive(i < level && i < maxLevel);
        }

        private void HandleClick() => _onPick?.Invoke(_card);
    }
}
