using TMPro;
using UnityEngine;

/// <summary>
/// Shows the name of the building whose buy zone the player is standing in.
/// Red while not owned, white once owned, and it pops and flashes on buy / upgrade.
/// </summary>
public class BuildingNamePopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI nameText;
    [Tooltip("Optional second line: BUY / LEVEL 2 - UPGRADE / UPGRADED!")]
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Colours")]
    [SerializeField] private Color notOwnedColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color ownedColor = Color.white;
    [SerializeField] private Color flashColor = new Color(1f, 0.85f, 0.25f);

    [Header("Feel")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float celebrateDuration = 0.9f;
    [SerializeField] private float popScale = 1.45f;
    [SerializeField] private float letterSpread = 25f;

    private PurchaseArea area;
    private bool visible;
    private float showTime;
    private float celebrateTime = float.MaxValue;
    private Vector3 baseScale;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        baseScale = transform.localScale;
        if (group != null) group.alpha = 0f;
    }

    private void OnEnable()
    {
        PurchaseArea.OnPlayerEntered += HandleEntered;
        PurchaseArea.OnPlayerExited += HandleExited;
    }

    private void OnDisable()
    {
        PurchaseArea.OnPlayerEntered -= HandleEntered;
        PurchaseArea.OnPlayerExited -= HandleExited;
        Bind(null);
    }

    private void HandleEntered(PurchaseArea entered)
    {
        Bind(entered);
        visible = true;
        showTime = 0f;
        celebrateTime = float.MaxValue;
        RefreshTexts();
    }

    private void HandleExited(PurchaseArea exited)
    {
        if (exited != area) return;
        Bind(null);
        visible = false;
    }

    private void Bind(PurchaseArea newArea)
    {
        if (area != null)
        {
            area.OnPurchased -= HandlePurchased;
            area.OnUpgraded -= HandleUpgraded;
        }

        area = newArea;

        if (area != null)
        {
            area.OnPurchased += HandlePurchased;
            area.OnUpgraded += HandleUpgraded;
        }
    }

    private void HandlePurchased() => Celebrate("PURCHASED!");
    private void HandleUpgraded(int level) => Celebrate($"UPGRADED TO LEVEL {level}!");

    private void Celebrate(string message)
    {
        celebrateTime = 0f;
        if (infoText != null) infoText.text = message;
    }

    private void RefreshTexts()
    {
        if (area == null) return;

        nameText.text = area.BuildingName.ToUpperInvariant();
        if (infoText != null) infoText.text = area.IsPurchased ? $"LEVEL {area.Level}  -  UPGRADE" : "BUY";
    }

    private void Update()
    {
        if (group == null || nameText == null) return;

        float dt = Time.unscaledDeltaTime;
        bool celebrating = celebrateTime < celebrateDuration;

        // Keep showing until the celebration ends, even if the zone closed (max level).
        float targetAlpha = visible || celebrating ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * dt);

        showTime += dt;
        float scale = Mathf.Lerp(0.8f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(showTime / 0.2f), 3f));
        Color color = area != null && !area.IsPurchased ? notOwnedColor : ownedColor;
        float spacing = 0f;

        if (celebrating)
        {
            celebrateTime += dt;
            float t = Mathf.Clamp01(celebrateTime / celebrateDuration);
            float bump = Mathf.Sin(Mathf.Min(t * 3f, 1f) * Mathf.PI) * (1f - t);

            scale *= 1f + (popScale - 1f) * bump;
            spacing = letterSpread * bump;
            color = Color.Lerp(flashColor, ownedColor, t);
            // Strobe between gold and white for the first part of the celebration.
            if (t < 0.4f && Mathf.Repeat(celebrateTime * 12f, 1f) > 0.5f) color = Color.white;

            if (celebrateTime >= celebrateDuration && visible) RefreshTexts();
        }

        transform.localScale = baseScale * scale;
        nameText.color = color;
        nameText.characterSpacing = spacing;
    }
}
