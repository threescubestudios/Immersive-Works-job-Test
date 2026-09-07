using UnityEngine;
using UnityEngine.UI;

namespace SaveCurupira.UI
{
    public class CurupiraUI : MonoBehaviour
    {
        public Text comboText;
        public Text timerText;
        public Slider rootHealthSlider; // Generic progress bar for total root health

        private void Update()
        {
            if (Core.CurupiraGameController.Instance != null && Core.CurupiraGameController.Instance.CurrentState == Core.CurupiraGameController.GameState.Playing)
            {
                float time = Core.CurupiraGameController.Instance.GetTimeRemaining();
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time - minutes * 60);
                if(timerText != null) timerText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
            }
        }
        
        public void UpdateComboUI(int streak, float multiplier)
        {
            if(comboText != null)
            {
                comboText.text = $"Combo: {streak}\nMultiplier: x{multiplier}";
            }
        }
        
        public void UpdateRootProgress(float normalizedHealth)
        {
            if(rootHealthSlider != null)
            {
                rootHealthSlider.value = normalizedHealth;
            }
        }
    }
}
