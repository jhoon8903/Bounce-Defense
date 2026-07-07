using System;
using System.Collections.Generic;
using Game.Roguelike;
using Game.Skills;
using UnityEngine;

namespace Game.Runtime.UI
{
    public sealed class CardSelectView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform cardTray;
        [SerializeField] private CardView activeTemplate;
        [SerializeField] private CardView passiveTemplate;
        [SerializeField] private SkillLoadoutView loadoutView;

        private readonly List<CardView> _activePool = new();
        private readonly List<CardView> _passivePool = new();
        private readonly List<CardView> _shown = new();
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
            ReturnShown();
            int slot = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                SkillCard card = cards[i];
                if (!card.IsValid) continue;
                bool passive = card.Definition.Category == SkillCategory.Passive && passiveTemplate != null;
                CardView template = passive ? passiveTemplate : activeTemplate;
                if (template == null) continue;
                CardView instance = Rent(template, passive ? _passivePool : _activePool);
                instance.transform.SetSiblingIndex(slot++);
                instance.gameObject.SetActive(true);
                instance.Bind(card, HandlePick);
                _shown.Add(instance);
            }
            SetVisible(true);
        }

        private CardView Rent(CardView template, List<CardView> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].gameObject.activeSelf) return pool[i];
            }
            CardView inst = Instantiate(template, cardTray != null ? cardTray : transform);
            pool.Add(inst);
            return inst;
        }

        public void Hide()
        {
            _onPick = null;
            ReturnShown();
            SetVisible(false);
        }

        private void HandlePick(SkillCard card)
        {
            Action<SkillCard> cb = _onPick;
            _onPick = null;
            cb?.Invoke(card);
        }

        private void ReturnShown()
        {
            for (int i = 0; i < _shown.Count; i++)
            {
                if (_shown[i] != null) _shown[i].gameObject.SetActive(false);
            }
            _shown.Clear();
        }

        private void SetVisible(bool visible)
        {
            GameObject target = panelRoot != null ? panelRoot : gameObject;
            if (target.activeSelf != visible) target.SetActive(visible);
        }

        private void EnsureAwake()
        {
            if (_awoke) return;
            if (activeTemplate != null) activeTemplate.gameObject.SetActive(false);
            if (passiveTemplate != null) passiveTemplate.gameObject.SetActive(false);
            _awoke = true;
        }
    }
}
