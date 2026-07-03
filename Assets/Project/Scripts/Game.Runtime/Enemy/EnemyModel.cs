using System.Collections.Generic;
using Game.Core.Observer;
using Game.Runtime.Grid;
using UnityEngine;

namespace Game.Runtime.Enemy
{
    // 적 상태 Observable. BallModel 규율: 뷰에 보이는 변경(HP·위치)에서만 Raise.
    // HP 소유·감산은 여기(모델), 킬/디스폰/그리드 lifecycle은 EnemyController가 담당.
    public sealed class EnemyModel : Observable
    {
        private string _id;
        private EnemyDefinition _definition;
        private int _hp;
        private int _maxHp;
        private Vector2 _position;
        private int _gridHandle; // 현재 점유 중인 그리드 블록 핸들(하강 시 재등록으로 갱신)
        private bool _isDead;
        private readonly List<StatusInstance> _statuses = new(); // Phase 4 상태이상 컨테이너

        public string Id => _id;
        public EnemyDefinition Definition => _definition;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public Vector2 Position => _position;
        public int GridHandle => _gridHandle;
        public bool IsDead => _isDead;
        public float DescentSpeed => _definition != null ? _definition.DescentSpeed : 0f;
        public Footprint Footprint => _definition != null ? _definition.Footprint : Footprint.Size1x1;
        public IReadOnlyList<StatusInstance> Statuses => _statuses;

        public void Initialize(string id, EnemyDefinition definition, Vector2 position)
        {
            _id = id;
            _definition = definition;
            _maxHp = definition != null ? definition.BaseHp : 1;
            _hp = _maxHp;
            _position = position;
            _gridHandle = 0;
            _isDead = false;
            _statuses.Clear();
            Raise();
        }

        // 데미지 적용(HP 상태만). 사망 판정은 여기서, 킬 이벤트·디스폰은 컨트롤러가 IsDead를 보고 처리.
        public void TakeDamage(int amount)
        {
            if (_isDead || amount <= 0) return;
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

        // 그리드 재등록(하강 행경계 통과) 시 핸들 갱신. 렌더 상태 아니므로 Raise 생략.
        public void SetGridHandle(int handle) => _gridHandle = handle;

        // ---- 상태이상(Phase 4 스텁) ----
        public void AddStatus(StatusInstance status)
        {
            if (status == null) return;
            _statuses.Add(status);
        }

        public void ClearStatuses() => _statuses.Clear();
    }
}
