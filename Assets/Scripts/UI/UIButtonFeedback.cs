using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Squashes a button while held and springs it back on release. Uses unscaled time so it works while paused.
/// </summary>
public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float speed = 18f;

    private Vector3 baseScale;
    private float target = 1f;
    private float current = 1f;
    private float velocity;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        target = current = 1f;
        velocity = 0f;
        transform.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData) => target = pressedScale;
    public void OnPointerUp(PointerEventData eventData) => target = 1f;
    public void OnPointerExit(PointerEventData eventData) => target = 1f;

    private void Update()
    {
        // Damped spring so the release overshoots a little.
        float dt = Time.unscaledDeltaTime;
        velocity += (target - current) * speed * speed * dt;
        velocity *= Mathf.Exp(-speed * 0.9f * dt);
        current += velocity * dt;
        transform.localScale = baseScale * current;
    }
}
