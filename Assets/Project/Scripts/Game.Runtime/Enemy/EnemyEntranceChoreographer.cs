using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    public sealed class EnemyEntranceChoreographer
    {
        private const float CascadeStagger = 0.1f;
        private const float ShadowLead = 0.12f;
        private const float DropDuration = 0.28f;
        private const float DropHeight = 4f;
        private const float BounceDuration = 0.14f;
        private const float BounceScale = 0.22f;
        private const float ShadowAlpha = 0.35f;

        private sealed class Entry
        {
            public EnemyModel Model;
            public EnemyView View;
            public float Delay;
            public float Elapsed;
        }

        private readonly Dictionary<string, Entry> _entries = new();
        private readonly Stack<Entry> _entryPool = new();
        private readonly List<string> _idCache = new();

        public void Begin(string id, EnemyModel model, EnemyView view, int cascadeIndex, Vector2 landedCenter, Vector2 worldSize)
        {
            if (model == null || view == null) return;
            model.BeginEntering(landedCenter.y);
            view.BeginEntranceVisual(worldSize);
            view.SetEntranceFrame(false, 0f, landedCenter, 0f);
            Entry e = _entryPool.Count > 0 ? _entryPool.Pop() : new Entry();
            e.Model = model;
            e.View = view;
            e.Delay = Mathf.Max(0, cascadeIndex) * CascadeStagger;
            e.Elapsed = 0f;
            _entries[id] = e;
        }

        public void Remove(string id) => ReturnEntry(id);

        private void ReturnEntry(string id)
        {
            if (!_entries.TryGetValue(id, out Entry e)) return;
            e.Model = null;
            e.View = null;
            _entryPool.Push(e);
            _entries.Remove(id);
        }

        public void Tick(float deltaTime)
        {
            if (_entries.Count == 0) return;
            _idCache.Clear();
            _idCache.AddRange(_entries.Keys);
            for (int i = 0; i < _idCache.Count; i++)
            {
                string id = _idCache[i];
                if (_entries.TryGetValue(id, out Entry entry)) TickEntry(id, entry, deltaTime);
            }
        }

        private void TickEntry(string id, Entry entry, float dt)
        {
            EnemyModel model = entry.Model;
            EnemyView view = entry.View;
            if (view == null || !ReferenceEquals(view.Model, model))
            {
                ReturnEntry(id);
                return;
            }
            entry.Elapsed += dt;
            float elapsed = entry.Elapsed;

            float delay = entry.Delay;
            float dropStart = delay + ShadowLead;
            float landTime = dropStart + DropDuration;
            float endTime = landTime + BounceDuration;

            float x = model.Position.x;
            float landedY = model.LandedY;
            Vector2 shadowPos = new Vector2(x, landedY);

            if (elapsed >= endTime)
            {
                model.SetPosition(new Vector2(x, landedY));
                view.EndEntranceVisual();
                model.MarkActive();
                ReturnEntry(id);
                return;
            }
            if (elapsed < delay)
            {
                view.SetEntranceFrame(false, 0f, shadowPos, 0f);
                return;
            }
            if (elapsed < dropStart)
            {
                float a = Mathf.InverseLerp(delay, dropStart, elapsed) * ShadowAlpha;
                view.SetEntranceFrame(false, a, shadowPos, 0f);
                return;
            }
            if (elapsed < landTime)
            {
                float t = Mathf.InverseLerp(dropStart, landTime, elapsed);
                float y = Mathf.Lerp(landedY + DropHeight, landedY, t * t);
                model.SetPosition(new Vector2(x, y));
                view.SetEntranceFrame(true, ShadowAlpha, shadowPos, 0f);
                return;
            }
            model.SetPosition(new Vector2(x, landedY));
            float bt = Mathf.InverseLerp(landTime, endTime, elapsed);
            view.SetEntranceFrame(true, ShadowAlpha * (1f - bt), shadowPos, BounceScale * (1f - bt));
        }
    }
}
