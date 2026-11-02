using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick scale bounce plus a colour flash on a graphic. Call Play() when a value changes.
/// </summary>
public class UIPunch : MonoBehaviour
{
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private Graphic tintTarget;

    private Vector3 baseScale;
    private Color baseColor;
    private Color flashColor;
    private float time = float.MaxValue;

    private void Awake()
    {
        baseScale = transform.localScale;
        if (tintTarget == null) tintTarget = GetComponent<Graphic>();
        if (tintTarget != null) baseColor = tintTarget.color;
    }

    public void Play() => Play(baseColor);

    public void Play(Color color)
    {
        flashColor = color;
        time = 0f;
    }

    private void Update()
    {
        if (time > duration) return;

        time += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(time / duration);
        float bump = Mathf.Sin(t * Mathf.PI) * (1f - t * 0.5f);

        transform.localScale = baseScale * (1f + (punchScale - 1f) * bump);
        if (tintTarget != null) tintTarget.color = Color.Lerp(baseColor, flashColor, 1f - t);
    }
}
