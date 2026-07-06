using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    // 스태프 뷰(dumb 렌더러). 발사 순간 CharController가 PlayCastAura()로 룬 마법진을 리셋·재생 → "생겼다 사라짐"(원샷).
    public class StaffView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem castAura; // 발사 룬 마법진(원샷, loop off). 미배선 시 무시.

        // 발사 순간 호출 — 재생 중이어도 항상 처음(time=0)부터 재시작 → 연발 재트리거해도 크기·개수 일정.
        // (Clear+Play는 재생시간을 리셋 안 해 위상이 제멋대로 → Stop(StopEmittingAndClear)로 완전 리셋 후 Play.)
        public void PlayCastAura()
        {
            if (castAura == null) return;
            castAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            castAura.Play(true);
        }
    }
}
