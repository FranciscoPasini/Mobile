using UnityEngine;
using Inputs = UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject KnobController;
    [SerializeField] private GameObject KnobBackController;

    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private float playerSpeed = 5f;

    private Vector2 joystickInput;

    private void OnEnable()
    {
        Inputs.EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        Inputs.EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        DetectTouch();
        MovePlayer();
    }

    private void DetectTouch()
    {
        if (Inputs.Touch.activeTouches.Count == 0)
        {
            joystickInput = Vector2.zero;

            // Return knob to the center
            KnobController.transform.position =
                KnobBackController.transform.position;

            return;
        }

        // Get the first active touch
        var touch = Inputs.Touch.activeTouches[0];

        Vector2 joystickCenter =
            KnobBackController.transform.position;

        Vector2 touchPosition = touch.screenPosition;

        // Direction from joystick center to finger
        Vector2 direction = touchPosition - joystickCenter;

        // Limit the knob to the joystick's radius
        direction = Vector2.ClampMagnitude(direction, joystickRadius);

        // Move the knob
        KnobController.transform.position =
            joystickCenter + direction;

        // Normalize to -1 to 1
        joystickInput = direction / joystickRadius;
    }

    private void MovePlayer()
    {
        Vector3 movement = new Vector3(
            joystickInput.x,
            0f,
            joystickInput.y
        );

        Player.transform.position +=
            movement * playerSpeed * Time.deltaTime;
    }
}
