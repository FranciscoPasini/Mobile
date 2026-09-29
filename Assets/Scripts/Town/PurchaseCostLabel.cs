using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// World-space remaining-cost number that faces the camera.
/// Works with a PurchaseArea or a turret buy zone, same lookup as BuyZoneVisual.
/// </summary>
public class PurchaseCostLabel : MonoBehaviour
{
    [Tooltip("Standalone pay zone. Found on this object if left empty.")]
    [SerializeField] private PurchaseArea purchaseArea;
    [Tooltip("Turret whose payment this label shows. Used only when there is no PurchaseArea. Found in the parents if left empty.")]
    [SerializeField] private Building_Turret turret;
    [SerializeField] private TextMeshPro label;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);
    [SerializeField] private float fontSize = 2.6f;
    [SerializeField] private Color textColor = Color.white;

    private Transform cam;
    private int lastShown = int.MinValue;

    private void Awake()
    {
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
            Debug.LogError($"{name}: PurchaseCostLabel has no PurchaseArea or Building_Turret to follow.", this);
            enabled = false;
            return;
        }

        if (label == null)
        {
            CreateLabel();
        }

        CacheCamera();
        RefreshAmount(true);
    }

    private void OnEnable()
    {
        if (purchaseArea != null)
        {
            purchaseArea.OnPaymentProgress += HandleProgress;
            purchaseArea.OnPurchased += HandleLevelCompleted;
            purchaseArea.OnUpgraded += HandleUpgraded;
            return;
        }

        if (turret != null)
        {
            turret.OnPaymentProgress += HandleProgress;
            turret.OnPurchased += HandleLevelCompleted;
            turret.OnUpgraded += HandleUpgraded;
        }
    }

    private void OnDisable()
    {
        if (purchaseArea != null)
        {
            purchaseArea.OnPaymentProgress -= HandleProgress;
            purchaseArea.OnPurchased -= HandleLevelCompleted;
            purchaseArea.OnUpgraded -= HandleUpgraded;
        }

        if (turret != null)
        {
            turret.OnPaymentProgress -= HandleProgress;
            turret.OnPurchased -= HandleLevelCompleted;
            turret.OnUpgraded -= HandleUpgraded;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            CacheCamera();
            if (cam == null) return;
        }

        if (SourceIsMaxLevel())
        {
            SetLabelVisible(false);
            enabled = false;
            return;
        }

        Transform labelTransform = label.transform;
        labelTransform.position = transform.position + worldOffset;
        labelTransform.rotation = cam.rotation;
        RefreshAmount(false);
    }

    private void HandleProgress(float progress)
    {
        RefreshAmount(false);
    }

    private void HandleUpgraded(int level)
    {
        HandleLevelCompleted();
    }

    private void HandleLevelCompleted()
    {
        lastShown = int.MinValue;
        RefreshAmount(true);
    }

    private void RefreshAmount(bool force)
    {
        if (label == null) return;

        if (SourceIsMaxLevel())
        {
            SetLabelVisible(false);
            return;
        }

        int remaining = SourceRemainingCost();
        if (!force && remaining == lastShown) return;

        lastShown = remaining;
        SetLabelVisible(true);
        label.SetText("{0}", remaining);
    }

    private bool SourceIsMaxLevel()
    {
        return purchaseArea != null ? purchaseArea.IsMaxLevel : turret.IsMaxLevel;
    }

    private int SourceRemainingCost()
    {
        return purchaseArea != null ? purchaseArea.RemainingCost : turret.RemainingCost;
    }

    private void SetLabelVisible(bool visible)
    {
        if (label != null && label.gameObject.activeSelf != visible)
        {
            label.gameObject.SetActive(visible);
        }
    }

    private void CacheCamera()
    {
        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }
    }

    private void CreateLabel()
    {
        GameObject textObject = new GameObject("CostLabel");
        textObject.transform.SetParent(transform, false);

        Vector3 parentScale = transform.lossyScale;
        textObject.transform.localScale = new Vector3(
            1f / Mathf.Max(0.01f, parentScale.x),
            1f / Mathf.Max(0.01f, parentScale.y),
            1f / Mathf.Max(0.01f, parentScale.z));

        label = textObject.AddComponent<TextMeshPro>();
        label.text = "0";
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = textColor;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        label.outlineWidth = 0.22f;
        label.outlineColor = new Color(0f, 0f, 0f, 0.9f);

        if (label.fontMaterial != null)
        {
            label.fontMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            label.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
        }

        Renderer textRenderer = label.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.shadowCastingMode = ShadowCastingMode.Off;
            textRenderer.receiveShadows = false;
        }
    }
}
