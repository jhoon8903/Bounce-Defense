using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    public class StaffView : MonoBehaviour
    {
        private static readonly Vector2 InitPosition = new(-0.13f, 0.22f);

        private void Initialize()
        {
            transform.localPosition =  InitPosition;
            transform.localRotation = Quaternion.Euler(0,0,20);
        }
    }
}