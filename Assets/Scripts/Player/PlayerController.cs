using UnityEngine;
using Inputs = UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject KnobController;
    [SerializeField] private GameObject KnobBackController;

    [SerializeField] private float joystickRadius = 100f;
    [Tooltip("Speed before upgrades.")]
    [SerializeField] private float playerSpeed = 5f;
    [Tooltip("Source of the move speed upgrades. Found automatically if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;
    [Tooltip("Degrees per second the player turns toward the movement direction.")]
    [SerializeField] private float turnSpeed = 720f;
    [Tooltip("Joystick input below this doesn't rotate the player, so a resting thumb doesn't jitter the facing.")]
    [SerializeField] private float rotationDeadZone = 0.1f;


    private Vector2 initialKnobObjectPosition;

    private Vector2 joystickInput;
    private float currentPlayerSpeed;

    void Awake()
    {
        Inputs.EnhancedTouchSupport.Enable();

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        CalculatedPlayerSpeed();
    }

    private void OnEnable()
    {
        Inputs.Touch.onFingerDown += SaveLocation;
        Inputs.Touch.onFingerUp += DisableKnobBackController;

        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded += CalculatedPlayerSpeed;
        }
    }

    private void OnDisable()
    {
        Inputs.EnhancedTouchSupport.Disable();

        if (playerStats != null)
        {
            playerStats.onPlayerStatsUpgraded -= CalculatedPlayerSpeed;
        }
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

        // Si hay m�s de 2 toques, desactivar el joystick
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
            movement * currentPlayerSpeed * Time.deltaTime;

        RotatePlayer(movement);
    }

    private void RotatePlayer(Vector3 movement)
    {
        // Keeps the last facing when the joystick is released.
        if (movement.sqrMagnitude < rotationDeadZone * rotationDeadZone) return;

        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        Player.transform.rotation = Quaternion.RotateTowards(
            Player.transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    void SaveLocation(Inputs.Finger finger)
    {
        // Si ya hay m�s de 2 toques, no activamos el knob
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

    public void CalculatedPlayerSpeed()
    {
        float multiplier = playerStats != null ? playerStats.GetMoveSpeedMultiplier() : 1f;
        currentPlayerSpeed = playerSpeed * multiplier;
    }

    public float GetCurrentPlayerSpeed()
    {
        return currentPlayerSpeed;
    }
}
