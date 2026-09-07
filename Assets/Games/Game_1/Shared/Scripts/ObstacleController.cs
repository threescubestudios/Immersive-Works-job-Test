using System;
using UnityEngine;
using DG.Tweening;
using ImmersiWorks.Platform.Runtime;

namespace ImmersiWorks.Games.BusChase
{
    // Tree/Root/Branch/Vine are the original set. Rock/Fruit/Bush are the obstacles from the
    // vertical-slice spec, each with its own motion (see UpdateVisuals). New values are appended,
    // never reordered, so existing serialized prefabs/pools that store the enum keep their meaning.
    public enum ObstacleType { Tree, Root, Branch, Vine, Rock, Fruit, Bush }

    public sealed class ObstacleController : MonoBehaviour
    {
        [Header("Lane Mapping")]
        public float[] LaneUVx = { 0.30f, 0.50f, 0.70f };

        [Header("Wall -> Floor Handoff")]
        [Tooltip("Fraction of the journey spent approaching on the wall before handing off to the floor. " +
                 "0.5 splits the journey evenly across both surfaces, per the wall+floor combined design.")]
        [Range(0.05f, 0.95f)]
        [SerializeField] private float _seamProgress = 0.5f;

        [Tooltip("Floor UV.y where the obstacle arrives at the player (entry side is 0, wall side is 1).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _floorArrivalY = 0.08f;

        [Header("Per-Type Motion (placeholder-friendly; tune live)")]
        [Tooltip("Rock: degrees/sec it rolls forward as it comes at the bus.")]
        [SerializeField] private float _rockRollSpeed = 320f;
        [Tooltip("Fruit: peak height (world metres) of the throwing arc toward the windshield.")]
        [SerializeField] private float _fruitArcHeight = 0.9f;
        [Tooltip("Fruit: degrees/sec it spins while airborne.")]
        [SerializeField] private float _fruitSpinSpeed = 520f;
        [Tooltip("Bush: degrees/sec it tumbles while flying in from the side.")]
        [SerializeField] private float _bushTumbleSpeed = 240f;
        [Tooltip("Tree: how much it starts squashed into the ground and rises up (0 = no emerge, 1 = full).")]
        [Range(0f, 1f)]
        [SerializeField] private float _treeEmergeAmount = 0.85f;

        private ObstacleType _type;
        private int _laneIndex;
        private float _travelDuration;
        private float _progress;
        private bool _isMoving;
        private Action<ObstacleController> _onComplete;

        [SerializeField] private Transform _visualRoot;
        private float _vineSwingTimer;
        private float _motionTimer;

        public ObstacleType Type => _type;
        public int LaneIndex => _laneIndex;
        public float Progress => _progress;

        public void Initialize(ObstacleType type, int lane, float travelDuration, Action<ObstacleController> onComplete)
        {
            _type = type;
            _laneIndex = lane;
            _travelDuration = travelDuration;
            _onComplete = onComplete;
            _progress = 0f;
            _isMoving = true;
            _motionTimer = 0f;
            _vineSwingTimer = 0f;

            if (_visualRoot != null)
            {
                _visualRoot.DOKill();
                _visualRoot.localRotation = Quaternion.identity;
            }

            // Update starting position and scale
            UpdateVisuals(0f);
        }

        private void Update()
        {
            if (!_isMoving) return;

            _progress += Time.deltaTime / _travelDuration;
            if (_progress >= 1.0f)
            {
                _progress = 1.0f;
                _isMoving = false;
                _onComplete?.Invoke(this);
            }

            UpdateVisuals(_progress);
        }

        private void UpdateVisuals(float progress)
        {
            _motionTimer += Time.deltaTime;

            float targetX = LaneUVx[_laneIndex];
            // Rocks and bushes enter from a side edge rather than the vanishing point: pick the edge
            // nearest their target lane (left lane comes from the left, right lane from the right,
            // centre lane from whichever side is arbitrary but consistent).
            float entryX = targetX <= 0.5f ? 0.03f : 0.97f;
            bool sideEntry = _type == ObstacleType.Rock || _type == ObstacleType.Bush;
            float currentScale;

            if (progress < _seamProgress)
            {
                // Approach on the wall: fake-perspective trick (shrink toward the vanishing point,
                // grow toward camera), rescaled to finish at the seam instead of the whole journey.
                float t = _seamProgress > 0f ? progress / _seamProgress : 1f;
                float startX = sideEntry ? entryX : 0.5f;
                float currentX = Mathf.Lerp(startX, targetX, t);
                float currentY = Mathf.Lerp(0.6f, 0.15f, t);

                Vector2 currentUV = new Vector2(currentX, currentY);
                transform.position = PlatformSpatial.WallUVToWorld(currentUV, Mathf.Lerp(0f, 1.2f, t));

                currentScale = Mathf.Lerp(0.05f, 1.0f, t);
            }
            else
            {
                // Hand off to the floor: the wall's bottom-centre point (UV.y = 0.15 side) lines up
                // with the floor's far/wall-side edge (UV.y = 1), so the obstacle continues sliding
                // across the floor toward the player instead of stopping at the wall base.
                float t = Mathf.InverseLerp(_seamProgress, 1f, progress);
                float startX = sideEntry ? Mathf.Lerp(entryX, targetX, 0.5f) : targetX;
                float currentX = Mathf.Lerp(startX, targetX, Mathf.Clamp01(t * 2f));
                float currentY = Mathf.Lerp(1f, _floorArrivalY, t);

                Vector2 currentUV = new Vector2(currentX, currentY);
                transform.position = PlatformSpatial.FloorUVToWorld(currentUV);

                currentScale = 1.0f;
            }

            ApplyTypeMotion(progress, currentScale);
        }

        // Each obstacle type gets its own flourish layered on top of the shared approach path.
        // Kept deliberately simple and driven by tunable fields so it reads correctly even on
        // placeholder art, before the final models/animations arrive.
        private void ApplyTypeMotion(float progress, float baseScale)
        {
            Vector3 scaleVec = Vector3.one * baseScale;
            Quaternion visualRot = Quaternion.identity;

            switch (_type)
            {
                case ObstacleType.Tree:
                    // Emerge from the ground: start squashed flat and rise to full height as it nears.
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.6f));
                    float yScale = Mathf.Lerp(1f - _treeEmergeAmount, 1f, rise);
                    scaleVec = new Vector3(baseScale, baseScale * yScale, baseScale);
                    break;

                case ObstacleType.Rock:
                    // Roll forward continuously as it barrels toward the bus.
                    visualRot = Quaternion.Euler(_motionTimer * _rockRollSpeed, 0f, 0f);
                    break;

                case ObstacleType.Fruit:
                    // Thrown at the windshield: parabolic hop plus a fast spin.
                    float arc = Mathf.Sin(progress * Mathf.PI) * _fruitArcHeight;
                    transform.position += Vector3.up * arc;
                    visualRot = Quaternion.Euler(_motionTimer * _fruitSpinSpeed,
                                                 _motionTimer * _fruitSpinSpeed * 0.6f, 0f);
                    break;

                case ObstacleType.Bush:
                    // Fly in from the side, tumbling end over end.
                    visualRot = Quaternion.Euler(_motionTimer * _bushTumbleSpeed,
                                                 0f, _motionTimer * _bushTumbleSpeed * 0.4f);
                    break;

                case ObstacleType.Vine:
                    _vineSwingTimer += Time.deltaTime * 5f;
                    float swingOffset = Mathf.Sin(_vineSwingTimer) * 0.4f * (1f - progress);
                    visualRot = Quaternion.Euler(0f, 0f, swingOffset * 50f);
                    break;
            }

            if (_visualRoot != null)
            {
                _visualRoot.localScale = scaleVec;
                _visualRoot.localRotation = visualRot;
            }
        }

