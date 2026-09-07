using UnityEngine;

namespace SaveCurupira.Core
{
    public class ComboManager : MonoBehaviour
    {
        public static ComboManager Instance { get; private set; }

        private int currentStreak = 0;
        public int CurrentMultiplier { get; private set; } = 1;
        public int TotalScore { get; private set; } = 0;
        public int TotalHits { get; private set; } = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void RegisterHit()
        {
            currentStreak++;
            TotalHits++;
            UpdateMultiplier();
            
            int pointsEarned = 100 * CurrentMultiplier;
            TotalScore += pointsEarned;
            
            if (CurupiraGameController.Instance != null)
            {
                CurupiraGameController.Instance.AddPoints(pointsEarned);
            }
            
            Debug.Log($"Hit! Streak: {currentStreak}, Points: +{pointsEarned}, Total Score: {TotalScore}");
        }

        public void RegisterMiss()
        {
            if (currentStreak >= 5 && CurupiraGameController.Instance != null) 
            {
                CurupiraGameController.Instance.ShowComboMessage("Combo Broken!");
            }
            
            currentStreak = 0;
            CurrentMultiplier = 1;
            Debug.Log("Combo Broken!");
        }

        private void UpdateMultiplier()
        {
            int previousMultiplier = CurrentMultiplier;

            if (currentStreak >= 10)
            {
                CurrentMultiplier = 4;
            }
            else if (currentStreak >= 5)
            {
                CurrentMultiplier = 2;
            }
            else
            {
                CurrentMultiplier = 1;
            }

            if (CurrentMultiplier > previousMultiplier && CurupiraGameController.Instance != null)
            {
                CurupiraGameController.Instance.ShowComboMessage($"Combo x{CurrentMultiplier}!");
            }
        }
    }
}
