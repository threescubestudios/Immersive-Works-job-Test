using System;
using UnityEngine;
using DG.Tweening;
using ImmersiWorks.Platform.Runtime;

namespace ImmersiWorks.Games.BusChase
{
    public sealed class CurupiraCharacter : MonoBehaviour
    {
        [Header("Procedural Visuals")]
        [SerializeField] private Material _skinMaterial;
        [SerializeField] private Material _hairMaterial;

        [SerializeField] private Transform _visualRoot;
        private Vector3 _baseLocalPosition;
        private float _runningTimer;
        private const float WallOffsetM = 0.2f;
        private static readonly Vector2 StartUV = new Vector2(0.10f, 0.34f);

        public void Initialize()
        {
            _baseLocalPosition = PlatformSpatial.WallUVToWorld(StartUV, WallOffsetM);
            transform.position = _baseLocalPosition;
        }

        // Mocking laugh gesture played when the bus is wrecked. Placeholder for a real laugh
        // animation: a gleeful bounce plus a shimmy so the beat reads even on the stand-in model.
        public void PlayTaunt()
        {
            if (_visualRoot == null) return;
            _visualRoot.DOComplete();
            _visualRoot.DOPunchScale(new Vector3(0.1f, 0.5f, 0.1f), 1.2f, 10, 0.4f);
            _visualRoot.DOPunchRotation(new Vector3(0f, 0f, 25f), 1.2f, 10, 0.5f);
        }

        public void PlaySummonTelegraph(int laneIndex)
        {
            float targetU = (laneIndex <= 1) ? 0.18f : 0.82f;
            Vector3 targetPos = PlatformSpatial.WallUVToWorld(new Vector2(targetU, StartUV.y), WallOffsetM);

            transform.DOMove(targetPos, 0.6f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                
                _visualRoot.DOPunchScale(Vector3.one * 0.3f, 0.5f, 8, 0.5f);
                _visualRoot.DOPunchRotation(new Vector3(0f, 0f, 20f), 0.5f, 8, 0.5f);
            });
        }

        [Header("Water Effects")]
        [SerializeField] private ParticleSystem _waterSplashPrefab;

        private void Update()
        {
            
            _runningTimer += Time.deltaTime * 10f;
            float bobValue = Mathf.Abs(Mathf.Sin(_runningTimer)) * 0.15f;
            _visualRoot.localPosition = new Vector3(0f, bobValue, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                if (_waterSplashPrefab != null)
                {
                    var splash = Instantiate(_waterSplashPrefab, transform.position, Quaternion.identity);
                    splash.Play();
                    Destroy(splash.gameObject, 3f);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Water"))
            {
                if (_waterSplashPrefab != null)
                {
                    var splash = Instantiate(_waterSplashPrefab, transform.position, Quaternion.identity);
                    splash.Play();
                    Destroy(splash.gameObject, 3f);
                }
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_visualRoot != null) _visualRoot.DOKill();
        }
    }
}
