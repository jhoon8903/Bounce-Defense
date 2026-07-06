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
        [SerializeField] private ParticleSystem burnFx; // 번 불꽃 루프(적-부착, 몹 위). SetBurning으로 on/off. 미배선 시 스킵.
        [SerializeField] private ParticleSystem freezeFx; // 냉동 서리/눈 루프(적-부착, 몹 위). SetFrozen으로 on/off. 미배선 시 스킵.

        private System.Action<EnemyView, int> _damageSink;
        private System.Action<EnemyView, float, float, int> _burnSink; // (view, duration, dps, maxStacks)
        private System.Action<EnemyView, float, float> _freezeSink;    // (view, duration, slow)

        // 몸체 오버레이(SpriteHitFlash 셰이더): 히트 화이트 플래시(순간) + 냉동 파랑 틴트(지속).
        // 둘 다 하나의 MaterialPropertyBlock에 공존 — 따로 쓰면 SetPropertyBlock이 서로를 덮어써서 하나가 사라짐(제로할당·공유머티리얼 무변경).
        private MaterialPropertyBlock _overlayMpb;
        private float _flashAmount;
        private float _frostAmount;
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
        private static readonly int FrostAmountId = Shader.PropertyToID("_FrostAmount");
        private static readonly int FrostColorId = Shader.PropertyToID("_FrostColor");
        private static readonly Color FlashWhite = Color.white;
        private static readonly Color FrostCyan = new Color(0.55f, 0.85f, 1f, 1f);
        private const float FrozenTint = 0.45f; // 얼어있는 동안 파랑 정도(0~1)
        private Vector3 _mobBaseScale = Vector3.one; // 스쿼시 기준(프리팹 몹 스케일이 1이 아닐 수 있어 캡처)
        private Vector3 _mobBasePos;                 // 움찔 반동 기준(몹 로컬 위치 — 킥 후 여기로 복귀)
        private bool _mobBaseCaptured;
        private CanvasGroup _hpCanvasGroup;
        private RectTransform _hpBarRect;            // HP 바(월드 캔버스) RectTransform — 풋프린트 높이별 위치 보정
        private Vector2 _hpBarBaseAnchored;          // 프리팹 기준 위치(1칸 높이 기준). 2칸 이상이면 그만큼 아래로.
        private bool _hpBarBaseCaptured;

        public void SetDamageSink(System.Action<EnemyView, int> sink) => _damageSink = sink;
        public void SetBurnSink(System.Action<EnemyView, float, float, int> sink) => _burnSink = sink;
        public void SetFreezeSink(System.Action<EnemyView, float, float> sink) => _freezeSink = sink;
        
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

        // 피격 움찔 반동(§4): 위 몹만 순간 밀었다 원복(HitFeedbackController가 매 틱 감쇠 오프셋으로 호출).
        // 몹 로컬 위치만 오프셋 → 콜라이더·돌 블록·모델(격자 하강 위치)은 불변이라 경로 이탈/경직 없음.
        public void SetRecoil(Vector2 localOffset)
        {
            if (mobRenderer == null || !_mobBaseCaptured) return;
            mobRenderer.transform.localPosition = _mobBasePos + (Vector3)localOffset;
        }

        // 침범 연출 중 볼 적중 차단(스펙 #3, Daniel): 콜라이더 off → 볼이 관통(반사·데미지 없음).
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

        // IStatusReceiver: 볼 모듈(Fire/Ice) → 여기. 상태이상 부여는 컨트롤러(EnemyStatusSimulator)로 포워드.
        public void ApplyBurn(float durationSeconds, float damagePerSecond, int maxStacks) => _burnSink?.Invoke(this, durationSeconds, damagePerSecond, maxStacks);

        public void ApplyFreeze(float durationSeconds, float slow) => _freezeSink?.Invoke(this, durationSeconds, slow);

        // 번 불꽃 VFX(적-부착 루프). 컨트롤러(EnemyStatusSimulator 경유)가 첫 스택=on, 만료/디스폰=off. 미배선 시 무시.
        public void SetBurning(bool on)
        {
            if (burnFx == null) return;
            if (on)
            {
                if (!burnFx.gameObject.activeSelf) burnFx.gameObject.SetActive(true); // 먼저 활성화해야 Play 유효
                if (!burnFx.isPlaying) burnFx.Play(true);
            }
            else
            {
                burnFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (burnFx.gameObject.activeSelf) burnFx.gameObject.SetActive(false);
            }
        }

        // 냉동 서리 VFX(적-부착 루프) + 몸체 파랑 틴트. 컨트롤러(EnemyStatusSimulator 경유)가 첫 냉동=on, 만료/디스폰=off.
        // 틴트는 파티클 배선 여부와 무관하게 항상 적용(서리 파티클 미배선이어도 몸은 파랗게).
        public void SetFrozen(bool on)
        {
            _frostAmount = on ? FrozenTint : 0f;
            ApplyOverlay();
            if (freezeFx == null) return;
            if (on)
            {
                if (!freezeFx.gameObject.activeSelf) freezeFx.gameObject.SetActive(true); // 먼저 활성화해야 Play 유효
                if (!freezeFx.isPlaying) freezeFx.Play(true);
            }
            else
            {
                freezeFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (freezeFx.gameObject.activeSelf) freezeFx.gameObject.SetActive(false);
            }
        }

        // 히트 플래시량(0=원색, 1=완전 흰색). HitFeedbackController가 매 틱 감쇠시키며 호출. 블록+몹 함께(음영 제외).
        public void SetHitFlash(float amount)
        {
            _flashAmount = Mathf.Clamp01(amount);
            ApplyOverlay();
        }

        // 플래시(순간)+틴트(지속)를 한 블록에 담아 블록+몹에 적용. 둘 중 하나만 갱신해도 나머지 값 유지(상호 덮어쓰기 방지).
        private void ApplyOverlay()
        {
            _overlayMpb ??= new MaterialPropertyBlock();
            _overlayMpb.SetColor(FlashColorId, FlashWhite);
            _overlayMpb.SetFloat(FlashAmountId, _flashAmount);
            _overlayMpb.SetColor(FrostColorId, FrostCyan);
            _overlayMpb.SetFloat(FrostAmountId, _frostAmount);
            if (blockRenderer != null) blockRenderer.SetPropertyBlock(_overlayMpb);
            if (mobRenderer != null) mobRenderer.SetPropertyBlock(_overlayMpb);
        }

        public override void OnInactive()
        {
            base.OnInactive(); // BaseView가 모델 언바인드 + null
            _damageSink = null;
            _burnSink = null;
            _freezeSink = null;
            SetHitFlash(0f); // 풀 재사용 대비 플래시 원복(고스트 방지 최종 보증)
            SetBurning(false); // 풀 재사용 대비 불꽃 끔
            SetFrozen(false); // 풀 재사용 대비 서리 끔
            HideVisuals();
            if (shadowRenderer != null) shadowRenderer.enabled = false;
            SetSquash(0f); // 몹 스쿼시 복구
            SetRecoil(Vector2.zero); // 몹 움찔 오프셋 원복(풀 재사용 대비)
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
