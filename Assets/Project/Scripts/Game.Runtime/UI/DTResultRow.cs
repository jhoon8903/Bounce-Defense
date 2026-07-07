using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    public sealed class DTResultRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text damageText;

        public void Bind(Sprite sprite, long damage)
        {
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            if (damageText != null) damageText.SetText(NumberFormat.Abbreviate(damage));
        }
    }
}
