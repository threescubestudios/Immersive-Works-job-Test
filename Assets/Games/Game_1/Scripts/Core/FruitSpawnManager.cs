using System.Collections.Generic;
using UnityEngine;

namespace SaveCurupira.Core
{
    public class FruitSpawnManager : MonoBehaviour
    {
        public GameObject fruitPrefab;
        public Transform[] spawnPoints;
        public SaveCurupiraDescriptor descriptor;
        
        private float spawnTimer;
        private Queue<GameObject> fruitPool = new Queue<GameObject>();

        private void Update()
        {
            if (CurupiraGameController.Instance.CurrentState != CurupiraGameController.GameState.Playing)
                return;

            float totalTime = 180f;
            float timeElapsed = totalTime - CurupiraGameController.Instance.GetTimeRemaining();
            float spawnRate = CalculateSpawnRate(timeElapsed, 180f);
            
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                SpawnFruit();
                spawnTimer = spawnRate;
            }
        }

        private float CalculateSpawnRate(float timeElapsed, float totalTime)
        {
            if (descriptor == null) return 3.0f;
            
            DifficultySettings settings = descriptor.GetCurrentSettings();
            if (settings.spawnMilestones == null || settings.spawnMilestones.Length == 0) return 3.0f;
            
            SpawnMilestone prev = settings.spawnMilestones[0];
            SpawnMilestone next = settings.spawnMilestones[settings.spawnMilestones.Length - 1];
            
            for (int i = 0; i < settings.spawnMilestones.Length - 1; i++)
            {
                if (timeElapsed >= settings.spawnMilestones[i].timeMarkSeconds && 
                    timeElapsed < settings.spawnMilestones[i+1].timeMarkSeconds)
                {
                    prev = settings.spawnMilestones[i];
                    next = settings.spawnMilestones[i+1];
                    break;
                }
            }
            
            if (timeElapsed >= next.timeMarkSeconds) return next.secondsBetweenFruits;
            if (timeElapsed <= prev.timeMarkSeconds) return prev.secondsBetweenFruits;
            
            float segmentDuration = next.timeMarkSeconds - prev.timeMarkSeconds;
            float timeInSegment = timeElapsed - prev.timeMarkSeconds;
            float t = timeInSegment / segmentDuration;
            
            return Mathf.Lerp(prev.secondsBetweenFruits, next.secondsBetweenFruits, t);
        }

        public void SpawnSingleFruit()
        {
            SpawnFruit();
        }

        private void SpawnFruit()
        {
            if (spawnPoints.Length == 0) return;
            
            // 0: Zone A (Left Side)
            // 1: Zone B (Center Left - Top)
            // 2: Zone C (Center Right - Top)
            // 3: Zone D (Right Side)
            int zone = Random.Range(0, 4);
            
            Vector3 startPos = Vector3.zero;
            Vector3 exactVelocity = Vector3.zero;
            
            switch (zone)
            {
                case 0: // Zone A (Left) - Arcs right
                    startPos = new Vector3(-4.5f, 0.5f, 5f);
                    exactVelocity = new Vector3(Random.Range(3.5f, 5f), Random.Range(6.5f, 8f), 0f);
                    break;
                case 1: // Zone B (Center Left, Top) - Tossed just below the ceiling to fall gracefully
                    startPos = new Vector3(-1.5f, 3.5f, 5f);
                    exactVelocity = new Vector3(Random.Range(-0.5f, 1f), Random.Range(3.5f, 4.5f), 0f);
                    break;
                case 2: // Zone C (Center Right, Top) - Tossed just below the ceiling to fall gracefully
                    startPos = new Vector3(1.5f, 3.5f, 5f);
                    exactVelocity = new Vector3(Random.Range(-1f, 0.5f), Random.Range(3.5f, 4.5f), 0f);
                    break;
                case 3: // Zone D (Right) - Arcs left
                    startPos = new Vector3(4.5f, 0.5f, 5f);
                    exactVelocity = new Vector3(-Random.Range(3.5f, 5f), Random.Range(6.5f, 8f), 0f);
                    break;
            }
            
            GameObject fruit = GetFruitFromPool();
            fruit.transform.position = startPos;
            fruit.SetActive(true);

            if(fruit.TryGetComponent(out Gameplay.FruitBehaviour behaviour))
            {
                // Pass the exact calculated velocity
                behaviour.Initialize(exactVelocity);
            }
        }

        private GameObject GetFruitFromPool()
        {
            if (fruitPool.Count > 0)
            {
                return fruitPool.Dequeue();
            }
            GameObject newFruit = Instantiate(fruitPrefab);
            newFruit.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            return newFruit;
        }

        public void ReturnToPool(GameObject fruit)
        {
            fruit.SetActive(false);
            fruitPool.Enqueue(fruit);
        }
    }
}
