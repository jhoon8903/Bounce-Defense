using Game.Combat;
using Game.Core.Mvc;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.Enemy
{
    // 적 뷰: 돌 타일(block) + 위 몹 스프라이트 + 콜라이더(볼 피격 대상) + HP 숫자.
    // IDamageable로서 볼 데미지를 받되, 실제 처리(HP감산/사망/디스폰)는 컨트롤러로 포워드(뷰는 dumb 렌더러+브리지).
    [DisallowMultipleComponent]
    public sealed class EnemyView : BaseView<EnemyModel>, IDamageable, IStatusReceiver
    {
        [SerializeField] private SpriteRenderer blockRenderer; // 돌 타일(콜라이더 몸체와 정렬)
        [SerializeField] private SpriteRenderer mobRenderer;   // 위에 올라가는 몬스터(순수 비주얼)
        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private Canvas hpCanvas;// 볼 CircleCast 대상(Enemy 레이어)
        [SerializeField] private Image hpSliderFill;              // 몸체 HP 숫자(선택 — 미배선 시 스킵)
        [SerializeField] private SpriteRenderer shadowRenderer; // 착지 텔레그래프 음영(미배선 시 런타임 자동생성)

        private System.Action<EnemyView, int> _damageSink;
        private System.Action<EnemyView, float, float, int> _burnSink; // (view, duration, dps, maxStacks)

        // 히트 화이트 플래시(SpriteHitFlash 셰이더). MaterialPropertyBlock으로 렌더러별 주입(제로할당·공유머티리얼 무변경).
        private MaterialPropertyBlock _flashMpb;
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly Color FlashWhite = Color.white;
        private Vector3 _mobBaseScale = Vector3.one; // 스쿼시 기준(프리팹 몹 스케일이 1이 아닐 수 있어 캡처)
        private bool _mobBaseCaptured;
        private CanvasGroup _hpCanvasGroup;
        private RectTransform _hpBarRect;            // HP 바(월드 캔버스) RectTransform — 풋프린트 높이별 위치 보정
        private Vector2 _hpBarBaseAnchored;          // 프리팹 기준 위치(1칸 높이 기준). 2칸 이상이면 그만큼 아래로.
        private bool _hpBarBaseCaptured;

        public void SetDamageSink(System.Action<EnemyView, int> sink) => _damageSink = sink;
        public void SetBurnSink(System.Action<EnemyView, float, float, int> sink) => _burnSink = sink;
        
        public void SetFootprintSize(Vector2 worldSize)
        {
            if (boxCollider != null) boxCollider.size = worldSize;
            if (blockRenderer != null && blockRenderer.drawMode != SpriteDrawMode.Simple) blockRenderer.size = worldSize;
            CaptureHpBarBase();
            if (_hpBarRect == null) return;
            float fpH = Model != null ? Mathf.Max(1, Model.Footprint.Height) : 1;
            float extraHalf = worldSize.y * (1f - 1f / fpH) * 0.5f;
            _hpBarRect.anchoredPosition = _hpBarBaseAnchored + new Vector2(0f, -extraHalf);
        }

        // 프리팹 기준 HP 바 위치를 최초 1회 캡처(풀 재사용 대비 원본 보존).
        private void CaptureHpBarBase()
        {
            if (_hpBarBaseCaptured || hpCanvas == null) return;
            _hpCanvasGroup =  hpCanvas.transform.GetComponent<CanvasGroup>();
            _hpCanvasGroup.alpha = 0;
            _hpBarRect = hpCanvas.transform as RectTransform;
            if (_hpBarRect != null) _hpBarBaseAnchored = _hpBarRect.anchoredPosition;
            _hpBarBaseCaptured = true;
        }

        protected override void OnModelBound(EnemyModel model)
        {
            if (!_mobBaseCaptured && mobRenderer != null)
            {
                _mobBaseScale = mobRenderer.transform.localScale;
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
            hpCanvas.worldCamera = Camera.main;
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

        // 등장 한 프레임: 몸체 표시여부 · 음영 알파/월드위치(고정 착지셀) · 몹 스쿼시량(0~1).
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

        // 등장 완료: 음영 끄고 스쿼시 복구, 몸체/콜라이더 on(전투 개시).
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
            if (_hpCanvasGroup != null)  _hpCanvasGroup.alpha = on ? 1 : 0;
        }

        // 덜컹 스쿼시: 위 몹만 눌러 밟는 느낌(넓게+낮게). 콜라이더 있는 돌 블록은 안 건드림.
        private void SetSquash(float amount)
        {
            if (mobRenderer == null) return;
            amount = Mathf.Clamp01(amount);
            mobRenderer.transform.localScale = Vector3.Scale(_mobBaseScale, new Vector3(1f + amount * 0.5f, 1f - amount, 1f));
        }

        private void EnsureShadow()
        {
            if (shadowRenderer != null) return;
            GameObject go = new GameObject("Shadow");
            go.transform.SetParent(transform, false);
            shadowRenderer = go.AddComponent<SpriteRenderer>();
            shadowRenderer.color = new Color(0f, 0f, 0f, 0f); // 실루엣(검정 틴트)
            if (blockRenderer != null)
            {
                shadowRenderer.sortingLayerID = blockRenderer.sortingLayerID;
                shadowRenderer.sortingOrder = blockRenderer.sortingOrder - 1; // 블록 뒤
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
            if (hpSliderFill != null)
            {
                // 실제 maxHp 기준 비율. (구 /100f는 24·30·60HP 적이 만피여도 24%·30%·60%만 차던 버그.)
                hpSliderFill.fillAmount = model.MaxHp > 0 ? (float)model.Hp / model.MaxHp : 0f;
            }
        }

        private void HideVisuals()
        {
            if (blockRenderer != null) blockRenderer.enabled = false;
            if (mobRenderer != null) mobRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;
        }

        // IDamageable: 볼 → DamageResolver → 여기. 실제 처리(HP감산/사망/디스폰)는 컨트롤러로 포워드.
        public void ApplyDamage(int amount) => _damageSink?.Invoke(this, amount);

        // IStatusReceiver: 볼 모듈(Fire) → 여기. 상태이상 부여는 컨트롤러(EnemyStatusSimulator)로 포워드.
        public void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks) =>
            _burnSink?.Invoke(this, durationSeconds, damagePerSecond, maxStacks);

        // 히트 플래시량(0=원색, 1=완전 흰색). HitFeedbackController가 매 틱 감쇠시키며 호출. 블록+몹 함께(음영 제외).
        public void SetHitFlash(float amount)
        {
            _flashMpb ??= new MaterialPropertyBlock();
            _flashMpb.SetColor(FlashColorId, FlashWhite);
            _flashMpb.SetFloat(FlashAmountId, Mathf.Clamp01(amount));
            if (blockRenderer != null) blockRenderer.SetPropertyBlock(_flashMpb);
            if (mobRenderer != null) mobRenderer.SetPropertyBlock(_flashMpb);
        }

        public override void OnInactive()
        {
            base.OnInactive(); // BaseView가 모델 언바인드 + null
            _damageSink = null;
            _burnSink = null;
            SetHitFlash(0f); // 풀 재사용 대비 플래시 원복(고스트 방지 최종 보증)
            HideVisuals();
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            SetSquash(0f); // 몹 스쿼시 복구
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
