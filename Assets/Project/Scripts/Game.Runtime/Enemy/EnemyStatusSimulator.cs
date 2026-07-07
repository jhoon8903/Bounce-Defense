using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyStatusSimulator
    {
        private const float TickInterval = 1f;

        private readonly Action<string, float> _applyBurn;
        private readonly Action<string, float> _setFreezeSlow;
        private readonly Action<string, bool> _setBurning;
        private readonly Action<string, bool> _setFrozen;
        private readonly Dictionary<string, List<StatusInstance>> _byId = new();
        private readonly HashSet<string> _burningIds = new();
        private readonly HashSet<string> _frozenIds = new();
        private readonly List<string> _idCache = new();

        public EnemyStatusSimulator(Action<string, float> applyBurn, Action<string, float> setFreezeSlow, Action<string, bool> setBurning, Action<string, bool> setFrozen)
        {
            _applyBurn = applyBurn;
            _setFreezeSlow = setFreezeSlow;
            _setBurning = setBurning;
            _setFrozen = setFrozen;
        }

        private void UpdateBurnState(string id, List<StatusInstance> list)
        {
            bool hasBurn = false;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Type == EnemyStatusType.Burn)
                    {
                        hasBurn = true;
                        break;
                    }
                }
            if (hasBurn) { if (_burningIds.Add(id)) _setBurning?.Invoke(id, true); }
            else { if (_burningIds.Remove(id)) _setBurning?.Invoke(id, false); }
        }

        private void UpdateFreezeState(string id, List<StatusInstance> list)
        {
            bool hasFreeze = false;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Type == EnemyStatusType.Freeze)
                    {
                        hasFreeze = true;
                        break;
                    }
                }
            if (hasFreeze) { if (_frozenIds.Add(id)) _setFrozen?.Invoke(id, true); }
            else { if (_frozenIds.Remove(id)) _setFrozen?.Invoke(id, false); }
        }

        public void ApplyBurn(string id, float duration, float dps, int maxStacks)
        {
            if (string.IsNullOrEmpty(id) || duration <= 0f || dps <= 0f) return;
            if (!_byId.TryGetValue(id, out List<StatusInstance> list))
            {
                list = new List<StatusInstance>();
                _byId[id] = list;
            }

            int burnCount = 0;
            int weakest = -1;
            float weakestRemaining = float.MaxValue;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type != EnemyStatusType.Burn) continue;
                burnCount++;
                if (list[i].Remaining < weakestRemaining)
                {
                    weakestRemaining = list[i].Remaining;
                    weakest = i;
                }
            }

            if (maxStacks > 0 && burnCount >= maxStacks && weakest >= 0)
            {
                list[weakest].Remaining = duration;
                list[weakest].Dps = dps;
                list[weakest].TickAccumulator = 0f;
            }
            else
            {
                list.Add(new StatusInstance(EnemyStatusType.Burn, duration, dps));
            }
            UpdateBurnState(id, list);
        }

        public void ApplyFreeze(string id, float duration, float slow)
        {
            if (string.IsNullOrEmpty(id) || duration <= 0f || slow <= 0f) return;
            if (!_byId.TryGetValue(id, out List<StatusInstance> list))
            {
                list = new List<StatusInstance>();
                _byId[id] = list;
            }

            bool refreshed = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type != EnemyStatusType.Freeze) continue;
                list[i].Remaining = Mathf.Max(list[i].Remaining, duration);
                list[i].Slow = Mathf.Max(list[i].Slow, slow);
                refreshed = true;
                break;
            }
            if (!refreshed) list.Add(new StatusInstance(EnemyStatusType.Freeze, duration, 0f) { Slow = slow });
            UpdateFreezeState(id, list);
        }

        public void Remove(string id)
        {
            _byId.Remove(id);
            _burningIds.Remove(id);
            _frozenIds.Remove(id);
        }

        public void Clear()
        {
            _byId.Clear();
            _burningIds.Clear();
            _frozenIds.Clear();
        }

        public void Tick(float dt)
        {
            if (_byId.Count == 0 || dt <= 0f) return;
            _idCache.Clear();
            _idCache.AddRange(_byId.Keys);
            for (int k = 0; k < _idCache.Count; k++)
            {
                string id = _idCache[k];
                if (!_byId.TryGetValue(id, out List<StatusInstance> list)) continue;

                bool despawned = false;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    StatusInstance st = list[i];
                    st.Remaining -= dt;
                    if (st.Type == EnemyStatusType.Burn)
                    {
                        st.TickAccumulator += dt;
                        while (st.TickAccumulator >= TickInterval)
                        {
                            st.TickAccumulator -= TickInterval;
                            _applyBurn?.Invoke(id, st.Dps);
                            if (!_byId.ContainsKey(id))
                            {
                                despawned = true;
                                break;
                            }
                        }
                        if (despawned) break;
                    }
                    if (st.Remaining <= 0f) list.RemoveAt(i);
                }

                if (despawned)
                {
                    _burningIds.Remove(id);
                    _frozenIds.Remove(id);
                    continue;
                }

                if (_byId.TryGetValue(id, out List<StatusInstance> after))
                {
                    if (after.Count == 0)
                    {
                        _byId.Remove(id);
                        _setFreezeSlow?.Invoke(id, 0f);
                        UpdateBurnState(id, null);
                        UpdateFreezeState(id, null);
                    }
                    else
                    {
                        float slow = 0f;
                        for (int i = 0; i < after.Count; i++)
                        {
                            if (after[i].Type == EnemyStatusType.Freeze && after[i].Slow > slow) slow = after[i].Slow;
                        }
                        _setFreezeSlow?.Invoke(id, slow);
                        UpdateBurnState(id, after);
                        UpdateFreezeState(id, after);
                    }
                }
            }
        }
    }
}
