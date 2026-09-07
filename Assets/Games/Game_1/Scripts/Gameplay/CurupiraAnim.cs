using UnityEngine;
using SaveCurupira.Core;

namespace SaveCurupira.Gameplay
{
    public class CurupiraAnim : MonoBehaviour
    {
        private Vector3 originalPosition;
        private bool isFreed = false;
        
        private float shakeIntensity = 0.08f;
        private float shakeSpeed = 30f;

        private void Start()
        {
            originalPosition = transform.position;
        }

        private void Update()
        {
            if (CurupiraGameController.Instance == null) return;
            
            if (CurupiraGameController.Instance.CurrentState == CurupiraGameController.GameState.Playing && !isFreed)
            {
                // Rapidly shake left and right to simulate struggling against the roots!
                float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
                transform.position = originalPosition + new Vector3(offsetX, 0, 0);
            }
            else if (CurupiraGameController.Instance.CurrentState == CurupiraGameController.GameState.End && !isFreed)
            {
                isFreed = true;
                
                // If the game ended and there is still time left, the player won!
                if (CurupiraGameController.Instance.GetTimeRemaining() > 0)
                {
                    // Jump celebration!
                    var rb = gameObject.GetComponent<Rigidbody>();
                    if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
                    
                    rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
                    rb.AddTorque(new Vector3(5f, 0, 5f), ForceMode.Impulse);
                }
                else
                {
                    // Lost. Slump in defeat.
                    transform.position = originalPosition;
                }
            }
        }
    }
}
