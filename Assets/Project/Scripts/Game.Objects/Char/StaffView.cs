using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    public class StaffView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem castAura;

        public void PlayCastAura()
        {
            if (castAura == null) return;
            castAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            castAura.Play(true);
        }
    }
}
