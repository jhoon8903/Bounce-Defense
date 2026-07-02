using UnityEngine;

namespace Spike
{
    public enum HitZone { Front, Back, Side }

    public static class HitZoneClassifier
    {
        const float FrontHalfAngle = 60f;
        const float BackHalfAngle = 60f;

        // hitNormal points outward from the enemy surface at the contact point, so it
        // roughly equals enemyForward for a leading-edge (front) hit and opposes it for a back hit.
        public static HitZone Classify(Vector2 enemyForward, Vector2 hitNormal)
        {
            float angle = Vector2.Angle(enemyForward, hitNormal);
            if (angle <= FrontHalfAngle) return HitZone.Front;
            if (angle >= 180f - BackHalfAngle) return HitZone.Back;
            return HitZone.Side;
        }
    }
}
