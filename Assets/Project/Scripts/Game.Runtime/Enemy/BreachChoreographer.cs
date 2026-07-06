using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 방어선 침범 연출(스펙 #3): 끝 라인 도달 → 부들부들 떨림(~1s) → 캐릭터로 돌진(~0.32s) → 충격 콜백.
    // EntranceChoreographer 미러: 명단(스폰/디스폰)은 EnemyController 소유, 여기는 연출 진행만.
    // 완료 시 onImpact(id, damage, pos) 콜백 → 컨트롤러가 HP감소+플로팅텍스트+피+디스폰.
    public sealed class BreachChoreographer
    {
        private const float TrembleDuration = 1.0f;  // 부들부들 시간
        private const float LungeDuration = 0.32f;    // 캐릭터로 돌진 시간
        private const float TrembleAmp = 0.09f;       // 떨림 진폭(월드 유닛, 몹 렌더러 로컬)
        private const float TrembleFreq = 42f;        // 떨림 빈도

        private sealed class Entry
        {
            public EnemyModel Model;
            public EnemyView View;
            public int Damage;
            public Vector2 Start;
            public Vector2 Target;
            public float Elapsed;
        }

        private readonly Dictionary<string, Entry> _entries = new();
        private readonly List<string> _idCache = new();
        private readonly Action<string, int, Vector2> _onImpact;

        public BreachChoreographer(Action<string, int, Vector2> onImpact) => _onImpact = onImpact;

        // 침범 연출 시작. model은 이미 BeginBreaching(하강 제외) 상태로 넘어온다.
        public void Begin(string id, EnemyModel model, EnemyView view, int damage, Vector2 target)
        {
            if (model == null || view == null) return;
            _entries[id] = new Entry
            {
                Model = model, View = view, Damage = damage,
                Start = model.Position, Target = target, Elapsed = 0f,
            };
        }

        public void Remove(string id)
        {
            if (_entries.TryGetValue(id, out Entry e) && e.View != null && ReferenceEquals(e.View.Model, e.Model))
                e.View.SetRecoil(Vector2.zero); // 떨림 오프셋 원복(풀 재사용 대비)
            _entries.Remove(id);
        }

        public void Tick(float dt)
        {
            if (_entries.Count == 0) return;
            _idCache.Clear();
            _idCache.AddRange(_entries.Keys);
            for (int i = 0; i < _idCache.Count; i++)
            {
                string id = _idCache[i];
                if (_entries.TryGetValue(id, out Entry e)) TickEntry(id, e, dt);
            }
        }

        private void TickEntry(string id, Entry e, float dt)
        {
            EnemyModel model = e.Model;
            EnemyView view = e.View;
            if (view == null || !ReferenceEquals(view.Model, model)) { _entries.Remove(id); return; } // 유령(디스폰/풀 재발급)

            e.Elapsed += dt;
            if (e.Elapsed < TrembleDuration)
            {
                // 부들부들: 몹 렌더러 로컬 지터(격자/콜라이더/모델 위치 불변).
                float t = e.Elapsed;
                float ox = Mathf.Sin(t * TrembleFreq) * TrembleAmp;
                float oy = Mathf.Sin(t * TrembleFreq * 1.37f + 1.1f) * TrembleAmp * 0.6f;
                view.SetRecoil(new Vector2(ox, oy));
                return;
            }

            float lt = e.Elapsed - TrembleDuration;
            if (lt < LungeDuration)
            {
                // 캐릭터로 돌진(ease-in 가속). 몹 오프셋 원복 후 전체가 날아감.
                view.SetRecoil(Vector2.zero);
                float k = Mathf.Clamp01(lt / LungeDuration);
                model.SetPosition(Vector2.Lerp(e.Start, e.Target, k * k));
                return;
            }

            // 충격 → 콜백(HP감소+텍스트+피). EnemyController가 Despawn하며 여기서 Remove.
            _entries.Remove(id);
            _onImpact?.Invoke(id, e.Damage, e.Target);
        }
    }
}
