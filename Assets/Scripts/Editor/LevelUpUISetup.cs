using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the experience bar and the level-up popup (with three cards) in the open scene
/// and wires every reference. Running it again replaces the previous ones.
/// </summary>
public static class LevelUpUISetup
{
    private const string BarName = "ExperienceBar";
    private const string PopupName = "LevelUpPopup";
    private const int CardCount = 3;

    private static readonly Color BarBackground = new Color(0.08f, 0.08f, 0.1f, 0.85f);
    private static readonly Color BarFill = new Color(0.3f, 0.8f, 1f, 1f);
    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color CardColor = new Color(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color CardText = new Color(0.15f, 0.15f, 0.2f, 1f);
    private static readonly Color LevelColor = new Color(0.15f, 0.45f, 0.9f, 1f);
    private static readonly Color TitleColor = new Color(1f, 0.85f, 0.2f, 1f);

    private static Sprite roundedSprite;

    [MenuItem("Tools/Game/Setup Level Up UI")]
    public static void Setup()
    {
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        Undo.SetCurrentGroupName("Setup Level Up UI");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = FindOrCreateCanvas();
        Player_ExperienceAndStats stats = FindOrCreateStats();

        RemoveExisting(canvas.transform, BarName);
        RemoveExisting(canvas.transform, PopupName);

        ExperienceBar bar = CreateExperienceBar(canvas.transform, stats);
        LevelUpPopup popup = CreatePopup(canvas.transform, stats);

        // Popup last so it draws over the bar and the rest of the HUD.
        bar.transform.SetAsLastSibling();
        popup.transform.SetAsLastSibling();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = popup.gameObject;

        Debug.Log("Level Up UI created. Save the scene to keep it.", popup);
    }

    #region Scene objects

    private static Canvas FindOrCreateCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!candidate.isRootCanvas || candidate.renderMode == RenderMode.WorldSpace) continue;
            if (candidate.name == "Canvas") return candidate;
            if (fallback == null) fallback = candidate;
        }
        if (fallback != null) return fallback;

        var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
        go.layer = LayerMask.NameToLayer("UI");

        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0f;

        return canvas;
    }

    private static Player_ExperienceAndStats FindOrCreateStats()
    {
        var stats = Object.FindFirstObjectByType<Player_ExperienceAndStats>(FindObjectsInactive.Include);
        if (stats != null) return stats;

        GameObject host = GameObject.Find("Manager");
        if (host == null)
        {
            host = new GameObject("PlayerStats");
            Undo.RegisterCreatedObjectUndo(host, "Create PlayerStats");
        }

        stats = Undo.AddComponent<Player_ExperienceAndStats>(host);

        var town = Object.FindFirstObjectByType<Building_Town>();
        if (town != null)
        {
            var so = new SerializedObject(stats);
            so.FindProperty("TownBuilding").objectReferenceValue = town;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log($"Added Player_ExperienceAndStats to '{host.name}'.", host);
        return stats;
    }

    private static void RemoveExisting(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
    }

    #endregion

    #region Experience bar

    private static ExperienceBar CreateExperienceBar(Transform canvas, Player_ExperienceAndStats stats)
    {
        // Bottom of the screen, full width with side margins, above the phone's home indicator.
        RectTransform root = CreateRect(BarName, canvas);
        root.anchorMin = new Vector2(0f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.offsetMin = new Vector2(60f, 60f);
        root.offsetMax = new Vector2(-60f, 130f);

        Image background = AddImage(root.gameObject, BarBackground, Image.Type.Sliced);
        background.raycastTarget = false;

        RectTransform fillRect = CreateRect("Fill", root);
        Stretch(fillRect, 6f, 6f, 6f, 6f);
        Image fill = AddImage(fillRect.gameObject, BarFill, Image.Type.Filled);
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.raycastTarget = false;

        TextMeshProUGUI levelText = CreateText("LevelText", root, "Lv 1", 40f, FontStyles.Bold, Color.white);
        Stretch(levelText.rectTransform, 0f, 0f, 0f, 0f);
        levelText.outlineWidth = 0.2f;
        levelText.outlineColor = new Color32(0, 0, 0, 200);

        ExperienceBar bar = root.gameObject.AddComponent<ExperienceBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("playerStats").objectReferenceValue = stats;
        so.FindProperty("fillImage").objectReferenceValue = fill;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.ApplyModifiedPropertiesWithoutUndo();

        return bar;
    }

    #endregion

    #region Level up popup

    private static LevelUpPopup CreatePopup(Transform canvas, Player_ExperienceAndStats stats)
    {
        // Stays active so it keeps listening; only the panel below is toggled.
        RectTransform root = CreateRect(PopupName, canvas);
        Stretch(root, 0f, 0f, 0f, 0f);

        RectTransform panel = CreateRect("Panel", root);
        Stretch(panel, 0f, 0f, 0f, 0f);
        // Also blocks taps from reaching the HUD while choosing.
        AddImage(panel.gameObject, Overlay, Image.Type.Simple).sprite = null;

        TextMeshProUGUI title = CreateText("Title", panel, "LEVEL UP!", 90f, FontStyles.Bold, TitleColor);
        SetCentered(title.rectTransform, new Vector2(0f, 420f), new Vector2(900f, 140f));
        title.outlineWidth = 0.2f;
        title.outlineColor = new Color32(0, 0, 0, 255);

        TextMeshProUGUI subtitle = CreateText("Subtitle", panel, "Choose an upgrade", 40f, FontStyles.Normal, Color.white);
        SetCentered(subtitle.rectTransform, new Vector2(0f, 330f), new Vector2(900f, 60f));

        Button healButton = CreateHealButton(panel);
        TMP_Text healLabel = healButton.GetComponentInChildren<TextMeshProUGUI>();

        RectTransform row = CreateRect("Cards", panel);
        SetCentered(row, new Vector2(0f, -90f), new Vector2(1020f, 540f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var cards = new UpgradeCard[CardCount];
        for (int i = 0; i < CardCount; i++)
        {
            cards[i] = CreateCard(row, i + 1);
        }

        LevelUpPopup popup = root.gameObject.AddComponent<LevelUpPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("playerStats").objectReferenceValue = stats;
        so.FindProperty("popupRoot").objectReferenceValue = panel.gameObject;
        so.FindProperty("healButton").objectReferenceValue = healButton;
        so.FindProperty("healButtonLabel").objectReferenceValue = healLabel;
        SerializedProperty cardList = so.FindProperty("cards");
        cardList.arraySize = CardCount;
        for (int i = 0; i < CardCount; i++)
        {
            cardList.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        // Hidden in the saved scene too, so it doesn't flash on the first frame.
        panel.gameObject.SetActive(false);
        return popup;
    }

    private static UpgradeCard CreateCard(Transform row, int number)
    {
        RectTransform card = CreateRect($"UpgradeCard {number}", row);
        card.sizeDelta = new Vector2(310f, 520f);

        Image background = AddImage(card.gameObject, CardColor, Image.Type.Sliced);
        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.75f);
        colors.disabledColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;

        RectTransform iconRect = CreateRect("Icon", card);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -30f);
        iconRect.sizeDelta = new Vector2(140f, 140f);
        Image icon = AddImage(iconRect.gameObject, Color.white, Image.Type.Simple);
        icon.sprite = null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        TextMeshProUGUI nameText = CreateText("Name", card, "Upgrade", 40f, FontStyles.Bold, CardText);
        AnchorTopBand(nameText.rectTransform, 185f, 70f, 16f);

        TextMeshProUGUI description = CreateText("Description", card, "Description", 28f, FontStyles.Normal, CardText);
        Stretch(description.rectTransform, 20f, 20f, 95f, 265f);
        description.alignment = TextAlignmentOptions.Top;
        description.textWrappingMode = TextWrappingModes.Normal;

        TextMeshProUGUI level = CreateText("Level", card, "Lv 0", 34f, FontStyles.Bold, LevelColor);
        level.rectTransform.anchorMin = new Vector2(0f, 0f);
        level.rectTransform.anchorMax = new Vector2(1f, 0f);
        level.rectTransform.pivot = new Vector2(0.5f, 0f);
        level.rectTransform.offsetMin = new Vector2(16f, 25f);
        level.rectTransform.offsetMax = new Vector2(-16f, 85f);

        UpgradeCard upgradeCard = card.gameObject.AddComponent<UpgradeCard>();
        var so = new SerializedObject(upgradeCard);
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("levelText").objectReferenceValue = level;
        so.FindProperty("iconImage").objectReferenceValue = icon;
        so.FindProperty("button").objectReferenceValue = button;
        so.ApplyModifiedPropertiesWithoutUndo();

        return upgradeCard;
    }

    private static Button CreateHealButton(Transform panel)
    {
        RectTransform heal = CreateRect("HealTownButton", panel);
        SetCentered(heal, new Vector2(0f, 250f), new Vector2(980f, 100f));

        Image background = AddImage(heal.gameObject, new Color(0.28f, 0.72f, 0.38f, 1f), Image.Type.Sliced);
        Button button = heal.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.85f, 0.48f, 1f);
        colors.pressedColor = new Color(0.2f, 0.55f, 0.28f, 1f);
        button.colors = colors;

        TextMeshProUGUI label = CreateText("Label", heal, "Heal 50%", 48f, FontStyles.Bold, Color.white);
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    #endregion

    #region Helpers

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image AddImage(GameObject go, Color color, Image.Type type)
    {
        Image image = go.AddComponent<Image>();
        image.sprite = roundedSprite;
        image.type = type;
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AnchorTopBand(RectTransform rect, float fromTop, float height, float sideMargin)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sideMargin, -fromTop - height);
        rect.offsetMax = new Vector2(-sideMargin, -fromTop);
    }

    #endregion
}
