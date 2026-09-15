using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Inputs = UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject KnobController;
    [SerializeField] private GameObject KnobBackController;
    [SerializeField] private Vector2 initialPosition;
    [SerializeField] private Vector2 currentPosition;

    private int TouchId = 0;


    private void OnEnable()
    {
        Inputs.EnhancedTouchSupport.Enable();
    }


    private void DetectTouch()
    {
        if (Inputs.Touch.activeTouches.Count > 0) 
        {
            int TouchId = Inputs.Touch.activeTouches[0].touchId;
            currentPosition = Inputs.Touch.activeTouches[TouchId].screenPosition;
            KnobController.transform.position = currentPosition;
        }

    }
}