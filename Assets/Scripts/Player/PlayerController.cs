using UnityEngine;
using Inputs = UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject KnobController;
    [SerializeField] private GameObject KnobBackController;

    [SerializeField] private float joystickRadius = 100f;
    [SerializeField] private float playerSpeed = 5f;

    private Vector2 initialKnobObjectPosition;

    private Vector2 joystickInput;

    void Awake()
    {
        Inputs.EnhancedTouchSupport.Enable();
    }

    private void OnEnable()
    {
        Inputs.Touch.onFingerDown += SaveLocation;
        Inputs.Touch.onFingerUp += DisableKnobBackController;
        
    }

    private void OnDisable()
    {
        Inputs.EnhancedTouchSupport.Disable();
    }


    void Start()
    {
        KnobBackController.SetActive(false);
    }
    private void Update()
    {
        DetectTouch();
        MovePlayer();
    }

    private void DetectTouch()
    {
        int touchCount = Inputs.Touch.activeTouches.Count;

        if (touchCount == 0)
        {
            joystickInput = Vector2.zero;

            // Return knob to the center
            KnobController.transform.position =
                KnobBackController.transform.position;

            return;
        }

        // Si hay más de 2 toques, desactivar el joystick
        if (touchCount >= 2)
        {
            joystickInput = Vector2.zero;
            KnobController.transform.position = KnobBackController.transform.position;
            KnobBackController.SetActive(false);
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

    void SaveLocation(Inputs.Finger finger)
    {
        // Si ya hay más de 2 toques, no activamos el knob
        if (Inputs.Touch.activeTouches.Count >= 2)
            return;

        initialKnobObjectPosition = finger.screenPosition;
        KnobBackController.transform.position = initialKnobObjectPosition;
        KnobBackController.SetActive(true);
    }

    void DisableKnobBackController(Inputs.Finger finger)
    {
        KnobBackController.SetActive(false);
    }
}
