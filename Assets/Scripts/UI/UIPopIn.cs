using UnityEngine;

/// <summary>
/// Fades and scales a panel in every time it is enabled. Uses unscaled time so it works while paused.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPopIn : MonoBehaviour
{
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float delay = 0f;

    private CanvasGroup group;
    private float time;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        time = -delay;
        Apply(0f);
    }

    private void Update()
    {
        if (time >= duration) return;

        time += Time.unscaledDeltaTime;
        Apply(Mathf.Clamp01(time / duration));
    }

    private void Apply(float t)
    {
        // Ease out back: overshoots slightly before settling.
        float c = 1.7f;
        float eased = 1f + (c + 1f) * Mathf.Pow(t - 1f, 3f) + c * Mathf.Pow(t - 1f, 2f);

        group.alpha = t;
        transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
    }
}
