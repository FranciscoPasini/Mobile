using UnityEngine;
using UnityEngine.InputSystem;
using InputGyroscope = UnityEngine.InputSystem.Gyroscope;

public class GyroscpeController : PlayerController
{
    [Header("Gyroscope")]
    [SerializeField] private float smoothing = 5f;
    [Tooltip("Grados de inclinación que equivalen al joystick a fondo.")]
    [SerializeField] private float tiltForFullSpeed = 30f;
    [Tooltip("Inclinación mínima (grados) antes de mover al jugador. Evita el drift.")]
    [SerializeField] private float tiltDeadZone = 2f;
    [Tooltip("Invertir ejes si el movimiento se siente al revés.")]
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;

    private Quaternion smoothedAttitude;
    private Quaternion baseLineAttitude;
    private bool initialized;
    private bool needsCalibration = true;

    // Anulamos el input táctil: este controlador solo usa sensores.
    protected override void EnableTouchControls() { }
    protected override void DisableTouchControls() { }
    protected override void SubscribeTouchEvents() { }
    protected override void UnsubscribeTouchEvents() { }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (InputGyroscope.current != null)
        {
            InputSystem.EnableDevice(InputGyroscope.current);
        }

        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }

        // Al activarse este modo, la postura actual del teléfono pasa a ser el "centro".
        needsCalibration = true;
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (InputGyroscope.current != null)
        {
            InputSystem.DisableDevice(InputGyroscope.current);
        }

        if (AttitudeSensor.current != null)
        {
            InputSystem.DisableDevice(AttitudeSensor.current);
        }
    }

    protected override void Update()
    {
        MovePlayer(ReadGyroInput());
    }

    private Vector2 ReadGyroInput()
    {
        if (AttitudeSensor.current == null)
        {
            return Vector2.zero;
        }

        Quaternion currentAttitude = AttitudeSensor.current.attitude.ReadValue();

        if (!initialized)
        {
            smoothedAttitude = currentAttitude;
            initialized = true;
        }

        smoothedAttitude = Quaternion.Slerp(smoothedAttitude, currentAttitude, smoothing * Time.deltaTime);

        if (needsCalibration)
        {
            // La orientación actual se toma como el "centro" (celular pantalla arriba, nivelado)
            baseLineAttitude = smoothedAttitude;
            needsCalibration = false;
        }

        // Diferencia entre la orientación actual y la calibrada como "centro".
        Quaternion deltaRotation = smoothedAttitude * Quaternion.Inverse(baseLineAttitude);
        Vector3 euler = deltaRotation.eulerAngles;

        float pitch = NormalizeAngle(euler.x); // Rotación alrededor del eje X (inclinar adelante/atrás)
        float roll = NormalizeAngle(euler.z);  // Rotación alrededor del eje Z (inclinar izquierda/derecha)

        // MAPEO DE EJES PARA CELULAR PANTALLA ARRIBA:
        // - Hundir borde IZQUIERDO (levantar derecho) = ir a la IZQUIERDA → input.x negativo
        // - Hundir borde DERECHO (levantar izquierdo) = ir a la DERECHA → input.x positivo
        // - Hundir borde SUPERIOR/cámara (levantar inferior) = ir ADELANTE/ARRIBA → input.y positivo
        // - Hundir borde INFERIOR (levantar superior) = ir ATRÁS/ABAJO → input.y negativo

        Vector2 input = new Vector2(
            TiltToInput(-roll),   // Invertimos roll porque hundir izquierda genera roll positivo
            TiltToInput(-pitch)); // Invertimos pitch porque hundir arriba genera pitch negativo

        if (invertX) input.x = -input.x;
        if (invertY) input.y = -input.y;

        return input;
    }

    private float TiltToInput(float tiltDegrees)
    {
        if (Mathf.Abs(tiltDegrees) < tiltDeadZone)
        {
            return 0f;
        }

        return Mathf.Clamp(tiltDegrees / tiltForFullSpeed, -1f, 1f);
    }

    public void Calibrate()
    {
        needsCalibration = true;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}