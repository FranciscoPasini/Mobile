using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Restyles the main menu and the in-game HUD in the open scene. Existing buttons are restyled in place,
/// so their OnClick wiring (ads, scene loads, pause) is kept. Running it again is safe.
/// </summary>
public static class UIStyleSetup
{
    private const string GeneratedFolder = "Assets/Sprites/UI/Generated";
    private const string TitleFontPath = "Assets/Fonts/karmatic-arcade/ka1 SDF.asset";

    private static readonly Color Panel = new Color(0.08f, 0.09f, 0.13f, 0.92f);
    private static readonly Color PanelLight = new Color(0.16f, 0.18f, 0.24f, 1f);
    private static readonly Color Accent = new Color(1f, 0.55f, 0.22f, 1f);
    private static readonly Color Gold = new Color(1f, 0.8f, 0.25f, 1f);
    private static readonly Color Danger = new Color(1f, 0.35f, 0.3f, 1f);
    private static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.85f);

    private static Sprite rounded;
    private static Sprite white;
    private static TMP_FontAsset titleFont;

    #region Main menu

    [MenuItem("Tools/Game/Style Main Menu UI")]
    public static void StyleMainMenu()
    {
        var manager = Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        Canvas canvas = FindRootCanvas();
        if (manager == null || canvas == null)
        {
            Debug.LogError("Open the MainMenu scene first (needs a Canvas and a MainMenuManager).");
            return;
        }

        LoadAssets();
        Undo.SetCurrentGroupName("Style Main Menu UI");
        int undoGroup = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Style Main Menu UI");

        // Background gradient plus a warm campfire glow at the bottom.
        Transform background = FindChild(canvas.transform, "Background");
        if (background != null)
        {
            Image bg = GetOrAdd<Image>(background.gameObject);
            bg.sprite = GetGradientSprite();
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
            bg.raycastTarget = false;
            Stretch((RectTransform)background, 0f, 0f, 0f, 0f);
            background.SetAsFirstSibling();
        }

        RemoveExisting(canvas.transform, "FireGlow");
        Image fireGlow = CreateImage("FireGlow", canvas.transform, GetGlowSprite(), new Color(1f, 0.45f, 0.15f, 0.35f));
        RectTransform fireRect = fireGlow.rectTransform;
        fireRect.anchorMin = fireRect.anchorMax = new Vector2(0.5f, 0f);
        fireRect.pivot = new Vector2(0.5f, 0.5f);
        fireRect.anchoredPosition = Vector2.zero;
        fireRect.sizeDelta = new Vector2(1800f, 1100f);
        fireRect.SetSiblingIndex(background != null ? 1 : 0);
        SetFloat(Undo.AddComponent<UIFloat>(fireGlow.gameObject), 0f, 0.05f, 1.2f);

        // Title, glow and subtitle.
        Transform titleTransform = FindChild(canvas.transform, "Title");
        if (titleTransform != null)
        {
            Transform parent = titleTransform.parent;
            RemoveExisting(parent, "TitleGlow");
            RemoveExisting(parent, "Subtitle");

            Image titleGlow = CreateImage("TitleGlow", parent, GetGlowSprite(), new Color(1f, 0.7f, 0.25f, 0.25f));
            SetAnchoredCenter(titleGlow.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1300f, 800f));
            titleGlow.transform.SetSiblingIndex(titleTransform.GetSiblingIndex());

            TextMeshProUGUI title = titleTransform.GetComponent<TextMeshProUGUI>();
            StyleText(title, "CAMPSITE\nDEFENSE", 150f, Gold, true);
            title.lineSpacing = -10f;
            SetAnchoredCenter(title.rectTransform, new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1000f, 420f));
            GetOrAdd<UIFloat>(title.gameObject);

            TextMeshProUGUI subtitle = CreateText("Subtitle", parent, "SURVIVE THE NIGHT", 44f, SoftWhite, true);
            subtitle.characterSpacing = 8f;
            SetAnchoredCenter(subtitle.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0f, -250f), new Vector2(1000f, 80f));
        }

        // Button column: Play + Quit.
        Transform playTransform = FindChild(canvas.transform, "ButtonPlay");
        if (playTransform != null)
        {
            Button play = playTransform.GetComponent<Button>();
            RectTransform column = (RectTransform)playTransform.parent;
            Image columnImage = column.GetComponent<Image>();
            if (columnImage != null)
            {
                columnImage.color = Color.clear;
                columnImage.raycastTarget = false;
            }
            SetAnchoredCenter(column, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(680f, 400f));
            SetupColumn(column, 36f);

            StyleButton(play, Accent, "PLAY", 96f, 190f);

            Transform quitTransform = column.Find("ButtonQuit");
            if (quitTransform == null)
            {
                GameObject quitGo = Object.Instantiate(play.gameObject, column);
                quitGo.name = "ButtonQuit";
                Undo.RegisterCreatedObjectUndo(quitGo, "Create Quit Button");
                quitTransform = quitGo.transform;
            }
            Button quit = quitTransform.GetComponent<Button>();
            quit.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(quit.onClick, manager.Exit);
            StyleButton(quit, PanelLight, "QUIT", 60f, 140f);
            quitTransform.SetAsLastSibling();
        }

        Transform settingsButton = FindChild(canvas.transform, "SettingsButton");
        if (settingsButton != null && settingsButton.TryGetComponent(out Button settings))
        {
            StyleButton(settings, PanelLight, "SETTINGS", 48f, 0f);
        }

        // Settings panel as a card.
        var managerSo = new SerializedObject(manager);
        GameObject settingsPanel = managerSo.FindProperty("settingsPanel").objectReferenceValue as GameObject;
        if (settingsPanel != null)
        {
            StyleCard(settingsPanel);
            foreach (TextMeshProUGUI text in settingsPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.GetComponentInParent<Button>(true) != null) continue;
                StyleText(text, "SETTINGS", 80f, Gold, true);
            }
            foreach (Button button in settingsPanel.GetComponentsInChildren<Button>(true))
            {
                StyleButton(button, Accent, "CLOSE", 56f, 0f);
            }
            GetOrAdd<UIPopIn>(settingsPanel);
        }

        RemoveExisting(canvas.transform, "Version");
        TextMeshProUGUI version = CreateText("Version", canvas.transform, $"V{PlayerSettings.bundleVersion}", 28f, new Color(1f, 1f, 1f, 0.4f), false);
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = version.rectTransform.pivot = new Vector2(0.5f, 0f);
        version.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        version.rectTransform.sizeDelta = new Vector2(600f, 50f);

        Finish(canvas, undoGroup, "Main menu styled.");
    }

    #endregion

    #region Game HUD

    [MenuItem("Tools/Game/Style Game HUD UI")]
    public static void StyleGameHud()
    {
        var texts = Object.FindFirstObjectByType<UIsTexts>(FindObjectsInactive.Include);
        var townManager = Object.FindFirstObjectByType<TownManager>(FindObjectsInactive.Include);
        Canvas canvas = FindRootCanvas();
        if (texts == null || townManager == null || canvas == null)
        {
            Debug.LogError("Open the Game scene first (needs a Canvas, UIsTexts and TownManager).");
            return;
        }

        LoadAssets();
        Undo.SetCurrentGroupName("Style Game HUD UI");
        int undoGroup = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Style Game HUD UI");

        var safeArea = Object.FindFirstObjectByType<SafeArea>(FindObjectsInactive.Include);
        Transform hudParent = safeArea != null ? safeArea.transform : canvas.transform;

        RemoveExisting(canvas.transform, "DamageVignette");
        RemoveExisting(hudParent, "TopBar");
        RemoveExisting(hudParent, "WaveBanner");

        CreateVignette(canvas.transform, townManager);

        RectTransform topBar = CreateRect("TopBar", hudParent);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.offsetMin = new Vector2(30f, -250f);
        topBar.offsetMax = new Vector2(-30f, -30f);

        CreateHealthCard(topBar, townManager);
        TextMeshProUGUI coinsText = CreatePill("Coins", topBar, new Vector2(0f, -116f), 270f,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Coin.png"), "0", Gold);
        TextMeshProUGUI ammoText = CreatePill("Ammo", topBar, new Vector2(290f, -116f), 310f,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/BulletsAmmo.png"), "INF", Color.white);
        Image nextWaveFill;
        TextMeshProUGUI waveText = CreateWaveBadge(topBar, out nextWaveFill);

        CreateWaveBanner(hudParent, townManager, nextWaveFill);
        CreateBuildingNamePopup(hudParent);
        CreateWeaponPickupPopup(hudParent);

        var so = new SerializedObject(texts);
        so.FindProperty("waveText").objectReferenceValue = waveText;
        so.FindProperty("coinsText").objectReferenceValue = coinsText;
        so.FindProperty("ammoText").objectReferenceValue = ammoText;
        so.FindProperty("coinsPunch").objectReferenceValue = coinsText.GetComponent<UIPunch>();
        so.FindProperty("wavePunch").objectReferenceValue = waveText.GetComponent<UIPunch>();
        so.ApplyModifiedPropertiesWithoutUndo();

        // Old HUD is hidden, not deleted, in case you want something back from it.
        Transform oldStats = FindChild(canvas.transform, "UIStatsPanel");
        if (oldStats != null) oldStats.gameObject.SetActive(false);

        StylePauseButton(canvas.transform);
        StylePausePanel(canvas.transform);
        StyleLosePanel(canvas.transform, townManager);

        Finish(canvas, undoGroup, "Game HUD styled.");
    }

    private static void CreateVignette(Transform canvas, TownManager townManager)
    {
        Image vignette = CreateImage("DamageVignette", canvas, GetVignetteSprite(), Color.clear);
        Stretch(vignette.rectTransform, 0f, 0f, 0f, 0f);
        vignette.transform.SetAsFirstSibling();

        var damage = vignette.gameObject.AddComponent<DamageVignette>();
        var so = new SerializedObject(damage);
        so.FindProperty("townManager").objectReferenceValue = townManager;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateHealthCard(RectTransform topBar, TownManager townManager)
    {
        Image card = CreateImage("TownHealth", topBar, rounded, Panel);
        card.type = Image.Type.Sliced;
        PlaceTopLeft(card.rectTransform, Vector2.zero, new Vector2(600f, 100f));

        Sprite heart = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/FreeButtonSet/Textures/icons/128x128/heart.png");
        Image icon = CreateImage("Icon", card.transform, heart, Danger);
        PlaceLeft(icon.rectTransform, 16f, new Vector2(72f, 72f));
        icon.enabled = heart != null;

        Image back = CreateImage("BarBack", card.transform, rounded, new Color(0f, 0f, 0f, 0.55f));
        back.type = Image.Type.Sliced;
        RectTransform backRect = back.rectTransform;
        backRect.anchorMin = new Vector2(0f, 0.5f);
        backRect.anchorMax = new Vector2(1f, 0.5f);
        backRect.offsetMin = new Vector2(104f, -26f);
        backRect.offsetMax = new Vector2(-20f, 26f);

        Image chip = CreateFill("Chip", back.transform, new Color(1f, 0.92f, 0.7f, 1f));
        Image fill = CreateFill("Fill", back.transform, new Color(0.3f, 0.85f, 0.4f, 1f));

        Image flash = CreateImage("Flash", back.transform, rounded, Color.clear);
        flash.type = Image.Type.Sliced;
        Stretch(flash.rectTransform, 0f, 0f, 0f, 0f);

        TextMeshProUGUI label = CreateText("Label", back.transform, "100 / 100", 30f, Color.white, true);
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        var bar = card.gameObject.AddComponent<TownHealthBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("townManager").objectReferenceValue = townManager;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("chip").objectReferenceValue = chip;
        so.FindProperty("flash").objectReferenceValue = flash;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("shakeTarget").objectReferenceValue = card.rectTransform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI CreatePill(string name, RectTransform topBar, Vector2 position, float width, Sprite iconSprite, string value, Color valueColor)
    {
        Image pill = CreateImage(name, topBar, rounded, Panel);
        pill.type = Image.Type.Sliced;
        PlaceTopLeft(pill.rectTransform, position, new Vector2(width, 84f));

        Image icon = CreateImage("Icon", pill.transform, iconSprite, Color.white);
        icon.preserveAspect = true;
        icon.enabled = iconSprite != null;
        PlaceLeft(icon.rectTransform, 12f, new Vector2(60f, 60f));

        TextMeshProUGUI text = CreateText("Value", pill.transform, value, 42f, valueColor, true);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(text.rectTransform, 86f, 14f, 0f, 0f);
        text.gameObject.AddComponent<UIPunch>();
        return text;
    }

    private static TextMeshProUGUI CreateWaveBadge(RectTransform topBar, out Image nextWaveFill)
    {
        Image badge = CreateImage("Wave", topBar, rounded, Panel);
        badge.type = Image.Type.Sliced;
        RectTransform rect = badge.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        // Leaves room for the pause button in the corner.
        rect.anchoredPosition = new Vector2(-150f, 0f);
        rect.sizeDelta = new Vector2(230f, 150f);

        TextMeshProUGUI header = CreateText("Header", badge.transform, "WAVE", 28f, Accent, true);
        header.characterSpacing = 6f;
        AnchorTopBand(header.rectTransform, 10f, 36f);

        TextMeshProUGUI number = CreateText("Value", badge.transform, "1", 68f, Color.white, true);
        Stretch(number.rectTransform, 10f, 10f, 30f, 40f);
        number.gameObject.AddComponent<UIPunch>();

        Image timerBack = CreateImage("NextWaveBack", badge.transform, white, new Color(0f, 0f, 0f, 0.5f));
        RectTransform timerRect = timerBack.rectTransform;
        timerRect.anchorMin = new Vector2(0f, 0f);
        timerRect.anchorMax = new Vector2(1f, 0f);
        timerRect.offsetMin = new Vector2(20f, 14f);
        timerRect.offsetMax = new Vector2(-20f, 24f);

        nextWaveFill = CreateFill("NextWaveFill", timerBack.transform, Accent);
        Stretch(nextWaveFill.rectTransform, 0f, 0f, 0f, 0f);
        nextWaveFill.fillAmount = 0f;
        return number;
    }

    private static void CreateWaveBanner(Transform parent, TownManager townManager, Image nextWaveFill)
    {
        RectTransform root = CreateRect("WaveBanner", parent);
        SetAnchoredCenter(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1000f, 240f));

        RectTransform bannerRect = CreateRect("Banner", root);
        Stretch(bannerRect, 0f, 0f, 0f, 0f);
        CanvasGroup group = bannerRect.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image glow = CreateImage("Glow", bannerRect, GetGlowSprite(), new Color(1f, 0.6f, 0.2f, 0.35f));
        Stretch(glow.rectTransform, -100f, -100f, -60f, -60f);

        TextMeshProUGUI text = CreateText("Text", bannerRect, "WAVE 1", 150f, Gold, true);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        var banner = root.gameObject.AddComponent<WaveBanner>();
        var so = new SerializedObject(banner);
        so.FindProperty("townManager").objectReferenceValue = townManager;
        so.FindProperty("banner").objectReferenceValue = group;
        so.FindProperty("bannerText").objectReferenceValue = text;
        so.FindProperty("nextWaveFill").objectReferenceValue = nextWaveFill;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Game/Add Building Name Popup")]
    public static void AddBuildingNamePopup()
    {
        Canvas canvas = FindRootCanvas();
        if (canvas == null)
        {
            Debug.LogError("Open the Game scene first (needs a Canvas).");
            return;
        }

        LoadAssets();
        Undo.SetCurrentGroupName("Add Building Name Popup");
        int undoGroup = Undo.GetCurrentGroup();

        var safeArea = Object.FindFirstObjectByType<SafeArea>(FindObjectsInactive.Include);
        CreateBuildingNamePopup(safeArea != null ? safeArea.transform : canvas.transform);

        Finish(canvas, undoGroup, "Building name popup added.");
    }

    private static void CreateBuildingNamePopup(Transform parent)
    {
        RemoveExisting(parent, "BuildingNamePopup");

        // Below the top bar, above the wave banner.
        RectTransform root = CreateRect("BuildingNamePopup", parent);
        SetAnchoredCenter(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 560f), new Vector2(1000f, 200f));
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        Image backing = CreateImage("Backing", root, GetGlowSprite(), new Color(0f, 0f, 0f, 0.6f));
        Stretch(backing.rectTransform, -60f, -60f, -40f, -40f);

        TextMeshProUGUI nameText = CreateText("Name", root, "BUILDING", 80f, Color.white, true);
        AnchorTopBand(nameText.rectTransform, 10f, 110f);

        TextMeshProUGUI infoText = CreateText("Info", root, "BUY", 38f, SoftWhite, true);
        infoText.characterSpacing = 4f;
        AnchorTopBand(infoText.rectTransform, 125f, 60f);

        var popup = root.gameObject.AddComponent<BuildingNamePopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("infoText").objectReferenceValue = infoText;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/Game/Add Weapon Pickup Popup")]
    public static void AddWeaponPickupPopup()
    {
        Canvas canvas = FindRootCanvas();
        if (canvas == null)
        {
            Debug.LogError("Open the Game scene first (needs a Canvas).");
            return;
        }

        LoadAssets();
        Undo.SetCurrentGroupName("Add Weapon Pickup Popup");
        int undoGroup = Undo.GetCurrentGroup();

        var safeArea = Object.FindFirstObjectByType<SafeArea>(FindObjectsInactive.Include);
        CreateWeaponPickupPopup(safeArea != null ? safeArea.transform : canvas.transform);

        Finish(canvas, undoGroup, "Weapon pickup popup added.");
    }

    private static void CreateWeaponPickupPopup(Transform parent)
    {
        RemoveExisting(parent, "WeaponPickupPopup");

        // Just under the player, who stands in the middle of the screen.
        RectTransform root = CreateRect("WeaponPickupPopup", parent);
        SetAnchoredCenter(root, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(1000f, 160f));
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // No background, so an outline keeps the rainbow letters readable.
        TextMeshProUGUI text = CreateText("Text", root, "WEAPON", 96f, Color.white, true);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.richText = true;
        text.outlineWidth = 0.25f;
        text.outlineColor = new Color32(0, 0, 0, 255);

        var popup = root.gameObject.AddComponent<WeaponPickupPopup>();
        var so = new SerializedObject(popup);
        so.FindProperty("weaponSystem").objectReferenceValue = Object.FindFirstObjectByType<PlayerWeaponSystem>(FindObjectsInactive.Include);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("text").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void StylePauseButton(Transform canvas)
    {
        Transform pause = FindChild(canvas, "PauseButton");
        if (pause == null || !pause.TryGetComponent(out Button button)) return;

        StyleButton(button, Panel, "II", 56f, 0f);
        RectTransform rect = (RectTransform)pause;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-30f, -30f);
        rect.sizeDelta = new Vector2(130f, 130f);
    }

    private static void StylePausePanel(Transform canvas)
    {
        Transform panel = FindChild(canvas, "PausePanel");
        if (panel == null) return;

        StyleCard(panel.gameObject);
        ((RectTransform)panel).sizeDelta = new Vector2(760f, 760f);

        VerticalLayoutGroup layout = SetupColumn((RectTransform)panel, 36f);
        layout.padding = new RectOffset(60, 60, 70, 70);

        foreach (Transform child in panel)
        {
            if (child.TryGetComponent(out TextMeshProUGUI title))
            {
                StyleText(title, "PAUSED", 110f, Gold, true);
                SetPreferredHeight(child.gameObject, 220f);
            }
        }

        Transform resume = panel.Find("ResumeButton");
        if (resume != null) StyleButton(resume.GetComponent<Button>(), Accent, "RESUME", 72f, 170f);

        Transform menu = panel.Find("MenuButton");
        if (menu != null) StyleButton(menu.GetComponent<Button>(), PanelLight, "MAIN MENU", 52f, 140f);

        // Settings is not implemented in game yet.
        Transform settings = panel.Find("SettingsButton");
        if (settings != null) settings.gameObject.SetActive(false);

        GetOrAdd<UIPopIn>(panel.gameObject);
    }

    private static void StyleLosePanel(Transform canvas, TownManager townManager)
    {
        Transform panel = FindChild(canvas, "LosePanel");
        if (panel == null) return;

        StyleCard(panel.gameObject);
        ((RectTransform)panel).sizeDelta = new Vector2(820f, 1060f);

        Transform header = panel.Find("Losetxt");
        if (header != null && header.TryGetComponent(out TextMeshProUGUI headerText))
        {
            StyleText(headerText, "THE CAMP\nHAS FALLEN", 92f, Danger, true);
            SetAnchoredCenter(headerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 330f), new Vector2(760f, 260f));
        }

        Transform wavesTransform = panel.Find("WavesText");
        TextMeshProUGUI waves = wavesTransform != null
            ? wavesTransform.GetComponent<TextMeshProUGUI>()
            : CreateText("WavesText", panel, "YOU SURVIVED 0 WAVES", 40f, SoftWhite, true);
        SetAnchoredCenter(waves.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(760f, 70f));

        GameOverStats stats = GetOrAdd<GameOverStats>(panel.gameObject);
        var so = new SerializedObject(stats);
        so.FindProperty("townManager").objectReferenceValue = townManager;
        so.FindProperty("wavesText").objectReferenceValue = waves;
        so.ApplyModifiedPropertiesWithoutUndo();

        var townSo = new SerializedObject(townManager);
        townSo.FindProperty("gameOverScreen").objectReferenceValue = panel.gameObject;
        townSo.ApplyModifiedProperties();

        Transform buttons = panel.Find("ButtonPanel");
        if (buttons != null)
        {
            if (buttons.TryGetComponent(out Image buttonsImage)) buttonsImage.color = Color.clear;
            if (buttons.TryGetComponent(out HorizontalLayoutGroup horizontal)) Undo.DestroyObjectImmediate(horizontal);
            if (buttons.TryGetComponent(out ContentSizeFitter fitter)) Undo.DestroyObjectImmediate(fitter);

            SetAnchoredCenter((RectTransform)buttons, new Vector2(0.5f, 0.5f), new Vector2(0f, -220f), new Vector2(620f, 350f));
            SetupColumn((RectTransform)buttons, 30f);

            Transform restart = buttons.Find("RestartButton");
            if (restart != null)
            {
                StyleButton(restart.GetComponent<Button>(), Accent, "TRY AGAIN", 68f, 180f);
                restart.SetAsFirstSibling();
            }

            Transform main = buttons.Find("MainButton");
            if (main != null) StyleButton(main.GetComponent<Button>(), PanelLight, "MAIN MENU", 52f, 140f);
        }

        GetOrAdd<UIPopIn>(panel.gameObject);
    }

    #endregion

    #region Styling helpers

    private static void StyleButton(Button button, Color color, string label, float fontSize, float height)
    {
        if (button == null) return;

        if (button.targetGraphic is Image image)
        {
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            image.color = color;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Shadow shadow = GetOrAdd<Shadow>(button.gameObject);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -8f);

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            StyleText(text, label, fontSize, Color.white, true);
            Stretch(text.rectTransform, 10f, 10f, 0f, 0f);
        }

        if (height > 0f) SetPreferredHeight(button.gameObject, height);
        GetOrAdd<UIButtonFeedback>(button.gameObject);
    }

    private static void StyleText(TextMeshProUGUI text, string value, float size, Color color, bool titleStyle)
    {
        if (text == null) return;

        text.text = value;
        if (titleStyle && titleFont != null) text.font = titleFont;
        text.enableAutoSizing = false;
        text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
    }

    private static void StyleCard(GameObject card)
    {
        Image image = GetOrAdd<Image>(card);
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 0.3f;
        image.color = new Color(0.08f, 0.09f, 0.13f, 0.96f);

        Shadow shadow = GetOrAdd<Shadow>(card);
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -14f);
    }

    private static VerticalLayoutGroup SetupColumn(RectTransform column, float spacing)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(column.gameObject);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return layout;
    }

    private static void SetPreferredHeight(GameObject go, float height)
    {
        LayoutElement element = GetOrAdd<LayoutElement>(go);
        element.preferredHeight = height;
        element.minHeight = height;
    }

    private static void SetFloat(UIFloat floater, float height, float scale, float speed)
    {
        var so = new SerializedObject(floater);
        so.FindProperty("height").floatValue = height;
        so.FindProperty("scaleAmount").floatValue = scale;
        so.FindProperty("speed").floatValue = speed;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    #endregion

    #region Generated sprites

    private static void LoadAssets()
    {
        rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath);
        white = GetGeneratedSprite("White", 4, 4, (u, v) => Color.white);
    }

    private static Sprite GetGradientSprite()
    {
        Color bottom = new Color(0.3f, 0.12f, 0.07f);
        Color middle = new Color(0.09f, 0.09f, 0.17f);
        Color top = new Color(0.03f, 0.04f, 0.09f);
        return GetGeneratedSprite("MenuGradient", 4, 256, (u, v) =>
            v < 0.45f ? Color.Lerp(bottom, middle, v / 0.45f) : Color.Lerp(middle, top, (v - 0.45f) / 0.55f));
    }

    private static Sprite GetGlowSprite()
    {
        return GetGeneratedSprite("Glow", 128, 128, (u, v) =>
        {
            float d = Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 2f;
            float a = Mathf.Clamp01(1f - d);
            return new Color(1f, 1f, 1f, a * a);
        });
    }

    private static Sprite GetVignetteSprite()
    {
        return GetGeneratedSprite("Vignette", 256, 256, (u, v) =>
        {
            float d = new Vector2((u - 0.5f) * 2f, (v - 0.5f) * 2f).magnitude;
            return new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1.35f, d)));
        });
    }

    private static Sprite GetGeneratedSprite(string name, int width, int height, System.Func<float, float, Color> pixel)
    {
        string path = $"{GeneratedFolder}/{name}.png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(GeneratedFolder);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    texture.SetPixel(x, y, pixel(x / (width - 1f), y / (height - 1f)));
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError($"UI Style Setup: could not load sprite '{path}'. Run the tool again.");
        return sprite;
    }

    #endregion

    #region Scene helpers

    private static Canvas FindRootCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas candidate in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!candidate.isRootCanvas || candidate.renderMode == RenderMode.WorldSpace) continue;
            if (candidate.gameObject.scene != SceneManager.GetActiveScene()) continue;
            if (candidate.name == "Canvas") return candidate;
            if (fallback == null) fallback = candidate;
        }
        return fallback;
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    private static void RemoveExisting(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.TryGetComponent(out T component) ? component : Undo.AddComponent<T>(go);
    }

    private static void Finish(Canvas canvas, int undoGroup, string message)
    {
        // Direct edits on prefab instances (SafeArea, pause prefabs) must be recorded or they're lost on reload.
        foreach (Component component in canvas.GetComponentsInChildren<Component>(true))
        {
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log($"{message} Save the scene to keep it.", canvas);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        Image image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateFill(string name, Transform parent, Color color)
    {
        Image fill = CreateImage(name, parent, white, color);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        Stretch(fill.rectTransform, 4f, 4f, 4f, 4f);
        return fill;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, Color color, bool titleStyle)
    {
        TextMeshProUGUI text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        StyleText(text, value, size, color, titleStyle);
        return text;
    }

    private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetAnchoredCenter(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceLeft(RectTransform rect, float x, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = size;
    }

    private static void AnchorTopBand(RectTransform rect, float fromTop, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -fromTop - height);
        rect.offsetMax = new Vector2(0f, -fromTop);
    }

    #endregion
}
