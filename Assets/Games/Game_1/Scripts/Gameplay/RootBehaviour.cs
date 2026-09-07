using UnityEngine;

namespace SaveCurupira.Gameplay
{
    public class RootBehaviour : MonoBehaviour
    {
        public int health = 60;
        private int maxHealth = 60;
        private bool isDestroyed = false;
        
        public RectTransform healthFill;
        public Canvas healthCanvas;
        private float showHealthTimer = 0f;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (healthCanvas != null)
            {
                canvasGroup = healthCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = healthCanvas.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                healthCanvas.gameObject.SetActive(false);
            }
        }

        public void Initialize(int startingHealth)
        {
            maxHealth = startingHealth;
            health = startingHealth;
            isDestroyed = false;
            gameObject.SetActive(true);
            
            if (healthFill != null)
            {
                healthFill.anchorMax = new Vector2(1f, 1f); // Full
            }
            
            if (healthCanvas != null)
            {
                if (canvasGroup != null) canvasGroup.alpha = 0f;
                healthCanvas.gameObject.SetActive(false);
            }
        }

        public void TakeDamage(int amount)
        {
            if (isDestroyed) return;

            health -= amount;
            showHealthTimer = 2f; // Show for 2 seconds
            
            if (healthFill != null)
            {
                float healthPercent = (float)Mathf.Max(0, health) / maxHealth;
                healthFill.anchorMax = new Vector2(healthPercent, 1f); // Scale down horizontally
            }
            
            if (health <= 0)
            {
                isDestroyed = true;
                if (Core.CurupiraGameController.Instance != null)
                {
                    Core.CurupiraGameController.Instance.OnRootDestroyed();
                }
                gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (healthCanvas != null && canvasGroup != null && !isDestroyed)
            {
                if (showHealthTimer > 0)
                {
                    showHealthTimer -= Time.deltaTime;
                    if (!healthCanvas.gameObject.activeSelf) healthCanvas.gameObject.SetActive(true);
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime * 5f); // Fade in fast
                }
                else
                {
                    if (canvasGroup.alpha > 0)
                    {
                        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime * 2f); // Fade out smoothly
                    }
                    else if (healthCanvas.gameObject.activeSelf)
                    {
                        healthCanvas.gameObject.SetActive(false); // Disable when fully faded out
                    }
                }
            }
        }
    }
}
