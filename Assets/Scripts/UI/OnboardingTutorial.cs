using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Short guide at the start of a run: first points at the camp to defend, then at a turret buy zone.
/// When the first turret is bought, it shows how to upgrade it.
/// Never blocks touches, pauses with the game, and turns itself off when done.
/// </summary>
public class OnboardingTutorial : MonoBehaviour
{
    private const string SeenKey = "OnboardingSeen";

    [Header("Behaviour")]
    [Tooltip("Only show the guide on the very first run on this device.")]
    [SerializeField] private bool onlyFirstRun = false;
    [Tooltip("Waits for the WAVE 1 banner to clear first.")]
    [SerializeField] private float startDelay = 2f;
    [SerializeField] private float fadeTime = 0.4f;

    [Header("Step 1 - Defend")]
    [SerializeField] private string defendTitle = "DEFEND YOUR CAMP";
    [SerializeField, TextArea] private string defendBody = "Enemies are coming for it.\nIf it falls, the run is over!";
    [SerializeField] private string defendLabel = "YOUR CAMP";
    [SerializeField] private Sprite defendIcon;
    [SerializeField] private Color defendColor = new Color(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private float defendDuration = 5f;

    [Header("Step 2 - Build")]
    [SerializeField] private string buildTitle = "BUILD TURRETS";
    [SerializeField, TextArea] private string buildBody = "Stand on a glowing zone to spend coins.\nTurrets shoot enemies for you!";
    [SerializeField] private string buildLabel = "BUY HERE";
    [SerializeField] private Sprite buildIcon;
    [SerializeField] private Color buildColor = new Color(1f, 0.8f, 0.25f, 1f);
    [SerializeField] private float buildDuration = 7f;

    [Header("Step 3 - Upgrade (after the first turret is bought, even once the guide is over)")]
    [SerializeField] private string upgradeTitle = "NICE BUILD!";
    [SerializeField, TextArea] private string upgradeBody = "Stand in its circle again to upgrade it.\nUpgrades make it stronger!";
    [SerializeField] private string upgradeLabel = "UPGRADE HERE";
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private Color upgradeColor = new Color(0.45f, 1f, 0.45f, 1f);
    [Tooltip("Only counts while the player is outside the circle, so it isn't missed behind the zone popup.")]
    [SerializeField] private float upgradeDuration = 6f;

    [Header("References")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform card;
    [SerializeField] private CanvasGroup cardGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image icon;
    [SerializeField] private Image iconBack;
    [SerializeField] private Image timerFill;
    [SerializeField] private Image[] stepDots;
    [Tooltip("Custom/Outline material. Added on top of the camp and buy zone meshes while they're highlighted.")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private RectTransform marker;
    [SerializeField] private RectTransform markerArrow;
    [SerializeField] private TextMeshProUGUI markerLabel;
    [Tooltip("Copied once per enemy spawn area to show where enemies come from.")]
    [SerializeField] private RectTransform edgeArrowTemplate;

    [Header("Layout")]
    [Tooltip("Keeps edge arrows clear of the top HUD and the experience bar, in canvas units.")]
    [SerializeField] private float edgeMarginTop = 330f;
    [SerializeField] private float edgeMarginBottom = 220f;
    [SerializeField] private float edgeMarginSide = 90f;
    [SerializeField] private int maxEdgeArrows = 6;

    // Idle: the intro is over but no turret is bought yet, so it waits invisibly for the upgrade step.
    private enum Phase { Waiting, Defend, Build, Idle, Upgrade, Finished }

    private readonly List<RectTransform> edgeArrows = new List<RectTransform>();
    private readonly List<RectTransform> edgeArrowHeads = new List<RectTransform>();
    private readonly List<Collider> spawnAreas = new List<Collider>();
    private readonly List<PurchaseArea> turretAreas = new List<PurchaseArea>();
    private readonly List<Renderer> outlined = new List<Renderer>();
    private readonly List<Material[]> originalMaterials = new List<Material[]>();
    private readonly List<Renderer> rendererBuffer = new List<Renderer>();
    private Material defendOutline;
    private Material buildOutline;
    private Material upgradeOutline;
    private PurchaseArea buildArea;
    private PurchaseArea upgradeArea;
    private bool upgradeShown;

    private Phase phase = Phase.Waiting;
    private float phaseTime;
    private float phaseDuration;
    private float cardTime;
    private float fade;
    private float zoneFade = 1f;
    private bool playerInZone;
    private Camera cam;
    private Canvas canvas;
    private RectTransform root;
    private Transform town;
    private Transform buildTarget;

    private void Awake()
    {
        if (onlyFirstRun && PlayerPrefs.GetInt(SeenKey, 0) == 1)
        {
            gameObject.SetActive(false);
            return;
        }

        root = (RectTransform)transform;
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        canvas = parentCanvas != null ? parentCanvas.rootCanvas : null;
        if (group != null) group.alpha = 0f;
        if (edgeArrowTemplate != null) edgeArrowTemplate.gameObject.SetActive(false);
        if (marker != null) marker.gameObject.SetActive(false);
    }

    private void Start()
    {
        cam = Camera.main;

        Building_Town townBuilding = FindFirstObjectByType<Building_Town>();
        if (townBuilding != null) town = townBuilding.transform;

        EnemyPool pool = FindFirstObjectByType<EnemyPool>();
        if (pool != null && edgeArrowTemplate != null)
        {
            for (int i = 0; i < pool.SpawnAreas.Count && spawnAreas.Count < maxEdgeArrows; i++)
            {
                if (pool.SpawnAreas[i] == null) continue;
                spawnAreas.Add(pool.SpawnAreas[i]);
                RectTransform arrow = Instantiate(edgeArrowTemplate, edgeArrowTemplate.parent);
                arrow.gameObject.SetActive(false);
                edgeArrows.Add(arrow);
                edgeArrowHeads.Add(arrow.childCount > 0 ? (RectTransform)arrow.GetChild(0) : arrow);
            }
        }

        FindBuildTarget();
        defendOutline = MakeOutline(defendColor);
        buildOutline = MakeOutline(buildColor);
        upgradeOutline = MakeOutline(upgradeColor);
    }

    private void OnEnable()
    {
        PurchaseArea.OnPlayerEntered += HandleEnteredZone;
        PurchaseArea.OnPlayerExited += HandleExitedZone;
    }

    private void OnDisable()
    {
        PurchaseArea.OnPlayerEntered -= HandleEnteredZone;
        PurchaseArea.OnPlayerExited -= HandleExitedZone;
        for (int i = 0; i < turretAreas.Count; i++)
        {
            if (turretAreas[i] != null) turretAreas[i].OnPurchased -= HandleTurretBought;
        }
        ClearOutline();
    }

    private void OnDestroy()
    {
        if (defendOutline != null) Destroy(defendOutline);
        if (buildOutline != null) Destroy(buildOutline);
        if (upgradeOutline != null) Destroy(upgradeOutline);
    }

    private Material MakeOutline(Color color)
    {
        if (outlineMaterial == null) return null;
        var material = new Material(outlineMaterial);
        material.SetColor("_Color", color);
        return material;
    }

    /// <summary>
    /// Adds the outline as one extra material on every mesh under the given objects.
    /// </summary>
    private void Outline(Material material, Transform first, Transform second = null)
    {
        ClearOutline();
        if (material == null) return;

        AddOutline(material, first);
        AddOutline(material, second);
    }

    private void AddOutline(Material material, Transform target)
    {
        if (target == null) return;

        target.GetComponentsInChildren(false, rendererBuffer);
        for (int i = 0; i < rendererBuffer.Count; i++)
        {
            Renderer r = rendererBuffer[i];
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !r.enabled || outlined.Contains(r)) continue;
            // World space cost labels are TextMeshPro meshes.
            if (r.TryGetComponent(out TMP_Text _)) continue;

            Material[] original = r.sharedMaterials;
            var withOutline = new Material[original.Length + 1];
            original.CopyTo(withOutline, 0);
            withOutline[original.Length] = material;
            r.sharedMaterials = withOutline;

            outlined.Add(r);
            originalMaterials.Add(original);
        }
    }

    private void ClearOutline()
    {
        for (int i = 0; i < outlined.Count; i++)
        {
            if (outlined[i] != null) outlined[i].sharedMaterials = originalMaterials[i];
        }
        outlined.Clear();
        originalMaterials.Clear();
    }

    private void HandleEnteredZone(PurchaseArea area) => playerInZone = true;
    private void HandleExitedZone(PurchaseArea area) => playerInZone = false;

    private void HandleTurretBought()
    {
        if (upgradeShown) return;

        PurchaseArea bought = null;
        for (int i = 0; i < turretAreas.Count; i++)
        {
            if (turretAreas[i] != null && turretAreas[i].IsPurchased) { bought = turretAreas[i]; break; }
        }

        if (bought == null || bought.IsMaxLevel)
        {
            // Nothing left to upgrade, so there's nothing to teach.
            upgradeShown = true;
            if (phase == Phase.Idle) EnterPhase(Phase.Finished);
            return;
        }

        upgradeArea = bought;
        EnterPhase(Phase.Upgrade);
    }

    private void Complete()
    {
        if (onlyFirstRun)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }
        gameObject.SetActive(false);
    }

    /// <summary>
    /// The closest turret zone still for sale to the camp. Any unbought zone works if there are no turrets.
    /// </summary>
    private void FindBuildTarget()
    {
        Vector3 from = town != null ? town.position : Vector3.zero;
        float bestTurret = float.MaxValue;
        float bestOther = float.MaxValue;
        PurchaseArea turret = null;
        PurchaseArea other = null;

        foreach (PurchaseArea area in FindObjectsByType<PurchaseArea>(FindObjectsSortMode.None))
        {
            if (area.IsPurchased || !area.isActiveAndEnabled) continue;

            float distance = (area.transform.position - from).sqrMagnitude;
            if (area.Target is Building_Turret)
            {
                turretAreas.Add(area);
                area.OnPurchased += HandleTurretBought;
                if (distance < bestTurret) { bestTurret = distance; turret = area; }
            }
            else if (distance < bestOther)
            {
                bestOther = distance;
                other = area;
            }
        }

        buildArea = turret != null ? turret : other;
        if (buildArea != null) buildTarget = buildArea.transform;
    }

    private void EnterPhase(Phase next)
    {
        phase = next;
        phaseTime = 0f;
        cardTime = 0f;

        switch (next)
        {
            case Phase.Defend:
                phaseDuration = defendDuration;
                ShowCard(defendTitle, defendBody, defendIcon, defendColor, 0);
                SetMarker(town != null, defendLabel, defendColor);
                Outline(defendOutline, town);
                break;
            case Phase.Build:
                phaseDuration = buildDuration;
                ShowCard(buildTitle, buildBody, buildIcon, buildColor, 1);
                SetMarker(buildTarget != null, buildLabel, buildColor);
                Component building = buildArea != null ? buildArea.Target as Component : null;
                Outline(buildOutline, buildTarget, building != null ? building.transform : null);
                break;
            case Phase.Upgrade:
                upgradeShown = true;
                phaseDuration = upgradeDuration;
                ShowCard(upgradeTitle, upgradeBody, upgradeIcon, upgradeColor, 2);
                SetMarker(true, upgradeLabel, upgradeColor);
                Component turret = upgradeArea.Target as Component;
                Outline(upgradeOutline, upgradeArea.transform, turret != null ? turret.transform : null);
                break;
            case Phase.Idle:
            case Phase.Finished:
                phaseDuration = fadeTime;
                SetMarker(false, string.Empty, buildColor);
                ClearOutline();
                break;
        }

        bool showArrows = next == Phase.Defend;
        for (int i = 0; i < edgeArrows.Count; i++) edgeArrows[i].gameObject.SetActive(showArrows);
    }

    private void ShowCard(string title, string body, Sprite iconSprite, Color color, int stepIndex)
    {
        if (titleText != null) { titleText.text = title; titleText.color = color; }
        if (bodyText != null) bodyText.text = body;
        if (icon != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = iconSprite != null;
        }
        if (iconBack != null) iconBack.color = new Color(color.r, color.g, color.b, 0.25f);
        if (timerFill != null) timerFill.color = color;

        if (stepDots == null) return;
        for (int i = 0; i < stepDots.Length; i++)
        {
            if (stepDots[i] == null) continue;
            stepDots[i].color = i == stepIndex ? color : new Color(1f, 1f, 1f, 0.25f);
            stepDots[i].rectTransform.localScale = Vector3.one * (i == stepIndex ? 1.25f : 1f);
        }
    }

    private void SetMarker(bool visible, string label, Color color)
    {
        if (marker == null) return;

        marker.gameObject.SetActive(visible);
        if (!visible) return;

        if (markerLabel != null) { markerLabel.text = label; markerLabel.color = color; }
        if (markerArrow != null && markerArrow.TryGetComponent(out Image arrowImage)) arrowImage.color = color;
    }

    private void Update()
    {
        if (phase == Phase.Idle && fade <= 0f) return;

        float dt = Time.deltaTime;
        float unscaled = Time.unscaledDeltaTime;

        // Pause and the level-up popup set timeScale to 0: the guide hides and its timer stops.
        bool running = Time.timeScale > 0f;
        bool zoneStep = phase == Phase.Build || phase == Phase.Upgrade;
        if (!(zoneStep && playerInZone)) phaseTime += dt;
        cardTime += unscaled;

        switch (phase)
        {
            case Phase.Waiting:
                if (phaseTime >= startDelay) EnterPhase(Phase.Defend);
                break;
            case Phase.Defend:
                if (phaseTime >= phaseDuration) EnterPhase(buildTarget != null ? Phase.Build : EndPhase());
                break;
            case Phase.Build:
                if (phaseTime >= phaseDuration) EnterPhase(EndPhase());
                break;
            case Phase.Upgrade:
                // The first upgrade proves the point, so wrap up early.
                if (phaseTime >= phaseDuration || upgradeArea.Level >= 2) EnterPhase(Phase.Finished);
                break;
            case Phase.Finished:
                if (phaseTime >= phaseDuration)
                {
                    Complete();
                    return;
                }
                break;
        }

        bool shown = running && phase != Phase.Waiting && phase != Phase.Finished && phase != Phase.Idle;
        fade = Mathf.MoveTowards(fade, shown ? 1f : 0f, unscaled / Mathf.Max(0.01f, fadeTime));
        if (group != null) group.alpha = fade;
        if (phase == Phase.Waiting || fade <= 0f) return;

        AnimateCard();
        if (phase == Phase.Defend) PlaceMarker(town);
        else if (phase == Phase.Build) PlaceMarker(buildTarget);
        else if (phase == Phase.Upgrade) PlaceMarker(upgradeArea.transform);
        if (phase == Phase.Defend) PlaceEdgeArrows();
    }

    /// <summary>
    /// Keeps waiting for the first turret if the upgrade step hasn't happened yet.
    /// </summary>
    private Phase EndPhase()
    {
        return upgradeShown || turretAreas.Count == 0 ? Phase.Finished : Phase.Idle;
    }

    private void AnimateCard()
    {
        if (card == null) return;

        // Ease out back pop each time the step changes.
        float t = Mathf.Clamp01(cardTime / 0.35f);
        float eased = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
        card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, eased);

        // Standing in a buy zone shows the zone's own popup in the same spot, so step aside.
        if (cardGroup != null)
        {
            float target = playerInZone && (phase == Phase.Build || phase == Phase.Upgrade) ? 0.15f : 1f;
            zoneFade = Mathf.MoveTowards(zoneFade, target, Time.unscaledDeltaTime * 4f);
            cardGroup.alpha = zoneFade * Mathf.Clamp01(t * 2f);
        }

        if (timerFill != null && phaseDuration > 0f) timerFill.fillAmount = 1f - Mathf.Clamp01(phaseTime / phaseDuration);

        if (icon != null)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 4f);
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, bob * 6f);
        }
    }

    private void PlaceMarker(Transform target)
    {
        if (marker == null || target == null || cam == null) return;

        bool onScreen = ToCanvas(target.position, out Vector2 local, out float angle, false);
        marker.anchoredPosition = local;

        float time = Time.unscaledTime;
        if (markerLabel != null) markerLabel.enabled = onScreen;

        if (markerArrow != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(time * 4f)) * 26f;
            if (onScreen)
            {
                markerArrow.anchoredPosition = new Vector2(0f, 170f + bob);
                markerArrow.localRotation = Quaternion.Euler(0f, 0f, 180f);
            }
            else
            {
                // Off screen: the arrow sits on the edge and points toward the target.
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                markerArrow.anchoredPosition = -dir * (40f + bob * 0.5f);
                markerArrow.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
            }
        }
    }

    private void PlaceEdgeArrows()
    {
        if (cam == null) return;

        float time = Time.unscaledTime;
        for (int i = 0; i < edgeArrows.Count; i++)
        {
            RectTransform arrow = edgeArrows[i];
            Collider area = spawnAreas[i];
            if (area == null) continue;

            ToCanvas(area.bounds.center, out Vector2 local, out float angle, true);
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            arrow.anchoredPosition = local - dir * 40f;

            // The head pokes out toward the spawn; the enemy icon in the middle stays upright.
            RectTransform head = edgeArrowHeads[i];
            float push = 55f + Mathf.Sin(time * 6f + i) * 10f;
            head.anchoredPosition = dir * push;
            head.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
        }
    }

    /// <summary>
    /// World point to a position in this canvas. Returns false when it is clamped to the screen edge,
    /// with <paramref name="angle"/> pointing from the screen centre toward the point.
    /// </summary>
    private bool ToCanvas(Vector3 world, out Vector2 local, out float angle, bool alwaysEdge)
    {
        Vector3 screen = cam.WorldToScreenPoint(world);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir = (Vector2)screen - center;
        if (screen.z < 0f) dir = -dir;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
        angle = Mathf.Atan2(dir.y, dir.x);

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        float top = edgeMarginTop * scale;
        float bottom = edgeMarginBottom * scale;
        float side = edgeMarginSide * scale;

        Vector2 point = (Vector2)screen;
        bool onScreen = screen.z > 0f
            && point.x > side && point.x < Screen.width - side
            && point.y > bottom && point.y < Screen.height - top;

        if (alwaysEdge || !onScreen)
        {
            float halfX = center.x - side;
            float limitY = dir.y > 0f ? center.y - top : center.y - bottom;
            float k = Mathf.Min(halfX / Mathf.Max(0.001f, Mathf.Abs(dir.x)), limitY / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            point = center + dir * k;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, point, null, out local);
        return onScreen && !alwaysEdge;
    }

    [Button("Reset First Run Flag")]
    private void ResetSeen()
    {
        PlayerPrefs.DeleteKey(SeenKey);
        Debug.Log("Onboarding will show again on the next run.");
    }
}
