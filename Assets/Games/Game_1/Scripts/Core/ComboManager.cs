using UnityEngine;

namespace SaveCurupira.Core
{
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        public int TotalScore { get; private set; }
        public int TotalHits { get; private set; }
        public int CurrentCombo { get; private set; }

        public int MaxMisses => MAX_MISSES;

        private int totalMisses;

        private const int MAX_MISSES = 5;

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

        public void ResetStats()
        {
            TotalScore = 0;
            TotalHits = 0;
            CurrentCombo = 0;
            totalMisses = 0;

            Debug.Log("[ComboManager] Stats reset.");
        }

        public void RegisterHit()
        {
            if (CurupiraGameController.Instance != null &&
                CurupiraGameController.Instance.CurrentState !=
                CurupiraGameController.GameState.Playing &&
                CurupiraGameController.Instance.CurrentState !=
                CurupiraGameController.GameState.Tutorial)
            {
                return;
            }

            CurrentCombo++;
            TotalHits++;

            int points = 100 * CurrentCombo;

            TotalScore += points;

            Debug.Log(
                $"Hit! Streak: {CurrentCombo}, " +
                $"Points: +{points}, " +
                $"Total Score: {TotalScore}"
            );

            if (CurupiraGameController.Instance != null)
            {
                CurupiraGameController.Instance
                    .ShowComboMessage(
                        $"Hit! Streak: {CurrentCombo}, " +
                        $"Points: +{points}"
                    );
            }
        }

        public void RegisterMiss()
        {
            if (CurupiraGameController.Instance != null &&
                CurupiraGameController.Instance.CurrentState !=
                CurupiraGameController.GameState.Playing &&
                CurupiraGameController.Instance.CurrentState !=
                CurupiraGameController.GameState.Tutorial)
            {
                return;
            }

            totalMisses++;
            CurrentCombo = 0;

            Debug.Log(
                $"Combo Broken! " +
                $"Misses: {totalMisses}/{MAX_MISSES}"
            );

            if (CurupiraGameController.Instance != null)
            {
                CurupiraGameController.Instance
                    .ShowComboMessage(
                        $"Missed! " +
                        $"({totalMisses}/{MAX_MISSES})"
                    );
            }

            if (totalMisses >= MAX_MISSES)
            {
                Debug.Log(
                    "[ComboManager] Too many misses! Game Over!"
                );

                if (CurupiraGameController.Instance != null)
                {
                    CurupiraGameController.Instance
                        .OnTooManyMisses();
                }
            }
        }

        public int GetMissCount()
        {
            return totalMisses;
        }
    }
}