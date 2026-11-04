using UnityEngine;

public class PlayerInputSelector : MonoBehaviour
{
    [Header("Controladores")]
    [SerializeField] private PlayerController joystickController;
    [SerializeField] private GyroscpeController gyroscopeController;
    
    [Header("Joystick UI")]
    [SerializeField] private GameObject knob;
    [SerializeField] private GameObject knobBack;
    
    [Header("Referencias de UI")]
    [Tooltip("Panel de settings que contiene los botones de input mode.")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("Referencia al PauseManager para resumir el juego.")]
    [SerializeField] private PauseManager pauseManager;
    
    [Header("Configuración")]
    [SerializeField] private bool startWithGyroscope;

    private void Start()
    {
        SetGyroscopeActive(startWithGyroscope);
    }

    // Llamar desde un botón de UI para usar el knob.
    public void UseJoystick()
    {
        SetGyroscopeActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseManager != null) pauseManager.Resume();
    }

    // Llamar desde un botón de UI para usar el giroscopio.
    public void UseGyroscope()
    {
        SetGyroscopeActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseManager != null) pauseManager.Resume();
    }

    // Llamar desde un botón de UI para alternar entre ambos.
    public void ToggleInputMode()
    {
        SetGyroscopeActive(!gyroscopeController.enabled);
    }

    private void SetGyroscopeActive(bool useGyro)
    {
        if (joystickController != null) joystickController.enabled = !useGyro;
        if (gyroscopeController != null) gyroscopeController.enabled = useGyro;

        if (useGyro && gyroscopeController != null)
        {
            gyroscopeController.Calibrate();
        }
    }
}