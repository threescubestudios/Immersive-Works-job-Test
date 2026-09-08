using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace SaveCurupira.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [SerializeField] private Text victoryText;
        [SerializeField] private Text defeatText;
        public Text tutorialText;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            HidePanels();
        }

        public void onClickPlayAgain()
        {
            SceneManager.LoadScene(0);
        }

        public void onClickQuit()
        {
            Application.Quit();
        }
        public void ShowVictoryPanel(
            int score,
            float accuracy)
        {
            if (victoryPanel == null)
                return;

            if (defeatPanel != null)
                defeatPanel.SetActive(false);

            if (victoryText != null)
            {
                victoryText.text =
                    $"CURUPIRA SAVED!\n\n" +
                    $"Score: {score}\n" +
                    $"Accuracy: {accuracy:F1}%";
            }

            victoryPanel.SetActive(true);

            Time.timeScale = 0f;

            Debug.Log(
                "[UIManager] Victory Panel Shown!"
            );
        }

        public void ShowDefeatPanel(
            int score,
            int missCount,
            string reason)
        {
            if (defeatPanel == null)
                return;

            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            if (defeatText != null)
            {
                defeatText.text =
                    $"GAME OVER!\n\n" +
                    $"{reason}\n\n" +
                    $"Score: {score}\n" +
                    $"Misses: {missCount}/5";
            }

            defeatPanel.SetActive(true);

            Time.timeScale = 0f;

            Debug.Log(
                $"[UIManager] Defeat Panel Shown: {reason}"
            );
        }

        public void HidePanels()
        {
            if (victoryPanel != null)
                victoryPanel.SetActive(false);

            if (defeatPanel != null)
                defeatPanel.SetActive(false);

            Time.timeScale = 1f;
        }
    }
}