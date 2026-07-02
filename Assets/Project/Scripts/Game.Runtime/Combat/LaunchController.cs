using System.Collections;
using Game.Combat;
using Game.Core.Pool;
using Game.Events;
using UnityEngine;
using VContainer;

namespace Game.Runtime.Combat
{
    public sealed class LaunchController : MonoBehaviour
    {
        [SerializeField] private int ballCount = 8;
        [SerializeField] private float staggerSeconds = 0.08f;
        [SerializeField] private float ballSpeed = 12f;
        [SerializeField] private int normalBallDamage = 8;

        [Header("MCP 자동 검증용 (드래그 입력 시뮬레이션 불가 우회 — Phase0 SpikeLog 교훈)")]
        [SerializeField] private bool autoLaunchOnPlay;
        [SerializeField] private float autoLaunchDelay = 0.5f;
        [SerializeField] private float autoLaunchAngleDeg = 90f;
        [SerializeField] private Transform autoLaunchOrigin;

        private IPool _pool;
        private DamageResolver _resolver;
        private CombatEventHub _hub;

        [Inject]
        public void Construct(IPool pool, DamageResolver resolver, CombatEventHub hub)
        {
            _pool = pool;
            _resolver = resolver;
            _hub = hub;
        }

        private void Start()
        {
            if (autoLaunchOnPlay) StartCoroutine(AutoLaunchAfterDelay());
        }

        private IEnumerator AutoLaunchAfterDelay()
        {
            yield return new WaitForSeconds(autoLaunchDelay);
            float rad = autoLaunchAngleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 origin = autoLaunchOrigin ? autoLaunchOrigin.position : Vector2.zero;
            Launch(origin, dir);
        }

        public void Launch(Vector2 origin, Vector2 direction) => StartCoroutine(StaggeredLaunch(origin, direction));

        private IEnumerator StaggeredLaunch(Vector2 origin, Vector2 direction)
        {
            for (int i = 0; i < ballCount; i++)
            {
                Ball ball = _pool.Get<Ball>();
                if (ball != null)
                {
                    ball.Configure(_resolver, _hub, _pool);
                    ball.Launch(origin, direction, ballSpeed, normalBallDamage, BallSourceType.Normal);
                }
                yield return new WaitForSeconds(staggerSeconds);
            }
        }
    }
}
