using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BuyZoneVisual : MonoBehaviour
{
    [Tooltip("Standalone pay zone. Found on this object if left empty.")]
    [SerializeField] private PurchaseArea purchaseArea;
    [Tooltip("Turret whose payment this zone shows. Used only when there is no PurchaseArea. Found in the parents if left empty.")]
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
        // Instanced copy so every zone fills independently.
        material = zoneRenderer.material;
        material.SetColor(FillColorId, fillColor);

        if (purchaseArea == null)
        {
            purchaseArea = GetComponent<PurchaseArea>() ?? GetComponentInParent<PurchaseArea>();
        }

        if (purchaseArea == null && turret == null)
        {
            turret = GetComponentInParent<Building_Turret>();
        }

        if (purchaseArea == null && turret == null)
        {
            Debug.LogError($"{name}: BuyZoneVisual has no PurchaseArea or Building_Turret to follow.", this);
        }
    }

    private void OnEnable()
    {
        if (purchaseArea != null)
        {
            purchaseArea.OnPurchased += HandleLevelCompleted;
            purchaseArea.OnUpgraded += HandleUpgraded;
            return;
        }

        if (turret != null)
        {
            turret.OnPurchased += HandleLevelCompleted;
            turret.OnUpgraded += HandleUpgraded;
        }
    }

    private void OnDisable()
    {
        if (purchaseArea != null)
        {
            purchaseArea.OnPurchased -= HandleLevelCompleted;
            purchaseArea.OnUpgraded -= HandleUpgraded;
        }

        if (turret != null)
        {
            turret.OnPurchased -= HandleLevelCompleted;
            turret.OnUpgraded -= HandleUpgraded;
        }
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
        if (purchaseArea == null && turret == null) return;

        if (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            ApplyFill(1f);
            return;
        }

        if (SourceIsMaxLevel())
        {
            zoneRenderer.enabled = false;
            enabled = false;
            return;
        }

        displayedFill = Mathf.MoveTowards(displayedFill, SourceProgress(), fillSpeed * Time.deltaTime);
        ApplyFill(displayedFill);
    }

    private bool SourceIsMaxLevel()
    {
        return purchaseArea != null ? purchaseArea.IsMaxLevel : turret.IsMaxLevel;
    }

    private float SourceProgress()
    {
        return purchaseArea != null ? purchaseArea.PaymentProgress : turret.PaymentProgress;
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
