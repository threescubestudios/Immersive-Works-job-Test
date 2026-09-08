using UnityEngine;

namespace SaveCurupira.Gameplay
{
    public class RootBehaviour : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int defaultHealth = 3;

        public int health { get; private set; }

        private int maxHealth;
        private bool isDestroyed;

        [Header("Health UI")]
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private Canvas healthCanvas;

        [SerializeField] private float healthDisplayDuration = 2f;
        [SerializeField] private float fadeInSpeed = 5f;
        [SerializeField] private float fadeOutSpeed = 2f;

        private float showHealthTimer;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (healthCanvas != null)
            {
                canvasGroup =
                    healthCanvas.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup =
                        healthCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            maxHealth = Mathf.Max(1, defaultHealth);
            health = maxHealth;

            HideHealthBar();
        }

        public void Initialize(int startingHealth)
        {
            maxHealth = Mathf.Max(1, startingHealth);
            health = maxHealth;
            isDestroyed = false;
            showHealthTimer = 0f;

            UpdateHealthBar();
            HideHealthBar();

            Debug.Log(
                $"[RootBehaviour] {gameObject.name} initialized with {health} HP."
            );
        }

        public void TakeDamage(int amount)
        {
            if (isDestroyed)
                return;

            if (amount <= 0)
                return;

            health -= amount;
            health = Mathf.Max(0, health);

            ShowHealthBar();
            UpdateHealthBar();

            Debug.Log(
                $"[RootBehaviour] {gameObject.name} hit! " +
                $"Health: {health}/{maxHealth}"
            );

            if (health <= 0)
            {
                DestroyRoot();
            }
        }

        private void DestroyRoot()
        {
            if (isDestroyed)
                return;

            isDestroyed = true;

            HideHealthBar();

            Debug.Log(
                $"[RootBehaviour] {gameObject.name} destroyed!"
            );

            if (Core.CurupiraGameController.Instance != null)
            {
                Core.CurupiraGameController.Instance
                    .OnRootDestroyed();
            }

            gameObject.SetActive(false);
        }

        private void UpdateHealthBar()
        {
            if (healthFill == null)
                return;

            float healthPercent =
                maxHealth > 0
                    ? (float)health / maxHealth
                    : 0f;

            healthFill.anchorMin =
                new Vector2(0f, 0f);

            healthFill.anchorMax =
                new Vector2(
                    Mathf.Clamp01(healthPercent),
                    1f
                );
        }

        private void ShowHealthBar()
        {
            if (healthCanvas == null ||
                canvasGroup == null)
                return;

            showHealthTimer =
                healthDisplayDuration;

            healthCanvas.gameObject.SetActive(true);
        }

        private void HideHealthBar()
        {
            showHealthTimer = 0f;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (healthCanvas != null)
                healthCanvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (healthCanvas == null ||
                canvasGroup == null ||
                isDestroyed)
                return;

            if (showHealthTimer > 0f)
            {
                showHealthTimer -= Time.deltaTime;

                if (!healthCanvas.gameObject.activeSelf)
                    healthCanvas.gameObject.SetActive(true);

                canvasGroup.alpha =
                    Mathf.MoveTowards(
                        canvasGroup.alpha,
                        1f,
                        Time.deltaTime * fadeInSpeed
                    );
            }
            else
            {
                canvasGroup.alpha =
                    Mathf.MoveTowards(
                        canvasGroup.alpha,
                        0f,
                        Time.deltaTime * fadeOutSpeed
                    );

                if (canvasGroup.alpha <= 0f)
                {
                    healthCanvas.gameObject.SetActive(false);
                }
            }
        }
    }
}