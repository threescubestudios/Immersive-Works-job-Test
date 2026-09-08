using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaveCurupira.Core
{
    public class FruitSpawnManager : MonoBehaviour
    {
        [Header("Fruit")]
        [SerializeField] private GameObject fruitPrefab;
        [SerializeField] private float fruitLifetime = 5f;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Difficulty")]
        [SerializeField] private SaveCurupiraDescriptor descriptor;

        private float spawnTimer;

        private readonly Queue<GameObject> fruitPool =
            new Queue<GameObject>();

        private readonly Dictionary<GameObject, Coroutine>
            lifetimeCoroutines =
            new Dictionary<GameObject, Coroutine>();

        private void Update()
        {
            if (CurupiraGameController.Instance == null)
                return;

            if (CurupiraGameController.Instance.CurrentState !=
                CurupiraGameController.GameState.Playing)
                return;

            float totalTime = 180f;

            float timeElapsed =
                totalTime -
                CurupiraGameController.Instance.GetTimeRemaining();

            float spawnRate =
                CalculateSpawnRate(timeElapsed, totalTime);

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
            {
                SpawnFruit();
                spawnTimer = spawnRate;
            }
        }

        private float CalculateSpawnRate(
            float timeElapsed,
            float totalTime)
        {
            if (descriptor == null)
                return 2.5f;

            DifficultySettings settings =
                descriptor.GetCurrentSettings();

            if (settings.spawnMilestones == null ||
                settings.spawnMilestones.Length == 0)
                return 2.5f;

            SpawnMilestone prev =
                settings.spawnMilestones[0];

            SpawnMilestone next =
                settings.spawnMilestones[
                    settings.spawnMilestones.Length - 1
                ];

            for (int i = 0;
                 i < settings.spawnMilestones.Length - 1;
                 i++)
            {
                if (timeElapsed >=
                        settings.spawnMilestones[i].timeMarkSeconds &&
                    timeElapsed <
                        settings.spawnMilestones[i + 1]
                            .timeMarkSeconds)
                {
                    prev = settings.spawnMilestones[i];
                    next = settings.spawnMilestones[i + 1];
                    break;
                }
            }

            if (timeElapsed >= next.timeMarkSeconds)
                return next.secondsBetweenFruits;

            if (timeElapsed <= prev.timeMarkSeconds)
                return prev.secondsBetweenFruits;

            float segmentDuration =
                next.timeMarkSeconds -
                prev.timeMarkSeconds;

            if (segmentDuration <= 0f)
                return next.secondsBetweenFruits;

            float timeInSegment =
                timeElapsed -
                prev.timeMarkSeconds;

            float t =
                timeInSegment / segmentDuration;

            return Mathf.Lerp(
                prev.secondsBetweenFruits,
                next.secondsBetweenFruits,
                t
            );
        }

        public void SpawnSingleFruit()
        {
            SpawnFruit();
        }

        private void SpawnFruit()
        {
            if (fruitPrefab == null)
            {
                Debug.LogError(
                    "[FruitSpawnManager] Fruit prefab is not assigned!"
                );

                return;
            }

            if (spawnPoints == null ||
                spawnPoints.Length == 0)
            {
                Debug.LogWarning(
                    "[FruitSpawnManager] No spawn points assigned!"
                );

                return;
            }

            Transform spawnPoint =
                spawnPoints[
                    Random.Range(0, spawnPoints.Length)
                ];

            GameObject fruit = GetFruitFromPool();

            fruit.transform.position =
                spawnPoint.position;

            fruit.transform.rotation =
                spawnPoint.rotation;

            fruit.SetActive(true);

            Gameplay.FruitBehaviour behaviour =
                fruit.GetComponent<Gameplay.FruitBehaviour>();

            if (behaviour != null)
            {
                Vector3 fallingVelocity =
                    new Vector3(0f, -2f, 0f);

                behaviour.Initialize(fallingVelocity);
            }

            StartFruitLifetime(fruit);
        }

        private void StartFruitLifetime(GameObject fruit)
        {
            if (lifetimeCoroutines.TryGetValue(
                    fruit,
                    out Coroutine oldCoroutine))
            {
                if (oldCoroutine != null)
                    StopCoroutine(oldCoroutine);
            }

            Coroutine coroutine =
                StartCoroutine(
                    ReturnFruitAfterLifetime(fruit)
                );

            lifetimeCoroutines[fruit] = coroutine;
        }

        private IEnumerator ReturnFruitAfterLifetime(
            GameObject fruit)
        {
            yield return new WaitForSeconds(
                fruitLifetime
            );

            if (fruit == null)
                yield break;

            if (!fruit.activeInHierarchy)
                yield break;

            Gameplay.FruitBehaviour behaviour =
                fruit.GetComponent<Gameplay.FruitBehaviour>();

            if (behaviour != null)
                behaviour.OnExpired();
        }

        private GameObject GetFruitFromPool()
        {
            while (fruitPool.Count > 0)
            {
                GameObject pooledFruit =
                    fruitPool.Dequeue();

                if (pooledFruit != null)
                    return pooledFruit;
            }

            GameObject newFruit =
                Instantiate(fruitPrefab);

            newFruit.transform.localScale =
                new Vector3(0.5f, 0.5f, 0.5f);

            return newFruit;
        }

        public void ReturnToPool(GameObject fruit)
        {
            if (fruit == null)
                return;

            if (lifetimeCoroutines.TryGetValue(
                    fruit,
                    out Coroutine coroutine))
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);

                lifetimeCoroutines.Remove(fruit);
            }

            fruit.SetActive(false);

            if (!fruitPool.Contains(fruit))
                fruitPool.Enqueue(fruit);
        }

        public void ClearAllFruits()
        {
            foreach (GameObject fruit in fruitPool)
            {
                if (fruit != null)
                    fruit.SetActive(false);
            }

            lifetimeCoroutines.Clear();
        }

        private void OnDisable()
        {
            foreach (Coroutine coroutine
                     in lifetimeCoroutines.Values)
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
            }

            lifetimeCoroutines.Clear();
        }
    }
}   