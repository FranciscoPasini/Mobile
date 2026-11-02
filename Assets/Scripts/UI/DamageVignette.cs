using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Flashes a red vignette around the screen edges when the town is hit.
/// Stays faintly visible while the town health is low.
/// </summary>
[RequireComponent(typeof(Image))]
public class DamageVignette : MonoBehaviour
{
    [SerializeField] private TownManager townManager;
    [SerializeField] private Color color = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float hitAlpha = 0.6f;
    [SerializeField] private float fadeSpeed = 2.5f;
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float lowHealthAlpha = 0.25f;

    private Image image;
    private float alpha;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = false;
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
    }

    private void OnEnable() => townManager.OnTownDamageTaken += OnHit;
    private void OnDisable() => townManager.OnTownDamageTaken -= OnHit;

    private void OnHit(float damage) => alpha = hitAlpha;

    private void Update()
    {
        alpha = Mathf.MoveTowards(alpha, 0f, fadeSpeed * Time.unscaledDeltaTime);

        float floor = 0f;
        int max = townManager.GetTownMaxHealth();
        if (max > 0 && (float)townManager.GetTownHealth() / max <= lowHealthThreshold)
        {
            floor = lowHealthAlpha * (0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f));
        }

        image.color = new Color(color.r, color.g, color.b, Mathf.Max(alpha, floor));
    }
}
