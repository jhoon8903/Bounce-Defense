using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spike
{
    public sealed class LaunchPad : MonoBehaviour
    {
        [SerializeField] SpikeBall ballPrefab;
        [SerializeField] TrajectoryPreview preview;
        [SerializeField] Vector2 origin = new Vector2(0f, -8f);
        [SerializeField] float launchAngleDeg = 60f;
        [SerializeField] bool autoTestOnPlay = true;

        // Headless verification pass: MCP can't simulate keypresses, so this fires a fixed
        // shot sequence at Play-mode start covering thin-wall, block, moving-enemy and Ghost cases.
        static readonly (float angle, bool ghost)[] AutoTestShots =
        {
            (78f, false),
            (98f, false),
            (100f, false),
            (98f, true),
            (70f, true),
        };

        void Start()
        {
            if (autoTestOnPlay) StartCoroutine(AutoTestSequence());
        }

        IEnumerator AutoTestSequence()
        {
            SpikeLog.Clear();
            yield return new WaitForSeconds(0.5f);
            int index = 0;
            foreach (var shot in AutoTestShots)
            {
                Launch(AngleToDir(shot.angle), shot.ghost, index);
                index++;
                yield return new WaitForSeconds(1.5f);
            }
            SpikeLog.Add("AUTO_TEST_SEQUENCE_DONE");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.leftArrowKey.isPressed) launchAngleDeg += 60f * Time.deltaTime;
            if (kb.rightArrowKey.isPressed) launchAngleDeg -= 60f * Time.deltaTime;
            launchAngleDeg = Mathf.Clamp(launchAngleDeg, 15f, 165f);

            Vector2 dir = AngleToDir(launchAngleDeg);
            if (preview != null) preview.Draw(origin, dir);

            if (kb.spaceKey.wasPressedThisFrame) Launch(dir, false, -1);
            if (kb.gKey.wasPressedThisFrame) Launch(dir, true, -1);
        }

        void Launch(Vector2 dir, bool ghost, int index)
        {
            var ball = Instantiate(ballPrefab, origin, Quaternion.identity);
            string label = ghost ? "Ghost" : "Normal";
            ball.name = index >= 0 ? $"SpikeBall_{label}_{index}" : $"SpikeBall_{label}";
            ball.SetGhost(ghost);
            ball.Launch(origin, dir);
        }

        static Vector2 AngleToDir(float deg)
        {
            float rad = deg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