        // Played the moment the obstacle reaches the bus, before it is released to the pool. A hit
        // squashes it flat against the windshield (a splat), a dodge whooshes it away shrinking. The
        // caller passes a callback so it releases the obstacle only after the flourish finishes.
        // This is the DOTween stand-in for per-obstacle impact animations until authored ones exist.
        public void PlayArrivalImpact(bool wasHit, Action onDone)
        {
            _isMoving = false;

            if (_visualRoot == null)
            {
                onDone?.Invoke();
                return;
            }

            _visualRoot.DOKill();
            Vector3 baseScale = _visualRoot.localScale;

            if (wasHit)
            {
                DOTween.Sequence().SetLink(gameObject)
                    .Append(_visualRoot.DOScale(new Vector3(baseScale.x * 1.45f, baseScale.y * 0.4f,
                                                            baseScale.z * 1.45f), 0.12f).SetEase(Ease.OutQuad))
                    .Append(_visualRoot.DOScale(Vector3.zero, 0.16f).SetEase(Ease.InBack))
                    .OnComplete(() => onDone?.Invoke());
            }
            else
            {
                _visualRoot.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InQuad).SetLink(gameObject)
                    .OnComplete(() => onDone?.Invoke());
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (_visualRoot != null) _visualRoot.DOKill();
        }
    }
}
