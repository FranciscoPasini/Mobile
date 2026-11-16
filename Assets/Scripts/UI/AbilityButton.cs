using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Round ability button. A radial ring fills with the charge; when full it glows, throbs and can be pressed.
/// </summary>
public class AbilityButton : MonoBehaviour
{
    [SerializeField] private Player_ExperienceAndStats playerStats;
    [SerializeField] private Button button;
    [Tooltip("Radial filled image showing the charge.")]
    [SerializeField] private Image fill;
    [SerializeField] private Image icon;
    [SerializeField] private Image glow;
    [Tooltip("Scaled by the throb and the press punch.")]
    [SerializeField] private RectTransform body;

    [Header("Look")]
    [SerializeField] private Color chargingColor = new Color(0.45f, 0.75f, 1f, 1f);
    [SerializeField] private Color readyColor = new Color(1f, 0.8f, 0.25f, 1f);
    [SerializeField] private float fillSmoothing = 10f;
    [SerializeField] private float throbScale = 0.08f;
    [SerializeField] private float throbSpeed = 5f;
    [SerializeField] private float glowMaxAlpha = 0.9f;
    [SerializeField] private float punchScale = 0.25f;
    [SerializeField] private float punchDuration = 0.3f;

    private float targetFill;
    private float shownFill;
    private float readyTime;
    private float punchTime = float.MaxValue;

    private void Awake()
    {
        if (playerStats == null) playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        if (body == null) body = (RectTransform)transform;
        if (button != null) button.onClick.AddListener(OnPressed);
    }

    private void OnEnable()
    {
        if (playerStats == null) return;

        playerStats.onAbilityChargeChanged += HandleChargeChanged;
        targetFill = shownFill = playerStats.GetAbilityCharge();

        Base_OneTimePickup_Data pickup = playerStats.GetAbilityPickup();
        if (icon != null && pickup != null && pickup.pickupIcon != null) icon.sprite = pickup.pickupIcon;
    }

    private void OnDisable()
    {
        if (playerStats != null) playerStats.onAbilityChargeChanged -= HandleChargeChanged;
    }

    private void HandleChargeChanged(float charge)
    {
        if (charge >= 1f && targetFill < 1f) readyTime = 0f;
        targetFill = charge;
    }

    private void OnPressed()
    {
        if (playerStats != null && playerStats.UseAbility()) punchTime = 0f;
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bool ready = playerStats != null && playerStats.IsAbilityReady();

        // Snaps down on use, eases up while charging.
        shownFill = targetFill < shownFill ? targetFill : Mathf.Lerp(shownFill, targetFill, 1f - Mathf.Exp(-fillSmoothing * dt));
        if (fill != null)
        {
            fill.fillAmount = shownFill;
            fill.color = ready ? readyColor : chargingColor;
        }

        if (button != null) button.interactable = ready;

        float scale = 1f;
        float glowAlpha = 0f;
        if (ready)
        {
            readyTime += dt;
            float pulse = 0.5f + 0.5f * Mathf.Sin(readyTime * throbSpeed);
            scale += throbScale * pulse;
            glowAlpha = glowMaxAlpha * Mathf.Lerp(0.45f, 1f, pulse);
        }

        if (punchTime < punchDuration)
        {
            punchTime += dt;
            float t = Mathf.Clamp01(punchTime / punchDuration);
            scale += punchScale * Mathf.Sin(t * Mathf.PI) * (1f - t);
        }

        body.localScale = new Vector3(scale, scale, 1f);

        if (glow != null)
        {
            Color c = readyColor;
            c.a = glowAlpha;
            glow.color = c;
            glow.enabled = glowAlpha > 0.001f;
        }

        if (icon != null) icon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
    }
}
