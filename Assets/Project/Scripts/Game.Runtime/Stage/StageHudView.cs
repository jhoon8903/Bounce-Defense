using UnityEngine;
using VContainer;

namespace Game.Runtime.Stage
{
    // 디버그 등급 HUD(OnGUI). 웨이브/베이스HP/킬/상태를 화면에 표시해 루프를 눈으로 확인.
    // 폴리시 UI(초록 HP 바·결과 팝업 아트)는 Phase 5. 지금은 상태 가시화 용도.
    public sealed class StageHudView : MonoBehaviour
    {
        private StageController _stage;

        [Inject]
        public void Construct(StageController stage) => _stage = stage;

        private void OnGUI()
        {
            if (_stage == null) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(12, 10, 700, 30),
                $"Wave {_stage.WaveNumber}/{_stage.WaveCount}    Base {_stage.Base.Hp}/{_stage.Base.MaxHp}    Kills {_stage.TotalKills}    Alive {_stage.AliveThisWave}",
                style);

            if (_stage.State == StageState.Won)
            {
                style.fontSize = 40; style.normal.textColor = Color.green;
                GUI.Label(new Rect(12, 46, 700, 60), $"STAGE CLEAR!   Remaining HP {(_stage.Base.RemainingPercent * 100f):F0}%", style);
            }
            else if (_stage.State == StageState.Lost)
            {
                style.fontSize = 40; style.normal.textColor = Color.red;
                GUI.Label(new Rect(12, 46, 700, 60), "DEFEAT", style);
            }
        }
    }
}
