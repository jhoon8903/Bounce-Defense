using UnityEngine;

namespace Project.Scripts.Game.Objects.Char
{
    public class HeadView : MonoBehaviour
    {
        private void Initialize()
        {
            transform.localPosition =  Vector2.zero;
            transform.localRotation = Quaternion.Euler(0,0,20);
        }
    }
}