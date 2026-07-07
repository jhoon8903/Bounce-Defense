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
        [SerializeField] private SpriteRenderer hpBg;   // HP바 배경(SpriteRenderer = 스프라이트 배치, 적별 월드캔버스 제거)
        [SerializeField] private SpriteRenderer hpFill; // HP바 채움 — 좌측 고정 스케일로 HP% 표현(Image.fillAmount 대체)
        [SerializeField] private SpriteRenderer shadowRenderer; // 착지 텔레그래프 음영(미배선 시 런타임 자동생성)
        [SerializeField] private ParticleSystem burnFx; // 번 불꽃 루프(적-부착, 몹 위). SetBurning으로 on/off. 미배선 시 스킵.
        [SerializeField] private ParticleSystem freezeFx; // 냉동 서리/눈 루프(적-부착, 몹 위). SetFrozen으로 on/off. 미배선 시 스킵.

        private System.Action<EnemyView, int> _damageSink;
        private System.Action<EnemyView, float, float, int> _burnSink; // (view, duration, dps, maxStacks)
        private System.Action<EnemyView, float, float> _freezeSink;    // (view, duration, slow)

        // 몸체 오버레이(SpriteHitFlash URP 셰이더): 히트 화이트 플래시(순간) + 냉동 파랑 틴트(지속).
        // 렌더러별 인스턴스 머티리얼에 직접 SetFloat/SetColor. MPB는 렌더러를 SRP 배처 부적격으로 만들어 금지 —
        // 인스턴스 머티리얼(같은 셰이더, 다른 값)은 SRP 배칭됨. 풀 오브젝트라 인스턴스는 최초 1회 생성 후 영구 재사용(핫패스 할당 0).
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
        private const float FrozenTint = 0.45f; // 얼어있는 동안 파랑 정도(0~1)
        private Vector3 _mobBaseScale = Vector3.one; // 스쿼시 기준(프리팹 몹 스케일이 1이 아닐 수 있어 캡처)
        private Vector3 _mobBasePos;                 // 움찔 반동 기준(몹 로컬 위치 — 킥 후 여기로 복귀)
        private bool _mobBaseCaptured;
        private Vector3 _hpBarBaseLocalPos;  // HP바 앵커 프리팹 기준 로컬 위치(풋프린트 높이 보정 기준)
        private Vector3 _hpFillBaseScale;    // 채움 프리팹 기준 스케일
        private Vector3 _hpFillBaseLocalPos; // 채움 프리팹 기준 로컬 위치(좌측 앵커 기준)
        private float _hpFillWidth;          // 채움 로컬 폭(스케일 채움 시 좌측 고정 시프트 계산)
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

        // 프리팹 기준 HP 바 위치를 최초 1회 캡처(풀 재사용 대비 원본 보존).
        private void CaptureHpBarBase()
        {
            if (_hpBarBaseCaptured || HpBarRoot == null) return;
            _hpBarBaseLocalPos = HpBarRoot.localPosition;
            if (hpFill != null)
            {
                _hpFillBaseScale = hpFill.transform.localScale;
                _hpFillBaseLocalPos = hpFill.transform.localPosition;
                _hpFillWidth = hpFill.sprite != null ? hpFill.sprite.bounds.size.x * _hpFillBaseScale.x : 0f; // 로컬 만피 폭
            }
            SetBarVisible(false); // 초기 숨김(등장 전)
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
            SetBarVisible(on);
        }

        private void SetBarVisible(bool on)
        {
            if (hpBg != null) hpBg.enabled = on;
            if (hpFill != null) hpFill.enabled = on;
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
            if (hpFill == null) return;
            CaptureHpBarBase(); // 베이스 스케일/폭을 RefreshHp가 스케일 수정 전에 확보(캡처 순서 보장, idempotent)
            float pct = model.MaxHp > 0 ? (float)model.Hp / model.MaxHp : 0f;
            // 좌측 고정 스케일 채움(fillAmount 대체): x만 pct배 + 왼쪽 가장자리 유지하도록 시프트.
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

        // 최초 1회 렌더러별 인스턴스 머티리얼 생성·캐시(풀 재사용 → 이후 할당 0). 상수 색(플래시 흰·서리 청록)도 여기서 1회.
        private void EnsureOverlayMaterials()
        {
            if (_blockMat == null && blockRenderer != null)
            {
                _blockMat = blockRenderer.material; // 인스턴스 사본 생성·바인딩(SpriteHitFlash). 이후 이 렌더러 전용.
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

        // 플래시(순간)+틴트(지속) 양을 블록+몹 인스턴스 머티리얼에 적용. 둘은 독립 값이라 상호 덮어쓰기 없음.
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
