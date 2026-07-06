using Game.Core.Clock;
using Game.Core.Mvc;
using Project.Scripts.Game.Objects.Char;
using UnityEngine;

namespace Game.Runtime.Combat
{
    // Char(사신) 비주얼 구동 오케스트레이터. HitFeedbackController 미러링:
    //  - 순수 DI 생성자, OnInitialize에서 IClock.OnTick 구독(프레임 비주얼 → OnTick + GameDeltaTime).
    //  - 시간 로직(조준 스무딩·발사 반동 감쇠)을 여기서 GameDeltaTime으로 돌린다 → 일시정지(GameSpeed=0) 시 정지.
    //    적 움찔·데미지 숫자·플래시와 같은 게임 시계 아래로 통일(뷰의 자체 Update/Time.deltaTime 제거).
    //  - CharView/AnchorView는 dumb 렌더러: SetFacing/SetLocalAngle setter만 노출, 스스로 시간을 돌리지 않는다.
    // 데이터 소스 = LaunchController(CurrentDirection 조준 + ShotsFired 발사 카운터). 단방향: AimController→LaunchController→여기.
    public sealed class CharController : BaseController
    {
        private readonly IClock _clock;
        private readonly LaunchController _launch;
        private readonly CharView _view;

        private AnchorView _anchor;
        private float _targetZ; // 목표 로컬 각(조준 방향 매핑)
        private float _aimZ;    // 현재 로컬 각(스무딩, MoveTowardsAngle로 추종)
        private float _recoil;  // 발사 반동 오프셋(도) — 0으로 감쇠, 조준각 위에 합성
        private int _lastShots; // 마지막으로 본 발사 카운터(증가분 = 이번 틱 새 발사 → 반동 킥)
        private bool _flipped;  // 뒤집힘 부호(반동 방향 보정)

        public CharController(IClock clock, LaunchController launch, CharView view)
        {
            _clock = clock;
            _launch = launch;
            _view = view;
        }

        protected override void OnInitialize()
        {
            _anchor = _view != null ? _view.Anchor : null; // Awake 이후(빌드 콜백) 조회 — 뷰 준비 완료
            if (_launch != null) _lastShots = _launch.ShotsFired; // 초기 스퓨리어스 킥 방지
            _clock.OnTick += HandleTick;
        }

        protected override void OnDispose() => _clock.OnTick -= HandleTick;

        protected override void OnReset() { }
        protected override void OnTick(float _) { }       // 실틱은 HandleTick(구독)
        protected override void OnFixedTick(float _) { }

        // ---- IClock.OnTick: facing + 조준 스무딩 + 발사 반동(모두 GameDeltaTime → 일시정지 시 정지) ----
        private void HandleTick()
        {
            if (_view == null || _launch == null) return;
            float dt = _clock.GameDeltaTime;

            Vector2 dir = _launch.CurrentDirection;
            bool faceRight = dir.x > 0f;
            _flipped = faceRight;
            _view.SetFacing(faceRight); // 즉시값(비시간) — 일시정지 중 방향 불변이면 그대로

            if (_anchor == null) return;

            // 목표 로컬 각 = 조준 방향(클램프) → 스태프 전방(로컬 +Y) 기준 매핑. 미러 시 90-θ, 정상 시 θ-90.
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

            // 발사 카운터 증가분 → 반동 킥(총 쏘듯 까딱). 일시정지(dt=0) 중엔 발사도 멈추므로 킥도 없음.
            int shots = _launch.ShotsFired;
            if (shots != _lastShots)
            {
                _lastShots = shots;
                if (dt > 0f)
                {
                    _recoil = _flipped ? -_anchor.RecoilKickDeg : _anchor.RecoilKickDeg;
                    _view.Staff?.PlayCastAura(); // 발사 룬 마법진(생겼다 사라짐 — 매 발 재트리거)
                }
            }

            _anchor.SetLocalAngle(_aimZ + _recoil);
        }
    }
}
