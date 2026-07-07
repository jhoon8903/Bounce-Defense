using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class BreachChoreographer
    {
        private const float TrembleDuration = 1.0f;
        private const float LungeDuration = 0.32f;
        private const float TrembleAmp = 0.09f;
        private const float TrembleFreq = 42f;

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
                e.View.SetRecoil(Vector2.zero);
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
            if (view == null || !ReferenceEquals(view.Model, model))
            {
                _entries.Remove(id);
                return;
            }

            e.Elapsed += dt;
            if (e.Elapsed < TrembleDuration)
            {
                float t = e.Elapsed;
                float ox = Mathf.Sin(t * TrembleFreq) * TrembleAmp;
                float oy = Mathf.Sin(t * TrembleFreq * 1.37f + 1.1f) * TrembleAmp * 0.6f;
                view.SetRecoil(new Vector2(ox, oy));
                return;
            }

            float lt = e.Elapsed - TrembleDuration;
            if (lt < LungeDuration)
            {
                view.SetRecoil(Vector2.zero);
                float k = Mathf.Clamp01(lt / LungeDuration);
                model.SetPosition(Vector2.Lerp(e.Start, e.Target, k * k));
                return;
            }

            _entries.Remove(id);
            _onImpact?.Invoke(id, e.Damage, e.Target);
        }
    }
}
