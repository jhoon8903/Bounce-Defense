using Project.Scripts.Game.Objects.Char;
using UnityEngine;

public class CharView : MonoBehaviour
{
    private AnchorView _anchorView;
    private HeadView _headView;
    private StaffView _staffView;

    private void InitBody()
    {
        _anchorView = GetComponentInChildren<AnchorView>();
        _headView = GetComponentInChildren<HeadView>();
        _staffView = GetComponentInChildren<StaffView>();
    }
}
