using UnityEngine;
using SaveCurupira.Core;

namespace SaveCurupira.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class FruitBehaviour : MonoBehaviour
    {
        private Rigidbody rb;
        public bool isSwiped { get; private set; } = false;
        private bool isDespawning = false;
        private FruitSpawnManager spawner;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }
        
        private void Start()
        {
            spawner = FindObjectOfType<FruitSpawnManager>();
        }

        public void Initialize(Vector3 dropDirection)
        {
            isSwiped = false;
            isDespawning = false;
            StopAllCoroutines();
            rb.drag = 0f;
            rb.angularDrag = 0.05f;
            rb.velocity = dropDirection; // We now receive the exact velocity from the spawner
            rb.angularVelocity = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)); // Spin naturally
            
            var trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.emitting = false;
        }

        public void OnSwiped(Vector3 swipeForce)
        {
            isSwiped = true;
            // Force strict velocity to ensure perfect Auto-Aim physics regardless of object mass
            rb.velocity = swipeForce;
            
            // Add or activate motion trail dynamically
            var trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
                trail.time = 0.2f;
                trail.startWidth = 0.5f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.material.color = Color.yellow;
            }
            trail.emitting = true;
            trail.Clear();
        }

        private void OnCollisionEnter(Collision collision)
        {
            RootBehaviour root = collision.gameObject.GetComponentInParent<RootBehaviour>();
            if (root != null)
            {
                if (ComboManager.Instance != null) ComboManager.Instance.RegisterHit();
                
                root.TakeDamage(1);
                
                // Spawn procedural impact burst!
                CreateImpactParticles(transform.position);
                
                if (spawner != null) spawner.ReturnToPool(gameObject);
            }
            else if (collision.gameObject.name == "Ground" || collision.gameObject.name.Contains("Ground"))
            {
                if (!isDespawning)
                {
                    isDespawning = true;
                    // If the fruit hits the ground, it's an instant miss!
                    if (ComboManager.Instance != null) 
                    {
                        ComboManager.Instance.RegisterMiss();
                    }
                    if (spawner != null) spawner.ReturnToPool(gameObject);
                }
            }
        }

        private void CreateImpactParticles(Vector3 pos)
        {
            GameObject particleGO = new GameObject("ImpactParticles");
            particleGO.transform.position = pos;
            
            ParticleSystem ps = particleGO.AddComponent<ParticleSystem>();
            
            // Stop the system immediately because AddComponent auto-plays it, which locks the duration property
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startSpeed = 15f;
            main.startSize = 0.08f;
            main.startColor = Color.yellow;
            main.gravityModifier = 1.5f; // Pull pieces down naturally
            
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) }); // Burst of 40 fragments
            
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            
            var renderer = particleGO.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));
            
            ps.Play();
            Destroy(particleGO, 2f); // Auto cleanup
        }

        private System.Collections.IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (gameObject.activeInHierarchy && spawner != null)
            {
                spawner.ReturnToPool(gameObject);
            }
        }
    }
}
