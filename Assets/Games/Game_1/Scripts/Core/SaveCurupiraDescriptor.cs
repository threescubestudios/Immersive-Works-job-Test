using UnityEngine;

namespace SaveCurupira.Core
{
    public enum DifficultyLevel { Easy, Medium, Hard }

    [System.Serializable]
    public class SpawnMilestone
    {
        public float timeMarkSeconds; // e.g. 0, 30, 60...
        public float secondsBetweenFruits; // e.g. 3.0, 1.7...
    }

    [System.Serializable]
    public class DifficultySettings
    {
        public int rootCount;
        public int rootHealth;
        public SpawnMilestone[] spawnMilestones;
    }

    [CreateAssetMenu(fileName = "SaveCurupiraSettings", menuName = "Games/SaveCurupira/Game Settings")]
    public class SaveCurupiraDescriptor : ScriptableObject
    {
        [Header("Global Settings")]
        public DifficultyLevel currentDifficulty = DifficultyLevel.Easy;
        
        [Header("Difficulty Configurations")]
        public DifficultySettings easySettings;
        public DifficultySettings mediumSettings;
        public DifficultySettings hardSettings;
        
        [Header("Spawn Settings")]
        public float maxSpawnAngleDeviation = 30f;
        
        public DifficultySettings GetCurrentSettings()
        {
            switch (currentDifficulty)
            {
                case DifficultyLevel.Easy: return easySettings;
                case DifficultyLevel.Medium: return mediumSettings;
                case DifficultyLevel.Hard: return hardSettings;
                default: return easySettings;
            }
        }
        
        private void Reset()
        {
            easySettings = new DifficultySettings {
                rootCount = 4, rootHealth = 6,
                spawnMilestones = new SpawnMilestone[] {
                    new SpawnMilestone { timeMarkSeconds = 0f, secondsBetweenFruits = 3.0f },
                    new SpawnMilestone { timeMarkSeconds = 30f, secondsBetweenFruits = 1.7f },
                    new SpawnMilestone { timeMarkSeconds = 60f, secondsBetweenFruits = 1.1f },
                    new SpawnMilestone { timeMarkSeconds = 90f, secondsBetweenFruits = 0.9f },
                    new SpawnMilestone { timeMarkSeconds = 120f, secondsBetweenFruits = 0.7f },
                    new SpawnMilestone { timeMarkSeconds = 150f, secondsBetweenFruits = 0.6f },
                    new SpawnMilestone { timeMarkSeconds = 180f, secondsBetweenFruits = 0.5f }
                }
            };
            mediumSettings = new DifficultySettings {
                rootCount = 6, rootHealth = 8,
                spawnMilestones = new SpawnMilestone[] {
                    new SpawnMilestone { timeMarkSeconds = 0f, secondsBetweenFruits = 3.0f },
                    new SpawnMilestone { timeMarkSeconds = 30f, secondsBetweenFruits = 1.4f },
                    new SpawnMilestone { timeMarkSeconds = 60f, secondsBetweenFruits = 1.0f },
                    new SpawnMilestone { timeMarkSeconds = 90f, secondsBetweenFruits = 0.7f },
                    new SpawnMilestone { timeMarkSeconds = 120f, secondsBetweenFruits = 0.6f },
                    new SpawnMilestone { timeMarkSeconds = 150f, secondsBetweenFruits = 0.5f },
                    new SpawnMilestone { timeMarkSeconds = 180f, secondsBetweenFruits = 0.4f }
                }
            };
            hardSettings = new DifficultySettings {
                rootCount = 8, rootHealth = 10,
                spawnMilestones = new SpawnMilestone[] {
                    new SpawnMilestone { timeMarkSeconds = 0f, secondsBetweenFruits = 3.0f },
                    new SpawnMilestone { timeMarkSeconds = 30f, secondsBetweenFruits = 1.3f },
                    new SpawnMilestone { timeMarkSeconds = 60f, secondsBetweenFruits = 0.8f },
                    new SpawnMilestone { timeMarkSeconds = 90f, secondsBetweenFruits = 0.6f },
                    new SpawnMilestone { timeMarkSeconds = 120f, secondsBetweenFruits = 0.5f },
                    new SpawnMilestone { timeMarkSeconds = 150f, secondsBetweenFruits = 0.4f },
                    new SpawnMilestone { timeMarkSeconds = 180f, secondsBetweenFruits = 0.3f }
                }
            };
        }
    }
}
