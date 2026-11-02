using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Pops the name of a picked-up weapon or one-time pickup on screen, with rainbow letters,
/// then floats up and fades out. Pickups with an empty pickupName are skipped.
/// </summary>
public class WeaponPickupPopup : MonoBehaviour
{
    [SerializeField] private PlayerWeaponSystem weaponSystem;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI text;

    [Header("Feel")]
    [SerializeField] private float popTime = 0.25f;
    [SerializeField] private float holdTime = 1.2f;
    [SerializeField] private float fadeTime = 0.5f;
    [SerializeField] private float floatDistance = 60f;
    [Tooltip("How fast the rainbow scrolls.")]
    [SerializeField] private float rainbowSpeed = 0.8f;
    [Tooltip("Hue difference between neighbouring letters.")]
    [SerializeField] private float rainbowSpread = 0.08f;

    private readonly StringBuilder builder = new StringBuilder();
    private RectTransform rect;
    private Vector2 origin;
    private string weaponName = "";
    private float time = float.MaxValue;

    private void Awake()
    {
        rect = (RectTransform)transform;
        origin = rect.anchoredPosition;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (weaponSystem == null) weaponSystem = FindFirstObjectByType<PlayerWeaponSystem>();
        group.alpha = 0f;
    }

    private void OnEnable()
    {
        if (weaponSystem != null) weaponSystem.OnWeaponPickedUp += ShowWeapon;
        Base_OneTimePickup.OnCollected += ShowPickup;
    }

    private void OnDisable()
    {
        if (weaponSystem != null) weaponSystem.OnWeaponPickedUp -= ShowWeapon;
        Base_OneTimePickup.OnCollected -= ShowPickup;
    }

    private void ShowWeapon(Base_Weapon weapon)
    {
        Show(string.IsNullOrWhiteSpace(weapon.WeaponName) ? "NEW WEAPON" : weapon.WeaponName);
    }

    private void ShowPickup(Base_OneTimePickup pickup)
    {
        string pickupName = pickup.PickupData != null ? pickup.PickupData.pickupName : null;
        if (!string.IsNullOrWhiteSpace(pickupName)) Show(pickupName);
    }

    private void Show(string displayName)
    {
        weaponName = displayName.ToUpperInvariant();
        time = 0f;
    }

    private void Update()
    {
        if (time > popTime + holdTime + fadeTime) return;

        time += Time.deltaTime;

        float scale;
        float alpha;
        float rise;
        if (time < popTime)
        {
            float t = time / popTime;
            alpha = t;
            // Overshoot then settle.
            scale = Mathf.LerpUnclamped(0.3f, 1f, 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f));
            rise = 0f;
        }
        else
        {
            float fade = Mathf.Clamp01((time - popTime - holdTime) / fadeTime);
            alpha = 1f - fade;
            scale = 1f;
            rise = fade * floatDistance;
        }

        group.alpha = alpha;
        rect.localScale = Vector3.one * scale;
        rect.anchoredPosition = origin + Vector2.up * rise;
        text.text = Rainbow(weaponName);
    }

    private string Rainbow(string value)
    {
        builder.Clear();
        float baseHue = Time.time * rainbowSpeed;
        for (int i = 0; i < value.Length; i++)
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(baseHue + i * rainbowSpread, 1f), 0.75f, 1f);
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>').Append(value[i]);
        }
        return builder.ToString();
    }
}
