using Project.Scripts.Game.Objects.Char;
using UnityEngine;

public class CharView : MonoBehaviour
{
    [SerializeField] private AnchorView anchorView;
    [SerializeField] private StaffView staffView;
    [Tooltip("기본(왼쪽/2사분면) facing에서의 |localScale.x|.")]
    [SerializeField] private float baseScaleX = 1f;

    public AnchorView Anchor
    {
        get
        {
            if (!anchorView) anchorView = GetComponentInChildren<AnchorView>();
            return anchorView;
        }
    }

    public StaffView Staff
    {
        get
        {
            if (!staffView) staffView = GetComponentInChildren<StaffView>();
            return staffView;
        }
    }
    
    public void SetFacing(bool faceRight)
    {
        float mag = Mathf.Abs(baseScaleX) < 1e-4f ? 1f : Mathf.Abs(baseScaleX);
        Vector3 s = transform.localScale;
        s.x = mag * (faceRight ? -1f : 1f);
        transform.localScale = s;
    }
}
