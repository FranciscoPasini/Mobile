using UnityEngine;

/// <summary>
/// Gently bobs a UI element up and down, and optionally breathes its scale. For titles and idle menus.
/// </summary>
public class UIFloat : MonoBehaviour
{
    [SerializeField] private float height = 12f;
    [SerializeField] private float scaleAmount = 0.02f;
    [SerializeField] private float speed = 1.5f;

    private RectTransform rect;
    private Vector2 origin;

    private void Awake()
    {
        rect = (RectTransform)transform;
        origin = rect.anchoredPosition;
    }

    private void Update()
    {
        float wave = Mathf.Sin(Time.unscaledTime * speed);
        rect.anchoredPosition = origin + Vector2.up * wave * height;
        rect.localScale = Vector3.one * (1f + wave * scaleAmount);
    }
}
