using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    // 결과창 볼별 데미지 한 행(Daniel이 행 템플릿에 부착 + 자식 참조 배선). DTResultView가 복제·Bind.
    public sealed class DTResultRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text damageText;

        public void Bind(Sprite sprite, long damage)
        {
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
            if (damageText != null) damageText.SetText(NumberFormat.Abbreviate(damage)); // 1000↑ K, 1,000,000↑ M 약식
        }
    }
}
