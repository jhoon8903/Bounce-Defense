using System;
using System.Collections.Generic;

namespace Game.Runtime.Enemy
{
    // 적 상태이상(번) 시뮬레이션의 단일 소유자 — EnemyDescentSimulator·EnemyEntranceChoreographer와 같은 SRP 헬퍼.
    // id별 상태 목록을 소유하고 매 틱 감쇠 + 초당 dps flat 적용. 실제 데미지는 컨트롤러 콜백(DamageResolver 경유)이 낸다.
    // 번 데미지가 적을 죽여 디스폰(=Remove)될 수 있으므로 매 적용 후 존재 확인(중도 이탈 안전).
    public sealed class EnemyStatusSimulator
    {
        private const float TickInterval = 1f; // 초당 틱(스펙: 번 1초 틱).

        private readonly Action<string, float> _applyBurn; // (id, dps) → 컨트롤러가 DamageResolver로 flat 적용
        private readonly Dictionary<string, List<StatusInstance>> _byId = new();
        private readonly List<string> _idCache = new();

        public EnemyStatusSimulator(Action<string, float> applyBurn) => _applyBurn = applyBurn;

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
                    st.TickAccumulator += dt;
                    while (st.TickAccumulator >= TickInterval)
                    {
                        st.TickAccumulator -= TickInterval;
                        _applyBurn?.Invoke(id, st.Dps);
                        if (!_byId.ContainsKey(id)) { despawned = true; break; } // 이 틱 데미지로 사망·디스폰됨
                    }
                    if (despawned) break;
                    if (st.Remaining <= 0f) list.RemoveAt(i);
                }

                if (!despawned && _byId.TryGetValue(id, out List<StatusInstance> after) && after.Count == 0)
                    _byId.Remove(id);
            }
        }
    }
}
