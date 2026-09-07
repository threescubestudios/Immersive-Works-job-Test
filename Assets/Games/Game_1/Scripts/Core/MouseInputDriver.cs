// MouseInputDriver -- turns the mouse into the same input events the real sensors produce.
//
// On the installation a depth camera watches people in front of a curved wall and raises
// TouchStarted, TouchReleased and Swipe. You have no camera, so this raises exactly the same
// events from the mouse instead.
//
// The point is that it goes through the same events rather than around them: game code written
// against these works unchanged when a real sensor is attached. Nothing needs rewriting later.
//
// Drop this on any object in the scene. Drag to swipe.

using UnityEngine;
using ImmersiWorks.Platform.Core;

namespace SaveCurupira.Core
{
    [AddComponentMenu("Save Curupira/Mouse Input Driver")]
    public sealed class MouseInputDriver : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Which player these events are attributed to. The real room has four.")]
        [SerializeField] private int _playerId = 0;

        [Header("Swipe detection")]
        [Tooltip("How far the mouse must travel, as a fraction of the screen, to count as a swipe.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _minSwipeDistance01 = 0.08f;

        [Tooltip("Draw the current drag in the Scene view.")]
        [SerializeField] private bool _drawDebug = true;

        private Vector2 _pressUV;
        private float   _pressTime;
        private bool    _dragging;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _pressUV   = ScreenToUV(Input.mousePosition);
                _pressTime = Time.time;
                _dragging  = true;
                EventBus.Post(new TouchStarted(_playerId, WallId.North, _pressUV));
            }
            else if (Input.GetMouseButtonUp(0) && _dragging)
            {
                _dragging = false;
                Vector2 releaseUV = ScreenToUV(Input.mousePosition);
                EventBus.Post(new TouchReleased(_playerId, WallId.North, releaseUV));

                Vector2 delta = releaseUV - _pressUV;
                if (delta.magnitude >= _minSwipeDistance01)
                {
                    float seconds = Mathf.Max(0.0001f, Time.time - _pressTime);
                    EventBus.Post(new Swipe(
                        _playerId,
                        ResolveDirection(delta),
                        isRightHand: true,
                        speedUVs: delta.magnitude / seconds));
                }
            }
        }

        /// <summary>
        /// Screen position to a 0-1 position on the wall. The sensors report the same range, so a
        /// game reading UV works the same whether the input came from a mouse or a person.
        /// </summary>
        private static Vector2 ScreenToUV(Vector3 screenPosition)
        {
            return new Vector2(
                Screen.width  > 0 ? Mathf.Clamp01(screenPosition.x / Screen.width)  : 0f,
                Screen.height > 0 ? Mathf.Clamp01(screenPosition.y / Screen.height) : 0f);
        }

        private static SwipeDirection ResolveDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x >= 0f ? SwipeDirection.Right : SwipeDirection.Left;

            return delta.y >= 0f ? SwipeDirection.Up : SwipeDirection.Down;
        }

        private void OnDrawGizmos()
        {
            if (!_drawDebug || !_dragging || !Application.isPlaying)
                return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                ImmersiWorks.Platform.Runtime.PlatformSpatial.WallUVToWorld(_pressUV),
                ImmersiWorks.Platform.Runtime.PlatformSpatial.WallUVToWorld(ScreenToUV(Input.mousePosition)));
        }
    }
}
