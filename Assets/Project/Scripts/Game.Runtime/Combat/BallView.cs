using Game.Core.Mvc;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class BallView : BaseView<BallModel>
    {
        private SpriteRenderer _spriteRenderer;

        protected override void OnModelBound(BallModel model)
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            transform.position = model.Position;
            if (_spriteRenderer != null) _spriteRenderer.enabled = true;
        }

        protected override void OnModelUnbound(BallModel model)
        {
            if (_spriteRenderer != null) _spriteRenderer.enabled = false;
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
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
