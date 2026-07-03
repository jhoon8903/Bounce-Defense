using Game.Runtime.Combat;
using Project.Scripts.Game.Objects.Char;
using UnityEngine;

// 캐릭터(사신) 비주얼 오케스트레이터. LaunchController.CurrentDirection(단일 조준 방향)을 읽어:
//  - 조준이 1사분면(오른쪽, dir.x>0)이면 Char를 localScale.x=-1로 플립, 아니면 +1(기본).
//  - 앵커(스태프)를 같은 조준 방향으로 구동(AnchorView.AimTo, 플립 반영).
public class CharView : MonoBehaviour
{
    [SerializeField] private LaunchController launchController;
    [SerializeField] private AnchorView anchorView;
    [Tooltip("기본(왼쪽/2사분면) facing에서의 |localScale.x|.")]
    [SerializeField] private float baseScaleX = 1f;

    private void Awake()
    {
        if (!launchController) launchController = FindObjectOfType<LaunchController>();
        if (!anchorView) anchorView = GetComponentInChildren<AnchorView>();
    }

    private void LateUpdate()
    {
        if (launchController == null) return;
        Vector2 dir = launchController.CurrentDirection;
        bool faceRight = dir.x > 0f; // 1사분면(오른쪽)

        float mag = Mathf.Abs(baseScaleX) < 1e-4f ? 1f : Mathf.Abs(baseScaleX);
        Vector3 s = transform.localScale;
        s.x = mag * (faceRight ? -1f : 1f);
        transform.localScale = s;

        if (anchorView != null) anchorView.AimTo(dir, faceRight);
    }
}
