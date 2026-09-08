using UnityEngine;

namespace SaveCurupira.Core
{
    public class SwipeInputManager : MonoBehaviour
    {
        public static SwipeInputManager Instance { get; private set; }
        public int TotalSwipes { get; private set; } = 0;

        private class SwipeData
        {
            public Vector2 startUV;
            public float startTime;
        }

        private System.Collections.Generic.Dictionary<int, SwipeData> activeSwipes = new System.Collections.Generic.Dictionary<int, SwipeData>();

        public float minSwipeDistanceUV = 0.05f;

        private Camera mainCamera;

        private Camera GetMainCamera()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("[SwipeInputManager] No Camera.main found in scene!");
                }
            }
            return mainCamera;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

        private void Start()
        {
            mainCamera = Camera.main;
        }

        public void ProcessSensorTouchStart(int playerId, Vector2 uv)
        {
            if (!activeSwipes.ContainsKey(playerId))
            {
                activeSwipes.Add(playerId, new SwipeData { startUV = uv, startTime = Time.time });
            }
            else
            {
                activeSwipes[playerId].startUV = uv;
                activeSwipes[playerId].startTime = Time.time;
            }
        }

        public void ProcessSensorTouchEnd(int playerId, Vector2 endUV)
        {
            if (activeSwipes.TryGetValue(playerId, out SwipeData data))
            {
                float swipeTime = Time.time - data.startTime;
                Vector2 swipeDelta = endUV - data.startUV;

                if (swipeDelta.magnitude > minSwipeDistanceUV)
                {
                    ProcessSwipe(data.startUV, endUV, swipeTime);
                }
                activeSwipes.Remove(playerId);
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 screenPos = Input.mousePosition;
                Vector2 uv = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
                ProcessSensorTouchStart(999, uv);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Vector3 screenPos = Input.mousePosition;
                Vector2 uv = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
                ProcessSensorTouchEnd(999, uv);
            }
#endif
        }

        private float DistancePointToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ap = p - a;
            float sqrLenAB = ab.sqrMagnitude;
            if (sqrLenAB == 0) return ap.magnitude;

            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / sqrLenAB);
            Vector2 projection = a + t * ab;
            return Vector2.Distance(p, projection);
        }

        private void ProcessSwipe(Vector2 startUV, Vector2 endUV, float swipeTime)
        {
            TotalSwipes++;

            Gameplay.FruitBehaviour[] allFruits = FindObjectsOfType<Gameplay.FruitBehaviour>();
            Gameplay.FruitBehaviour hitFruit = null;
            float bestDist = float.MaxValue;

            foreach (var f in allFruits)
            {
                if (!f.gameObject.activeInHierarchy || f.isSwiped) continue;

                Camera cam = GetMainCamera();
                if (cam == null) continue;

                Vector3 viewportPos = cam.WorldToViewportPoint(f.transform.position);
                if (viewportPos.z < 0) continue;

                Vector2 fruitUV = new Vector2(viewportPos.x, viewportPos.y);
                float dist = DistancePointToLineSegment(fruitUV, startUV, endUV);

                if (dist < 0.35f && dist < bestDist)
                {
                    bestDist = dist;
                    hitFruit = f;
                }
            }

            if (hitFruit != null)
            {
                ApplyAutoAimToFruit(hitFruit);
            }
        }

        public void ProcessPlatformSwipe(ImmersiWorks.Platform.Core.Swipe e)
        {
            TotalSwipes++;

            Debug.Log(
                $"[SwipeInputManager] Native Platform Swipe detected: {e.Direction}"
            );

            Gameplay.FruitBehaviour[] allFruits =
                FindObjectsOfType<Gameplay.FruitBehaviour>();

            Gameplay.FruitBehaviour bestFruit = null;
            float bestDistance = float.MaxValue;

            Camera cam = GetMainCamera();

            if (cam == null)
                return;

            foreach (var fruit in allFruits)
            {
                if (!fruit.gameObject.activeInHierarchy)
                    continue;

                if (fruit.isSwiped)
                    continue;

                Vector3 viewportPos =
                    cam.WorldToViewportPoint(
                        fruit.transform.position
                    );

                if (viewportPos.z < 0f)
                    continue;

                // Prefer fruits that are lower on screen,
                // because those are closer to the swipeable zone.
                float distanceFromCenter =
                    Vector2.Distance(
                        new Vector2(
                            viewportPos.x,
                            viewportPos.y
                        ),
                        new Vector2(0.5f, 0.5f)
                    );

                if (distanceFromCenter < bestDistance)
                {
                    bestDistance = distanceFromCenter;
                    bestFruit = fruit;
                }
            }

            if (bestFruit != null)
            {
                ApplyAutoAimToFruit(bestFruit);

                Debug.Log(
                    "[SwipeInputManager] Platform swipe targeted one fruit."
                );
            }
        }

        private void ApplyAutoAimToFruit(Gameplay.FruitBehaviour fruit)
        {
            Gameplay.RootBehaviour[] allRoots = FindObjectsOfType<Gameplay.RootBehaviour>(true);
            System.Collections.Generic.List<Gameplay.RootBehaviour> activeRoots = new System.Collections.Generic.List<Gameplay.RootBehaviour>();

            foreach (var r in allRoots)
            {
                if (r.gameObject.activeInHierarchy && r.health > 0) activeRoots.Add(r);
            }

            if (activeRoots.Count > 0)
            {
                Gameplay.RootBehaviour targetRoot = activeRoots[Random.Range(0, activeRoots.Count)];

                float flightTime = 0.5f;
                Vector3 targetPos = targetRoot.transform.position + new Vector3(0, 0.5f, 0);
                Vector3 startPos = fruit.transform.position;

                Vector3 distance = targetPos - startPos;

                float gravityMagnitude = Mathf.Abs(Physics.gravity.y);
                Vector3 velocityY = Vector3.up * (distance.y / flightTime + 0.5f * gravityMagnitude * flightTime);
                Vector3 velocityXZ = new Vector3(distance.x, 0, distance.z) / flightTime;

                Vector3 finalVelocity = velocityXZ + velocityY;

                Debug.Log($"[SwipeInputManager] Fruit arcing from {startPos} to {targetPos}. Speed: {finalVelocity.magnitude:F2}");

                fruit.OnSwiped(finalVelocity);
            }
            else
            {
                fruit.OnSwiped(new Vector3(0, 8f, 5f));
            }
        }
    }
}   