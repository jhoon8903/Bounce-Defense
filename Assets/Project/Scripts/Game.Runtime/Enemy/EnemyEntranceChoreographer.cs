using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 등장 연출의 단일 소유자: 캐스케이드 지연 → 착지셀 음영 텔레그래프 → 낙하(ease-in) → 덜컹(스쿼시) → 활성.
    // 튜닝 상수와 진행 상태(경과 시간)를 전부 여기서 관리한다. 명단(스폰/디스폰)은 EnemyController 소유 —
    // 여기는 등록된 항목의 '연출 진행'만 담당하고, 완료 시 model.MarkActive()로 전투에 넘긴다.
    public sealed class EnemyEntranceChoreographer
    {
        // 등장 연출 튜닝 상수(코드 고정). 인스펙터 튜닝이 필요해지면 SO로 승격 + DI 주입.
        private const float CascadeStagger = 0.1f;  // 배치 순서 간 등장 지연(초) = 우루루루 캐스케이드
        private const float ShadowLead = 0.12f;     // 낙하 전 음영 선행 시간
        private const float DropDuration = 0.28f;   // 낙하 시간
        private const float DropHeight = 4f;        // 착지셀 위 시작 높이(월드 유닛)
        private const float BounceDuration = 0.14f; // 덜컹(스쿼시) 시간
        private const float BounceScale = 0.22f;    // 스쿼시 세기
        private const float ShadowAlpha = 0.35f;    // 음영 투명도

        private sealed class Entry
        {
            public EnemyModel Model;
            public EnemyView View;
            public float Delay;   // 캐스케이드 지연(초)
            public float Elapsed; // 등장 경과 시간
        }

        private readonly Dictionary<string, Entry> _entries = new();
        private readonly List<string> _idCache = new();

        // 등장 시작. 셀은 이미 그리드에 예약된 상태로 호출된다(예약과 동시에 연출만 지연).
        // 낙하 중 무적(콜라이더 off)은 view.BeginEntranceVisual이 처리.
        public void Begin(string id, EnemyModel model, EnemyView view, int cascadeIndex, Vector2 landedCenter, Vector2 worldSize)
        {
            if (model == null || view == null) return;
            model.BeginEntering(landedCenter.y);
            view.BeginEntranceVisual(worldSize);
            view.SetEntranceFrame(false, 0f, landedCenter, 0f);
            _entries[id] = new Entry
            {
                Model = model,
                View = view,
                Delay = Mathf.Max(0, cascadeIndex) * CascadeStagger,
                Elapsed = 0f,
            };
        }

        public void Remove(string id) => _entries.Remove(id);

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

        // 한 적의 등장 연출 1틱. 완료 시 활성(콜라이더 on) → 다음 틱부터 하강.
        private void TickEntry(string id, Entry entry, float dt)
        {
            EnemyModel model = entry.Model;
            EnemyView view = entry.View;
            // 불변식: 뷰가 풀로 반환·재발급됐다면(모델 불일치) 이 엔트리는 유령 — 새 적을 건드리기 전에 폐기.
            if (view == null || !ReferenceEquals(view.Model, model))
            {
                _entries.Remove(id);
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
                // 등장 완료 → 활성. 착지 셀에 정합(하강 시작점 = 셀 중심).
                model.SetPosition(new Vector2(x, landedY));
                view.EndEntranceVisual();
                model.MarkActive();
                _entries.Remove(id);
                return;
            }
            if (elapsed < delay)
            {
                view.SetEntranceFrame(false, 0f, shadowPos, 0f); // 대기: 전부 숨김
                return;
            }
            if (elapsed < dropStart)
            {
                // 음영 페이드인(몸체 숨김) — 착지 지점 텔레그래프.
                float a = Mathf.InverseLerp(delay, dropStart, elapsed) * ShadowAlpha;
                view.SetEntranceFrame(false, a, shadowPos, 0f);
                return;
            }
            if (elapsed < landTime)
            {
                // 낙하(ease-in, t² 가속) — 착지셀 위 DropHeight에서 착지 셀로.
                float t = Mathf.InverseLerp(dropStart, landTime, elapsed);
                float y = Mathf.Lerp(landedY + DropHeight, landedY, t * t);
                model.SetPosition(new Vector2(x, y));
                view.SetEntranceFrame(true, ShadowAlpha, shadowPos, 0f);
                return;
            }
            // 착지 후 덜컹(스쿼시 감쇠) + 음영 페이드아웃.
            model.SetPosition(new Vector2(x, landedY));
            float bt = Mathf.InverseLerp(landTime, endTime, elapsed);
            view.SetEntranceFrame(true, ShadowAlpha * (1f - bt), shadowPos, BounceScale * (1f - bt));
        }
    }
}
