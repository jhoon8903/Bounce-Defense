using System;
using System.Collections.Generic;
using Game.Roguelike;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    // 3택 카드 드래프트 패널. 컨트롤러가 Show(cards,onPick)/Hide로 구동(모델 관찰 아님, 명령형).
    // 카드는 카테고리별 템플릿(ActiveCard/PassiveCard)을 복제해 트레이(HorizontalLayoutGroup)에 배치.
    // Rerole은 스펙 §55 '구현 제외' → SetActive(false)로 숨김(오브젝트는 보존).
    public sealed class CardSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;         // 표시/숨김 토글 대상. 미할당 시 자기 gameObject.
        [SerializeField] private Transform cardTray;           // SkillCardTray
        [SerializeField] private CardView activeTemplate;      // ActiveCard
        [SerializeField] private CardView passiveTemplate;     // PassiveCard
        [SerializeField] private SkillLoadoutView loadoutView; // SkillMonitor// Rerole (스펙 제외 → 숨김)

        private readonly List<CardView> _spawned = new();
        private Action<SkillCard> _onPick;
        private bool _awoke;

        private void Awake()
        {
            if (activeTemplate != null) activeTemplate.gameObject.SetActive(false);
            if (passiveTemplate != null) passiveTemplate.gameObject.SetActive(false);
            _awoke = true;
            SetVisible(false);
        }

        public void BindLoadout(PlayerLoadout loadout)
        {
            if (loadoutView != null) loadoutView.Bind(loadout);
        }

        public void Show(IReadOnlyList<SkillCard> cards, Action<SkillCard> onPick)
        {
            EnsureAwake();
            _onPick = onPick;
            ClearSpawned();
            Transform parent = cardTray != null ? cardTray : transform;
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                if (!card.IsValid) continue;
                CardView template = card.Definition.Category == SkillCategory.Passive && passiveTemplate != null
                    ? passiveTemplate
                    : activeTemplate;
                if (template == null) continue;
                CardView instance = Instantiate(template, parent);
                instance.gameObject.SetActive(true);
                instance.Bind(card, HandlePick);
                _spawned.Add(instance);
            }
            SetVisible(true);
        }

        public void Hide()
        {
            _onPick = null;
            ClearSpawned();
            SetVisible(false);
        }

        // 첫 클릭만 유효(중복 선택 방지) — 콜백 소비 후 null.
        private void HandlePick(SkillCard card)
        {
            Action<SkillCard> cb = _onPick;
            _onPick = null;
            cb?.Invoke(card);
        }

        private void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++) if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            _spawned.Clear();
        }

        private void SetVisible(bool visible)
        {
            GameObject target = panelRoot != null ? panelRoot : gameObject;
            if (target.activeSelf != visible) target.SetActive(visible);
        }

        // 템플릿이 비활성 상태여도 Show가 처음 호출되기 전 Awake가 안 돌았을 수 있어 방어.
        private void EnsureAwake()
        {
            if (_awoke) return;
            if (activeTemplate != null) activeTemplate.gameObject.SetActive(false);
            if (passiveTemplate != null) passiveTemplate.gameObject.SetActive(false);
            _awoke = true;
        }
    }
}
