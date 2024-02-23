using UnityEngine;
using UnityEngine.InputSystem;

namespace Source.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private InputActionReference movement;

        [Header("Basic")]
        [SerializeField] private float movementSpeed = 8f;
        [SerializeField] private float movementSpeedSmoothTime = 0.03f;

        private Vector2 velocitySmoothing;

        private void Update()
        {
            // Input
            var movementInput = movement.action.ReadValue<Vector2>();

            // Modify
            var targetVelocity = Vector2.ClampMagnitude(movementInput, 1);
            targetVelocity *= movementSpeed;

            // Apply
            rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref velocitySmoothing, movementSpeedSmoothTime);
        }
    }
}
