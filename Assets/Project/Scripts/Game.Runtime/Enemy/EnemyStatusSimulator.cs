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
        private readonly Dictionary<string, List<StatusInstance>> _byId = new();
        private readonly List<string> _idCache = new();

        public EnemyStatusSimulator(Action<string, float> applyBurn, Action<string, float> setFreezeSlow)
        {
            _applyBurn = applyBurn;
            _setFreezeSlow = setFreezeSlow;
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

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Type != EnemyStatusType.Freeze) continue;
                list[i].Remaining = Mathf.Max(list[i].Remaining, duration);
                list[i].Slow = Mathf.Max(list[i].Slow, slow);
                return;
            }
            list.Add(new StatusInstance(EnemyStatusType.Freeze, duration, 0f) { Slow = slow });
        }

        public void Remove(string id) => _byId.Remove(id);

        public void Clear() => _byId.Clear();

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

                if (despawned) continue; // 디스폰됨(모델 없음) — 슬로우 반영 불필요

                // 냉동 슬로우 반영: 남은 Freeze 중 가장 강한 슬로우(없으면 0=해제). 모델 setter가 무변경이면 조기반환.
                if (_byId.TryGetValue(id, out List<StatusInstance> after))
                {
                    if (after.Count == 0)
                    {
                        _byId.Remove(id);
                        _setFreezeSlow?.Invoke(id, 0f); // 모든 상태 소멸 → 슬로우 해제
                    }
                    else
                    {
                        float slow = 0f;
                        for (int i = 0; i < after.Count; i++)
                            if (after[i].Type == EnemyStatusType.Freeze && after[i].Slow > slow) slow = after[i].Slow;
                        _setFreezeSlow?.Invoke(id, slow);
                    }
                }
            }
        }
    }
}
