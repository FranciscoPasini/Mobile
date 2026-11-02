using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Town health bar with hit feedback: smooth fill, a trailing "damage chip", a red flash, a shake,
/// and a pulsing glow when health is low.
/// </summary>
public class TownHealthBar : MonoBehaviour
{
    [SerializeField] private TownManager townManager;

    [Header("Parts")]
    [SerializeField] private Image fill;
    [Tooltip("Sits behind the fill and drains slowly, showing how much was just lost.")]
    [SerializeField] private Image chip;
    [Tooltip("Full-size overlay that flashes on hit.")]
    [SerializeField] private Image flash;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private RectTransform shakeTarget;

    [Header("Colours")]
    [SerializeField] private Color highColor = new Color(0.3f, 0.85f, 0.4f);
    [SerializeField] private Color midColor = new Color(1f, 0.78f, 0.24f);
    [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.15f, 0.85f);

    [Header("Feel")]
    [SerializeField] private float fillSpeed = 6f;
    [SerializeField] private float chipDelay = 0.4f;
    [SerializeField] private float chipSpeed = 1.5f;
    [SerializeField] private float flashDuration = 0.35f;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float shakeDuration = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float lowHealthThreshold = 0.3f;

    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private float targetPercent = 1f;
    private float shownPercent = 1f;
    private float chipPercent = 1f;
    private float chipTimer;
    private float flashTimer = float.MaxValue;
    private float shakeTimer = float.MaxValue;
    private Vector2 shakeOrigin;

    private void Awake()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (shakeTarget != null) shakeOrigin = shakeTarget.anchoredPosition;
    }

    private void OnEnable()
    {
        townManager.OnTownHealthChanged += Refresh;
        townManager.OnTownDamageTaken += OnHit;
    }

    private void OnDisable()
    {
        townManager.OnTownHealthChanged -= Refresh;
        townManager.OnTownDamageTaken -= OnHit;
    }

    private void Start()
    {
        Refresh();
        shownPercent = chipPercent = targetPercent;
        if (flash != null) flash.color = Color.clear;
    }

    private void Refresh()
    {
        lastHealth = townManager.GetTownHealth();
        lastMaxHealth = townManager.GetTownMaxHealth();

        int max = lastMaxHealth;
        int current = Mathf.Max(0, lastHealth);
        targetPercent = max > 0 ? (float)current / max : 0f;

        // Healing: the chip should not lag behind, snap it up.
        if (targetPercent > chipPercent) chipPercent = targetPercent;

        if (label != null) label.text = $"{current} / {max}";
    }

    private void OnHit(float damage)
    {
        chipTimer = chipDelay;
        flashTimer = 0f;
        shakeTimer = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Max health can change without an event (upgrades), so check every frame.
        if (townManager.GetTownHealth() != lastHealth || townManager.GetTownMaxHealth() != lastMaxHealth) Refresh();

        shownPercent = Mathf.Lerp(shownPercent, targetPercent, 1f - Mathf.Exp(-fillSpeed * dt));

        chipTimer -= dt;
        if (chipTimer <= 0f) chipPercent = Mathf.MoveTowards(chipPercent, targetPercent, chipSpeed * dt);

        bool low = targetPercent <= lowHealthThreshold;
        Color barColor = targetPercent > 0.6f ? highColor : targetPercent > lowHealthThreshold ? midColor : lowColor;
        if (low) barColor = Color.Lerp(barColor, Color.white, (Mathf.Sin(Time.unscaledTime * 8f) + 1f) * 0.15f);

        if (fill != null)
        {
            fill.fillAmount = shownPercent;
            fill.color = barColor;
        }
        if (chip != null) chip.fillAmount = chipPercent;

        UpdateFlash(dt);
        UpdateShake(dt);
    }

    private void UpdateFlash(float dt)
    {
        if (flash == null || flashTimer > flashDuration) return;

        flashTimer += dt;
        float t = Mathf.Clamp01(flashTimer / flashDuration);
        flash.color = Color.Lerp(flashColor, new Color(flashColor.r, flashColor.g, flashColor.b, 0f), t);
    }

    private void UpdateShake(float dt)
    {
        if (shakeTarget == null || shakeTimer > shakeDuration) return;

        shakeTimer += dt;
        float strength = shakeStrength * (1f - Mathf.Clamp01(shakeTimer / shakeDuration));
        shakeTarget.anchoredPosition = shakeTimer >= shakeDuration
            ? shakeOrigin
            : shakeOrigin + Random.insideUnitCircle * strength;
    }
}
