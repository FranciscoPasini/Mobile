using UnityEngine;

/// <summary>
/// Switches between joystick and gyroscope movement using the saved control setting.
/// </summary>
public class PlayerInputSelector : MonoBehaviour
{
    [Header("Controladores")]
    [SerializeField] private PlayerController joystickController;
    [SerializeField] private GyroscpeController gyroscopeController;

    [Header("Joystick UI")]
    [SerializeField] private GameObject knob;
    [SerializeField] private GameObject knobBack;

    private void OnEnable()
    {
        GameSettings.Changed += ApplySetting;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplySetting;
    }

    private void Start()
    {
        ApplySetting();
    }

    // Llamar desde un botón de UI para usar el knob.
    public void UseJoystick() => GameSettings.UseGyro = false;

    // Llamar desde un botón de UI para usar el giroscopio.
    public void UseGyroscope() => GameSettings.UseGyro = true;

    // Llamar desde un botón de UI para alternar entre ambos.
    public void ToggleInputMode() => GameSettings.UseGyro = !GameSettings.UseGyro;

    private void ApplySetting()
    {
        bool useGyro = GameSettings.UseGyro;
        bool wasGyro = gyroscopeController != null && gyroscopeController.enabled;

        if (joystickController != null) joystickController.enabled = !useGyro;
        if (gyroscopeController != null) gyroscopeController.enabled = useGyro;

        if (useGyro && !wasGyro && gyroscopeController != null)
        {
            gyroscopeController.Calibrate();
        }
    }
}
