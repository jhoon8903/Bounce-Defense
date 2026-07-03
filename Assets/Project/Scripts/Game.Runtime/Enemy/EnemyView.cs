using Game.Combat;
using Game.Core.Mvc;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 뷰: 돌 타일(block) + 위 몹 스프라이트 + 콜라이더(볼 피격 대상) + HP 숫자.
    // IDamageable로서 볼 데미지를 받되, 실제 처리(HP감산/사망/디스폰)는 컨트롤러로 포워드(뷰는 dumb 렌더러+브리지).
    [DisallowMultipleComponent]
    public sealed class EnemyView : BaseView<EnemyModel>, IDamageable
    {
        [SerializeField] private SpriteRenderer blockRenderer; // 돌 타일(콜라이더 몸체와 정렬)
        [SerializeField] private SpriteRenderer mobRenderer;   // 위에 올라가는 몬스터(순수 비주얼)
        [SerializeField] private BoxCollider2D boxCollider;    // 볼 CircleCast 대상(Enemy 레이어)
        [SerializeField] private TextMesh hpText;              // 몸체 HP 숫자(선택 — 미배선 시 스킵)

        private System.Action<EnemyView, int, HitContext> _damageSink;

        public void SetDamageSink(System.Action<EnemyView, int, HitContext> sink) => _damageSink = sink;

        // 그리드 배치가 정한 풋프린트 월드 크기로 콜라이더(핀볼 반사 몸체) 크기 세팅. 모델 밖 정보라 컨트롤러가 주입.
        // 블록 스프라이트는 풋프린트별 아트(Block_1x1~2x2)를 쓰므로 스케일 안 함. Sliced/Tiled면 size도 맞춘다.
        public void SetFootprintSize(Vector2 worldSize)
        {
            if (boxCollider != null) boxCollider.size = worldSize;
            if (blockRenderer != null && blockRenderer.drawMode != SpriteDrawMode.Simple)
                blockRenderer.size = worldSize;
        }

        protected override void OnModelBound(EnemyModel model)
        {
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
            if (hpText != null) hpText.text = model.Hp.ToString();
        }

        private void HideVisuals()
        {
            if (blockRenderer != null) blockRenderer.enabled = false;
            if (mobRenderer != null) mobRenderer.enabled = false;
            if (boxCollider != null) boxCollider.enabled = false;
        }

        // IDamageable: 볼 → DamageResolver.ApplyDamageStage → 여기. 실제 처리는 컨트롤러로 포워드.
        public void ApplyDamage(int amount, HitContext context) => _damageSink?.Invoke(this, amount, context);

        public override void OnInactive()
        {
            base.OnInactive(); // BaseView가 모델 언바인드 + null
            _damageSink = null;
            HideVisuals();
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
