using Game.Core.Clock;
using Game.Core.Mvc;
using Project.Scripts.Game.Objects.Char;
using UnityEngine;

namespace Game.Runtime.Combat
{
    public sealed class CharController : BaseController
    {
        private readonly IClock _clock;
        private readonly LaunchController _launch;
        private readonly CharView _view;

        private AnchorView _anchor;
        private float _targetZ;
        private float _aimZ;
        private float _recoil;
        private int _lastShots;
        private bool _flipped;

        public CharController(IClock clock, LaunchController launch, CharView view)
        {
            _clock = clock;
            _launch = launch;
            _view = view;
        }

        protected override void OnInitialize()
        {
            _anchor = _view != null ? _view.Anchor : null;
            if (_launch != null) _lastShots = _launch.ShotsFired;
            _clock.OnTick += HandleTick;
        }

        protected override void OnDispose() => _clock.OnTick -= HandleTick;

        protected override void OnReset() { }
        protected override void OnTick(float _) { }
        protected override void OnFixedTick(float _) { }

        private void HandleTick()
        {
            if (_view == null || _launch == null) return;
            float dt = _clock.GameDeltaTime;

            Vector2 dir = _launch.CurrentDirection;
            bool faceRight = dir.x > 0f;
            _flipped = faceRight;
            _view.SetFacing(faceRight);

            if (_anchor == null) return;

            if (dir.sqrMagnitude > 1e-6f)
            {
                float worldAngle = AimController.ClampAimAngle(
                    Vector2.SignedAngle(Vector2.right, dir), _anchor.MinAngleDeg, _anchor.MaxAngleDeg);
                _targetZ = faceRight ? 90f - worldAngle : worldAngle - 90f;
            }

            if (dt > 0f)
            {
                _aimZ = _anchor.TurnSpeedDeg <= 0f
                    ? _targetZ
                    : Mathf.MoveTowardsAngle(_aimZ, _targetZ, _anchor.TurnSpeedDeg * dt);
                if (!Mathf.Approximately(_recoil, 0f))
                    _recoil = Mathf.MoveTowards(_recoil, 0f, _anchor.RecoilReturnDeg * dt);
            }

            int shots = _launch.ShotsFired;
            if (shots != _lastShots)
            {
                _lastShots = shots;
                if (dt > 0f)
                {
                    _recoil = _flipped ? -_anchor.RecoilKickDeg : _anchor.RecoilKickDeg;
                    _view.Staff?.PlayCastAura();
                }
            }

            _anchor.SetLocalAngle(_aimZ + _recoil);
        }
    }
}
