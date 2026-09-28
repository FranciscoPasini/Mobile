using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BuyZoneVisual : MonoBehaviour
{
    [Tooltip("Turret whose payment this zone shows. Found in the parents if left empty.")]
    [SerializeField] private Building_Turret turret;
    [SerializeField] private Color fillColor = Color.green;
    [Tooltip("How fast the fill catches up to the real payment, in full circles per second.")]
    [SerializeField] private float fillSpeed = 3f;
    [Tooltip("How long the circle stays full after a purchase before emptying for the next upgrade.")]
    [SerializeField] private float completeHoldTime = 0.4f;

    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int FillColorId = Shader.PropertyToID("_FillColor");

    private Renderer zoneRenderer;
    private Material material;
    private float displayedFill;
    private float appliedFill = -1f;
    private float holdTimer;

    private void Awake()
    {
        zoneRenderer = GetComponent<Renderer>();
        // Instanced copy so every turret's zone fills independently.
        material = zoneRenderer.material;
        material.SetColor(FillColorId, fillColor);

        if (turret == null)
        {
            turret = GetComponentInParent<Building_Turret>();
        }

        if (turret == null)
        {
            Debug.LogError($"{name}: BuyZoneVisual has no Building_Turret to follow.", this);
        }
    }

    private void OnEnable()
    {
        if (turret == null) return;
        turret.OnPurchased += HandleLevelCompleted;
        turret.OnUpgraded += HandleUpgraded;
    }

    private void OnDisable()
    {
        if (turret == null) return;
        turret.OnPurchased -= HandleLevelCompleted;
        turret.OnUpgraded -= HandleUpgraded;
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    private void Update()
    {
        if (turret == null) return;

        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            ApplyFill(1f);
            return;
        }

        if (turret.IsMaxLevel)
        {
            zoneRenderer.enabled = false;
            enabled = false;
            return;
        }

        // The turret's progress resets to 0 on purchase, so this also drains the circle back to empty.
        displayedFill = Mathf.MoveTowards(displayedFill, turret.PaymentProgress, fillSpeed * Time.deltaTime);
        ApplyFill(displayedFill);
    }

    private void HandleUpgraded(int level)
    {
        HandleLevelCompleted();
    }

    private void HandleLevelCompleted()
    {
        displayedFill = 1f;
        holdTimer = completeHoldTime;
        ApplyFill(1f);
    }

    private void ApplyFill(float fill)
    {
        if (Mathf.Approximately(fill, appliedFill)) return;

        appliedFill = fill;
        material.SetFloat(FillId, fill);
    }
}
