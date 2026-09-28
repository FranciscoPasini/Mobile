
using UnityEngine;
using UnityEngine.InputSystem;
using InputGyroscope = UnityEngine.InputSystem.Gyroscope;

public class GyroscpeController : MonoBehaviour

{
    [SerializeField] private float smoothing;
    [SerializeField] private PlayerController playerController;

    private Quaternion currentAttitude;
    private Quaternion smoothedAttitude;
    private Quaternion baseLineAttitude;

    private bool initialized;
    private bool calibrated;

    private void OnEnable()

    {
        if (InputGyroscope.current != null)
        {
            InputSystem.EnableDevice(InputGyroscope.current);
        }

        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }
    }

    private void Start()
    {
        Calibrate();
    }

    private void Update()
    {
        AngularVelocity();
        UpdateOrientation();
    }

    private void AngularVelocity()
    {
        if (InputGyroscope.current == null)
        {
            Debug.Log("No hay un Gyroscope");
            return;
        }

        Vector3 angularVelocity = InputGyroscope.current.angularVelocity.ReadDefaultValue();
        Vector3 degreesPerSecond = angularVelocity * Mathf.Rad2Deg;
    }

    private void UpdateOrientation()
    {
        if (AttitudeSensor.current == null)
        {
            Debug.Log("Sensores de orientación");
            return;
        }

        currentAttitude = AttitudeSensor.current.attitude.ReadValue();

        if (!initialized)
        {
            smoothedAttitude = currentAttitude;
            initialized = true;
        }

        smoothedAttitude = Quaternion.Slerp(smoothedAttitude, currentAttitude, smoothing * Time.deltaTime);
        Vector3 euler = smoothedAttitude.eulerAngles;

        euler.x = NormalizeAngle(euler.x);
        euler.y = NormalizeAngle(euler.y);
        euler.z = NormalizeAngle(euler.z);
    }

    public void Calibrate()
    {
        if (!initialized)
        {
            return;
        }

        baseLineAttitude = smoothedAttitude;
        calibrated = true;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}