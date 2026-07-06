using Project.Scripts.Game.Objects.Char;
using UnityEngine;

// Char 루트 뷰 = dumb 렌더러. 스스로 시간을 돌리지 않는다(자체 LateUpdate 제거).
// CharController(IClock 틱)가 조준 방향을 읽어 SetFacing으로 좌우 뒤집기를 밀어넣고, 스태프 각은 Anchor로 구동한다.
public class CharView : MonoBehaviour
{
    [SerializeField] private AnchorView anchorView;
    [Tooltip("기본(왼쪽/2사분면) facing에서의 |localScale.x|.")]
    [SerializeField] private float baseScaleX = 1f;

    // 스태프 피벗(컨트롤러가 각도 구동). 미배선 시 자식에서 탐색(빌드 콜백 조회 타이밍 안전).
    public AnchorView Anchor
    {
        get
        {
            if (!anchorView) anchorView = GetComponentInChildren<AnchorView>();
            return anchorView;
        }
    }

    // 조준 방향 부호로 좌우 뒤집기. 즉시값(비시간) — 컨트롤러가 매 틱 호출.
    public void SetFacing(bool faceRight)
    {
        float mag = Mathf.Abs(baseScaleX) < 1e-4f ? 1f : Mathf.Abs(baseScaleX);
        Vector3 s = transform.localScale;
        s.x = mag * (faceRight ? -1f : 1f);
        transform.localScale = s;
    }
}
