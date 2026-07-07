using Game.Combat;
using Game.Core.Mvc;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Enemy
{
    [DisallowMultipleComponent]
    public sealed class EnemyView : BaseView<EnemyModel>, IDamageable, IStatusReceiver
    {
        [SerializeField] private SpriteRenderer blockRenderer;
        [SerializeField] private SpriteRenderer mobRenderer;
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private SpriteRenderer hpBg;
        [SerializeField] private SpriteRenderer hpFill;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private ParticleSystem burnFx;
        [SerializeField] private ParticleSystem freezeFx;

        private System.Action<EnemyView, int> _damageSink;
        private System.Action<EnemyView, float, float, int> _burnSink;
        private System.Action<EnemyView, float, float> _freezeSink;

        private Material _blockMat;
        private Material _mobMat;
        private float _flashAmount;
        private float _frostAmount;
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FrostAmountId = Shader.PropertyToID("_FrostAmount");
        private static readonly int FrostColorId = Shader.PropertyToID("_FrostColor");
        private static readonly Color FlashWhite = Color.white;
        private static readonly Color FrostCyan = new Color(0.55f, 0.85f, 1f, 1f);
        private const float FrozenTint = 0.45f;
        private Vector3 _mobBaseScale = Vector3.one;
        private Vector3 _mobBasePos;
        private bool _mobBaseCaptured;
        private Vector3 _hpBarBaseLocalPos;
        private Vector3 _hpFillBaseScale;
        private Vector3 _hpFillBaseLocalPos;
        private float _hpFillWidth;
        private bool _hpBarBaseCaptured;
        private Transform HpBarRoot => hpBg.transform;

        public void SetDamageSink(System.Action<EnemyView, int> sink) => _damageSink = sink;
        public void SetBurnSink(System.Action<EnemyView, float, float, int> sink) => _burnSink = sink;
        public void SetFreezeSink(System.Action<EnemyView, float, float> sink) => _freezeSink = sink;

        public void SetFootprintSize(Vector2 worldSize)
        {
            if (boxCollider != null) boxCollider.size = worldSize;
            if (blockRenderer != null && blockRenderer.drawMode != SpriteDrawMode.Simple) blockRenderer.size = worldSize;
            CaptureHpBarBase();
            if (HpBarRoot == null) return;
            float fpH = Model != null ? Mathf.Max(1, Model.Footprint.Height) : 1;
            float extraHalf = worldSize.y * (1f - 1f / fpH) * 0.5f;
            HpBarRoot.localPosition = _hpBarBaseLocalPos + new Vector3(0f, -extraHalf, 0f);
        }

        private void CaptureHpBarBase()
        {
            if (_hpBarBaseCaptured || HpBarRoot == null) return;
            _hpBarBaseLocalPos = HpBarRoot.localPosition;
            if (hpFill != null)
            {
                _hpFillBaseScale = hpFill.transform.localScale;
                _hpFillBaseLocalPos = hpFill.transform.localPosition;
                _hpFillWidth = hpFill.sprite != null ? hpFill.sprite.bounds.size.x * _hpFillBaseScale.x : 0f;
            }
            SetBarVisible(false);
            _hpBarBaseCaptured = true;
        }

        protected override void OnModelBound(EnemyModel model)
        {
            if (!_mobBaseCaptured && mobRenderer != null)
            {
                _mobBaseScale = mobRenderer.transform.localScale;
                _mobBasePos = mobRenderer.transform.localPosition;
                _mobBaseCaptured = true;
            }
            if (blockRenderer != null)
            {
                blockRenderer.sprite = model.Definition != null ? model.Definition.BlockSprite : null;
                blockRenderer.enabled = true;
            }
            if (mobRenderer != null)
            {
                mobRenderer.sprite = model.Definition != null ? model.Definition.MobSprite : null;
                mobRenderer.enabled = mobRenderer.sprite != null;
            }
            if (boxCollider != null) boxCollider.enabled = true;
            transform.position = model.Position;
            RefreshHp(model);
        }

        public void BeginEntranceVisual(Vector2 footprintSize)
        {
            EnsureShadow();
            if (shadowRenderer != null)
            {
                shadowRenderer.sprite = blockRenderer != null ? blockRenderer.sprite : null;
                if (shadowRenderer.drawMode != SpriteDrawMode.Simple) shadowRenderer.size = footprintSize;
                Color c = shadowRenderer.color; c.a = 0f; shadowRenderer.color = c;
                shadowRenderer.enabled = false;
            }
            SetBodyVisible(false);
            if (boxCollider != null) boxCollider.enabled = false;
            SetSquash(0f);
        }

        public void SetEntranceFrame(bool bodyVisible, float shadowAlpha, Vector2 shadowWorldPos, float squash)
        {
            SetBodyVisible(bodyVisible);
            if (shadowRenderer != null)
            {
                bool show = shadowAlpha > 0.001f;
                shadowRenderer.enabled = show;
                if (show)
                {
                    shadowRenderer.transform.position = new Vector3(shadowWorldPos.x, shadowWorldPos.y, transform.position.z + 0.01f);
                    Color c = shadowRenderer.color; c.a = shadowAlpha; shadowRenderer.color = c;
                }
            }
            SetSquash(squash);
        }

        public void EndEntranceVisual()
        {
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            SetSquash(0f);
            SetBodyVisible(true);
            if (boxCollider != null) boxCollider.enabled = true;
        }

        private void SetBodyVisible(bool on)
        {
            if (blockRenderer != null) blockRenderer.enabled = on;
            if (mobRenderer != null) mobRenderer.enabled = on && mobRenderer.sprite != null;
            SetBarVisible(on);
        }

        private void SetBarVisible(bool on)
        {
            if (hpBg != null) hpBg.enabled = on;
            if (hpFill != null) hpFill.enabled = on;
        }

        private void SetSquash(float amount)
        {
            if (mobRenderer == null) return;
            amount = Mathf.Clamp01(amount);
            mobRenderer.transform.localScale = Vector3.Scale(_mobBaseScale, new Vector3(1f + amount * 0.5f, 1f - amount, 1f));
        }

        public void SetRecoil(Vector2 localOffset)
        {
            if (mobRenderer == null || !_mobBaseCaptured) return;
            mobRenderer.transform.localPosition = _mobBasePos + (Vector3)localOffset;
        }

        public void SetColliderEnabled(bool on)
        {
            if (boxCollider != null) boxCollider.enabled = on;
        }

        private void EnsureShadow()
        {
            if (shadowRenderer != null) return;
            GameObject go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            shadowRenderer = go.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0f);
            if (blockRenderer != null)
            {
                shadowRenderer.sortingLayerID = blockRenderer.sortingLayerID;
                shadowRenderer.sortingOrder = blockRenderer.sortingOrder - 1;
            }
            shadowRenderer.enabled = false;
        }

        protected override void OnModelUnbound(EnemyModel model) => HideVisuals();

        protected override void OnModelChanged(EnemyModel model) { }

        protected override void RefreshView()
        {
            if (!IsModelBound) return;
            transform.position = Model.Position;
            RefreshHp(Model);
        }

        private void RefreshHp(EnemyModel model)
        {
            if (hpFill == null) return;
            CaptureHpBarBase();
            float pct = model.MaxHp > 0 ? (float)model.Hp / model.MaxHp : 0f;
            Vector3 s = _hpFillBaseScale; s.x = _hpFillBaseScale.x * pct;
            hpFill.transform.localScale = s;
            Vector3 p = _hpFillBaseLocalPos; p.x = _hpFillBaseLocalPos.x - _hpFillWidth * 0.5f * (1f - pct);
            hpFill.transform.localPosition = p;
        }

        private void HideVisuals()
        {
            if (blockRenderer != null) blockRenderer.enabled = false;
            if (mobRenderer != null) mobRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;
            SetBarVisible(false);
        }

        public void ApplyDamage(int amount) => _damageSink?.Invoke(this, amount);

        public void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks) => _burnSink?.Invoke(this, durationSeconds, damagePerSecond, maxStacks);

        public void ApplyFreeze(float durationSeconds, float slow) => _freezeSink?.Invoke(this, durationSeconds, slow);

        public void SetBurning(bool on)
        {
            if (burnFx == null) return;
            if (on)
            {
                if (!burnFx.gameObject.activeSelf) burnFx.gameObject.SetActive(true);
                if (!burnFx.isPlaying) burnFx.Play(true);
            }
            else
            {
                burnFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (burnFx.gameObject.activeSelf) burnFx.gameObject.SetActive(false);
            }
        }

        public void SetFrozen(bool on)
        {
            _frostAmount = on ? FrozenTint : 0f;
            ApplyOverlay();
            if (freezeFx == null) return;
            if (on)
            {
                if (!freezeFx.gameObject.activeSelf) freezeFx.gameObject.SetActive(true);
                if (!freezeFx.isPlaying) freezeFx.Play(true);
            }
            else
            {
                freezeFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (freezeFx.gameObject.activeSelf) freezeFx.gameObject.SetActive(false);
            }
        }

        public void SetHitFlash(float amount)
        {
            _flashAmount = Mathf.Clamp01(amount);
            ApplyOverlay();
        }

        private void EnsureOverlayMaterials()
        {
            if (_blockMat == null && blockRenderer != null)
            {
                _blockMat = blockRenderer.material;
                _blockMat.SetColor(FlashColorId, FlashWhite);
                _blockMat.SetColor(FrostColorId, FrostCyan);
            }
            if (_mobMat == null && mobRenderer != null)
            {
                _mobMat = mobRenderer.material;
                _mobMat.SetColor(FlashColorId, FlashWhite);
                _mobMat.SetColor(FrostColorId, FrostCyan);
            }
        }

        private void ApplyOverlay()
        {
            EnsureOverlayMaterials();
            if (_blockMat != null)
            {
                _blockMat.SetFloat(FlashAmountId, _flashAmount);
                _blockMat.SetFloat(FrostAmountId, _frostAmount);
            }
            if (_mobMat != null)
            {
                _mobMat.SetFloat(FlashAmountId, _flashAmount);
                _mobMat.SetFloat(FrostAmountId, _frostAmount);
            }
        }

        public override void OnInactive()
        {
            base.OnInactive();
            _damageSink = null;
            _burnSink = null;
            _freezeSink = null;
            SetHitFlash(0f);
            SetBurning(false);
            SetFrozen(false);
            HideVisuals();
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            SetSquash(0f);
            SetRecoil(Vector2.zero);
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
