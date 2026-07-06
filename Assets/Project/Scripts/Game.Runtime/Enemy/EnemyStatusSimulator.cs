using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 상태이상(번·냉동) 시뮬레이션의 단일 소유자 — EnemyDescentSimulator·EnemyEntranceChoreographer와 같은 SRP 헬퍼.
    // id별 상태 목록을 소유하고 매 틱 감쇠. Burn = 초당 dps flat(컨트롤러 콜백→DamageResolver). Freeze = 하강 슬로우
    // (매 틱 현재 슬로우를 콜백으로 모델에 반영, 만료 시 0). 번 데미지가 적을 죽여 디스폰될 수 있어 매 적용 후 존재 확인.
    public sealed class EnemyStatusSimulator
    {
        private const float TickInterval = 1f; // 초당 틱(스펙: 번 1초 틱).

        private readonly Action<string, float> _applyBurn;     // (id, dps) → 컨트롤러가 DamageResolver로 flat 적용
        private readonly Action<string, float> _setFreezeSlow; // (id, slow) → 컨트롤러가 모델 하강 감속률 세팅(0=해제)
        private readonly Action<string, bool> _setBurning;     // (id, on) → 컨트롤러가 EnemyView.SetBurning(불꽃 VFX). 첫 스택=on, 전 스택 만료=off.
        private readonly Action<string, bool> _setFrozen;      // (id, on) → 컨트롤러가 EnemyView.SetFrozen(서리 VFX). 첫 냉동=on, 전 냉동 만료=off.
        private readonly Dictionary<string, List<StatusInstance>> _byId = new();
        private readonly HashSet<string> _burningIds = new();  // 현재 번 붙은 id(시각 상태 전이 감지용)
        private readonly HashSet<string> _frozenIds = new();   // 현재 냉동 붙은 id(시각 상태 전이 감지용)
        private readonly List<string> _idCache = new();

        public EnemyStatusSimulator(Action<string, float> applyBurn, Action<string, float> setFreezeSlow, Action<string, bool> setBurning, Action<string, bool> setFrozen)
        {
            _applyBurn = applyBurn;
            _setFreezeSlow = setFreezeSlow;
            _setBurning = setBurning;
            _setFrozen = setFrozen;
        }

        // 번 시각 상태 전이 감지: 리스트에 Burn이 있으면 on(신규만 신호), 없으면 off(있었으면만 신호). 디스폰은 별도(뷰 풀반환이 끔).
        private void UpdateBurnState(string id, List<StatusInstance> list)
        {
            bool hasBurn = false;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Type == EnemyStatusType.Burn) { hasBurn = true; break; }
            if (hasBurn) { if (_burningIds.Add(id)) _setBurning?.Invoke(id, true); }
            else { if (_burningIds.Remove(id)) _setBurning?.Invoke(id, false); }
        }

        // 냉동 시각 상태 전이 감지: 리스트에 Freeze가 있으면 on(신규만 신호), 없으면 off(있었으면만 신호). 디스폰은 별도(뷰 풀반환이 끔).
        private void UpdateFreezeState(string id, List<StatusInstance> list)
        {
            bool hasFreeze = false;
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                    if (list[i].Type == EnemyStatusType.Freeze) { hasFreeze = true; break; }
            if (hasFreeze) { if (_frozenIds.Add(id)) _setFrozen?.Invoke(id, true); }
            else { if (_frozenIds.Remove(id)) _setFrozen?.Invoke(id, false); }
        }

        // 번 부여(독립타이머 스택, 캡). 캡 초과면 잔여시간이 가장 적은 스택을 새 값으로 갱신(refresh).
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
                if (list[i].Remaining < weakestRemaining) { weakestRemaining = list[i].Remaining; weakest = i; }
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
            UpdateBurnState(id, list); // 첫 번 스택이면 불꽃 on
        }

        // 냉동 부여(무스택 refresh): 기존 Freeze 있으면 더 긴 지속·더 강한 슬로우로 갱신, 없으면 1건 추가.
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
            UpdateFreezeState(id, list); // 첫 냉동이면 서리 on
        }

        public void Remove(string id) { _byId.Remove(id); _burningIds.Remove(id); _frozenIds.Remove(id); } // 디스폰 — 뷰 풀반환이 VFX 끔(시그널 불필요)

        public void Clear() { _byId.Clear(); _burningIds.Clear(); _frozenIds.Clear(); }

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
                            if (!_byId.ContainsKey(id)) { despawned = true; break; } // 이 틱 데미지로 사망·디스폰됨
                        }
                        if (despawned) break;
                    }
                    if (st.Remaining <= 0f) list.RemoveAt(i);
                }

                if (despawned) { _burningIds.Remove(id); _frozenIds.Remove(id); continue; } // 디스폰(모델 없음) — 뷰 풀반환이 VFX 끔

                // 냉동 슬로우 반영: 남은 Freeze 중 가장 강한 슬로우(없으면 0=해제). 모델 setter가 무변경이면 조기반환.
                if (_byId.TryGetValue(id, out List<StatusInstance> after))
                {
                    if (after.Count == 0)
                    {
                        _byId.Remove(id);
                        _setFreezeSlow?.Invoke(id, 0f); // 모든 상태 소멸 → 슬로우 해제
                        UpdateBurnState(id, null);       // 번도 소멸 → 불꽃 off
                        UpdateFreezeState(id, null);     // 냉동도 소멸 → 서리 off
                    }
                    else
                    {
                        float slow = 0f;
                        for (int i = 0; i < after.Count; i++)
                            if (after[i].Type == EnemyStatusType.Freeze && after[i].Slow > slow) slow = after[i].Slow;
                        _setFreezeSlow?.Invoke(id, slow);
                        UpdateBurnState(id, after);       // 번 만료(냉동 잔존)면 불꽃 off
                        UpdateFreezeState(id, after);     // 냉동 만료(번 잔존)면 서리 off
                    }
                }
            }
        }
    }
}
