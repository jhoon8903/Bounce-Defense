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

        private string _id;
        private EnemyDefinition _definition;
        private int _hp;
        private int _maxHp;
        private Vector2 _position;
        private bool _isDead;
        private SpawnPhase _phase = SpawnPhase.Active;
        private float _landedY; // 착지 셀 중심 Y = 하강 시작점(입장 중 이웃의 하강 floor 기준)

        public string Id => _id;
        public EnemyDefinition Definition => _definition;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public Vector2 Position => _position;
        public bool IsDead => _isDead;
        public float DescentSpeed => _definition != null ? _definition.DescentSpeed : 0f;
        public Footprint Footprint => _definition != null ? _definition.Footprint : Footprint.Size1x1;
        public bool IsEntering => _phase == SpawnPhase.Entering;
        public float LandedY => _landedY;

        public void Initialize(string id, EnemyDefinition definition, Vector2 position)
        {
            _id = id;
            _definition = definition;
            _maxHp = definition != null ? definition.BaseHp : 1;
            _hp = _maxHp;
            _position = position;
            _isDead = false;
            _phase = SpawnPhase.Active; // 풀 재사용 대비 리셋. Spawn이 곧 BeginEntering으로 덮는다.
            Raise();
        }

        // 등장 시작(스폰 직후 choreographer가 호출). landedY = 착지 셀 중심 Y = 하강 시작점.
        public void BeginEntering(float landedY)
        {
            _phase = SpawnPhase.Entering;
            _landedY = landedY;
        }

        public void MarkActive() => _phase = SpawnPhase.Active;

        // 데미지 적용(HP 상태만). 사망 판정은 여기서, 킬 이벤트·디스폰은 컨트롤러가 IsDead를 보고 처리.
        // 등장 중(Entering)엔 무적 — 낙하하는 몹은 아직 전장에 없다.
        public void TakeDamage(int amount)
        {
            if (_isDead || amount <= 0 || _phase == SpawnPhase.Entering) return;
            _hp = Mathf.Max(0, _hp - amount);
            if (_hp == 0) _isDead = true;
            Raise(); // HP 숫자 갱신
        }

        public void SetPosition(Vector2 position)
        {
            if (_position == position) return;
            _position = position;
            Raise();
        }
    }
}
