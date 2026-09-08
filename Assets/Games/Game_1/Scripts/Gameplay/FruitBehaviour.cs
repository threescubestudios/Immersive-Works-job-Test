using UnityEngine;
using SaveCurupira.Core;

namespace SaveCurupira.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class FruitBehaviour : MonoBehaviour
    {
        private Rigidbody rb;
        private FruitSpawnManager spawner;

        public bool isSwiped { get; private set; }

        private bool isDespawning;
        private bool isSwipeable;

        [Header("Swipe")]
        [SerializeField] private float swipeableYThreshold = 4.5f;

        [Header("Impact")]
        [SerializeField] private float impactParticleLifetime = 2f;

        private TrailRenderer trail;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            trail = GetComponent<TrailRenderer>();
        }

        private void Start()
        {
            spawner = FindObjectOfType<FruitSpawnManager>();

            if (trail != null)
                trail.emitting = false;
        }

        private void Update()
        {
            if (!isSwipeable &&
                transform.position.y < swipeableYThreshold)
            {
                isSwipeable = true;

                Debug.Log(
                    $"[FruitBehaviour] Fruit is now swipeable! " +
                    $"Y={transform.position.y:F2}"
                );
            }
        }

        public void Initialize(Vector3 dropVelocity)
        {
            isSwiped = false;
            isDespawning = false;
            isSwipeable = false;

            if (rb == null)
                rb = GetComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.drag = 0f;
            rb.angularDrag = 0.05f;

            rb.velocity = dropVelocity;

            rb.angularVelocity = new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f)
            );

            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        public void OnSwiped(Vector3 swipeForce)
        {
            if (isDespawning || isSwiped)
                return;

            if (!isSwipeable)
            {
                Debug.Log(
                    $"[FruitBehaviour] Fruit not yet swipeable! " +
                    $"Y={transform.position.y:F2}, " +
                    $"need < {swipeableYThreshold:F2}"
                );

                return;
            }

            isSwiped = true;

            rb.velocity = swipeForce;

            Debug.Log(
                $"[FruitBehaviour] Fruit swiped! " +
                $"Velocity: {swipeForce.magnitude:F2}"
            );

            EnableTrail();
        }

        private void EnableTrail()
        {
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();

                trail.time = 0.2f;
                trail.startWidth = 0.5f;
                trail.endWidth = 0f;
                trail.material =
                    new Material(Shader.Find("Sprites/Default"));

                trail.material.color = Color.yellow;
            }

            trail.Clear();
            trail.emitting = true;
        }

        /// <summary>
        /// Called by FruitSpawnManager when this fruit has
        /// existed long enough without being successfully used.
        /// </summary>
        public void OnExpired()
        {
            if (isDespawning || isSwiped)
                return;

            isDespawning = true;

            Debug.Log("[FruitBehaviour] Fruit expired - registering miss.");

            if (ComboManager.Instance != null)
                ComboManager.Instance.RegisterMiss();

            ReturnToPool();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isDespawning)
                return;

            // Root collision
            RootBehaviour root =
                collision.gameObject.GetComponentInParent<RootBehaviour>();

            if (root != null)
            {
                // Only a swiped fruit should damage a root.
                if (!isSwiped)
                    return;

                isDespawning = true;

                if (ComboManager.Instance != null)
                    ComboManager.Instance.RegisterHit();

                root.TakeDamage(1);

                CreateImpactParticles(transform.position);

                ReturnToPool();

                return;
            }

            // Ground collision
            if (IsGround(collision.gameObject))
            {
                isDespawning = true;

                if (ComboManager.Instance != null)
                    ComboManager.Instance.RegisterMiss();

                Debug.Log(
                    "[FruitBehaviour] Fruit reached ground - miss."
                );

                ReturnToPool();

                return;
            }

            // IMPORTANT:
            // Do not destroy/return the fruit merely because it
            // touched an unrelated collider.
        }

        private bool IsGround(GameObject obj)
        {
            if (obj == null)
                return false;

            return obj.CompareTag("Ground") ||
                   obj.name == "Ground" ||
                   obj.name.Contains("Ground");
        }

        private void ReturnToPool()
        {
            if (spawner != null)
            {
                spawner.ReturnToPool(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CreateImpactParticles(Vector3 pos)
        {
            GameObject particleGO =
                new GameObject("ImpactParticles");

            particleGO.transform.position = pos;

            ParticleSystem ps =
                particleGO.AddComponent<ParticleSystem>();

            ps.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSpeed = 15f;
            main.startSize = 0.08f;
            main.startColor = Color.yellow;
            main.gravityModifier = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;

            emission.SetBursts(
                new ParticleSystem.Burst[]
                {
                    new ParticleSystem.Burst(0f, 40)
                }
            );

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var renderer =
                particleGO.GetComponent<ParticleSystemRenderer>();

            renderer.material =
                new Material(Shader.Find("Sprites/Default"));

            ps.Play();

            Destroy(particleGO, impactParticleLifetime);
        }
    }
}