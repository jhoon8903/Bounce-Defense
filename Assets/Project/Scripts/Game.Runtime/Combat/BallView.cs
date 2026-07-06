using Game.Core.Mvc;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallView : BaseView<BallModel>
    {
        [SerializeField] private TrailRenderer trail; // 이동 꼬리(#1). 스폰=Clear+emit, 디스폰=stop+Clear(풀 재사용 시 이전 위치→스폰 위치 스트릭 방지).

        private SpriteRenderer _spriteRenderer;

        protected override void OnModelBound(BallModel model)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>(true);
            transform.position = model.Position;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
            if (trail != null) { trail.Clear(); trail.emitting = true; } // 위치 세팅 후 Clear → 풀 이전 위치서 줄 긋기 방지
        }

        protected override void OnModelUnbound(BallModel model)
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
            if (trail != null) { trail.emitting = false; trail.Clear(); }
        }

        protected override void OnModelChanged(BallModel model) { }

        protected override void RefreshView()
        {
            if (!IsModelBound) return;
            transform.position = Model.Position;
        }

        public override void OnInactive()
        {
            base.OnInactive();
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
            if (trail != null) { trail.emitting = false; trail.Clear(); }
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
