using Game.Core.Observer;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 상태 Observable. BallModel 규율: 뷰에 보이는 변경(HP·위치)에서만 Raise.
    // HP 소유·감산은 여기(모델), 킬/디스폰/그리드 lifecycle은 EnemyController가 담당.
    // 등장 연출의 '진행'(타이밍·낙하 곡선)은 EnemyEntranceChoreographer 소유 — 모델은 게임플레이에
    // 의미 있는 상태만 든다: 입장 중인가(무적·하강 제외)와 착지 목표 Y(하강 이웃 계산 기준).
    public sealed class EnemyModel : Observable
    {
        // 등장 단계. Entering = 낙하/덜컹 중(무적, 하강 제외). Active = 전투/하강 개시.
        public enum SpawnPhase { Entering, Active }

        private SpawnPhase _phase = SpawnPhase.Active;
        private float _freezeSlow; // 냉동 하강 감속률(0=없음, 0.20=20% 감속). EnemyStatusSimulator가 세팅.
        public string Id { get; private set; }
        public EnemyDefinition Definition { get; private set; }
        public int Hp { get; private set; }
        public int MaxHp { get; private set; }
        public Vector2 Position { get; private set; }
        public bool IsDead { get; private set; }

        // 하강속도 = 정의값 × (1 - 냉동슬로우). 냉동 중이면 그만큼 느리게 내려온다(Ice §199).
        public float DescentSpeed => (Definition != null ? Definition.DescentSpeed : 0f) * (1f - _freezeSlow);
        public Footprint Footprint => Definition != null ? Definition.Footprint : Footprint.Size1x1;
        public bool IsEntering => _phase == SpawnPhase.Entering;
        public float LandedY { get; private set; }

        public void Initialize(string id, EnemyDefinition definition, Vector2 position)
        {
            Id = id;
            Definition = definition;
            MaxHp = definition != null ? definition.BaseHp : 1;
            Hp = MaxHp;
            Position = position;
            IsDead = false;
            _freezeSlow = 0f; // 풀 재사용 대비 냉동 리셋
            _phase = SpawnPhase.Active; // 풀 재사용 대비 리셋. Spawn이 곧 BeginEntering으로 덮는다.
            Raise();
        }

        // 등장 시작(스폰 직후 choreographer가 호출). landedY = 착지 셀 중심 Y = 하강 시작점.
        public void BeginEntering(float landedY)
        {
            _phase = SpawnPhase.Entering;
            LandedY = landedY;
        }

        public void MarkActive() => _phase = SpawnPhase.Active;

        // 데미지 적용(HP 상태만). 사망 판정은 여기서, 킬 이벤트·디스폰은 컨트롤러가 IsDead를 보고 처리.
        // 등장 중(Entering)엔 무적 — 낙하하는 몹은 아직 전장에 없다.
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0 || _phase == SpawnPhase.Entering) return;
            Hp = Mathf.Max(0, Hp - amount);
            if (Hp == 0) IsDead = true;
            Raise(); // HP 숫자 갱신
        }

        public void SetPosition(Vector2 position)
        {
            if (Position == position) return;
            Position = position;
            Raise();
        }

        // 냉동 하강 감속률 세팅(EnemyStatusSimulator가 매 틱 호출). 하강속도에만 반영 — 뷰 변경 없어 Raise 불필요.
        public void SetFreezeSlow(float slow)
        {
            _freezeSlow = Mathf.Clamp01(slow);
        }
    }
}
