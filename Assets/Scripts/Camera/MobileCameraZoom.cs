using Unity.Cinemachine;
using UnityEngine;
using Inputs = UnityEngine.InputSystem.EnhancedTouch;

public class MobileCameraZoom : MonoBehaviour
{
    [SerializeField] private CinemachineCamera vCam;
    [SerializeField] private float zoomSpeed = 0.05f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    // Dependiendo de cómo esté configurada tu cámara, usamos FOV, Orthographic o Distancia.
    public enum ZoomType { FieldOfView, OrthographicSize, CameraDistance }
    [SerializeField] private ZoomType zoomType = ZoomType.FieldOfView;

    private CinemachinePositionComposer positionComposer;

    private void Awake()
    {
        Inputs.EnhancedTouchSupport.Enable();

        if (vCam != null && zoomType == ZoomType.CameraDistance)
        {
            positionComposer = vCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        }
    }

    private void OnEnable()
    {
        Inputs.EnhancedTouchSupport.Enable();
    }

    private void Update()
    {
        if (Inputs.Touch.activeTouches.Count == 2)
        {
            var touchZero = Inputs.Touch.activeTouches[0];
            var touchOne = Inputs.Touch.activeTouches[1];

            Vector2 touchZeroPrevPos = touchZero.screenPosition - touchZero.delta;
            Vector2 touchOnePrevPos = touchOne.screenPosition - touchOne.delta;

            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.screenPosition - touchOne.screenPosition).magnitude;

            float difference = prevMagnitude - currentMagnitude;

            ApplyZoom(difference * zoomSpeed);
        }
    }

    private void ApplyZoom(float zoomAmount)
    {
        if (vCam == null) return;

        switch (zoomType)
        {
            case ZoomType.FieldOfView:
                vCam.Lens.FieldOfView += zoomAmount;
                vCam.Lens.FieldOfView = Mathf.Clamp(vCam.Lens.FieldOfView, minZoom, maxZoom);
                break;

            case ZoomType.OrthographicSize:
                vCam.Lens.OrthographicSize += zoomAmount;
                vCam.Lens.OrthographicSize = Mathf.Clamp(vCam.Lens.OrthographicSize, minZoom, maxZoom);
                break;

            case ZoomType.CameraDistance:
                if (positionComposer != null)
                {
                    positionComposer.CameraDistance += zoomAmount;
                    positionComposer.CameraDistance = Mathf.Clamp(positionComposer.CameraDistance, minZoom, maxZoom);
                }
                break;
        }
    }
}