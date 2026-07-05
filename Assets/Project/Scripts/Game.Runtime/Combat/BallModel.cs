using Game.Core.Observer;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // 볼 상태 Observable. 뷰에 보이는 변경(위치)에서만 Raise.
    // 이동/반사는 IBallMotor, 데미지 수치는 BallConfig, 종료 판정은 BallController가 담당 — 모델은 위치+바운스 집계만.
    // (식별자는 BallController가 딕셔너리 키로만 쓴다 — 모델은 자기 id를 알 필요 없음.)
    public sealed class BallModel : Observable
    {
        private Vector2 _position;
        private int _bounceCount;

        public Vector2 Position => _position;
        public int BounceCount => _bounceCount;

        public void Initialize(Vector2 origin)
        {
            _position = origin;
            _bounceCount = 0;
            Raise();
        }

        public void SetPosition(Vector2 position)
        {
            if (_position == position) return;
            _position = position;
            Raise();
        }

        // 바운스 카운트는 렌더 상태가 아니므로 Raise 생략 (컨트롤러 종료 판정용)
        public void RegisterBounce(int count)
        {
            if (count <= 0) return;
            _bounceCount += count;
        }
    }
}
