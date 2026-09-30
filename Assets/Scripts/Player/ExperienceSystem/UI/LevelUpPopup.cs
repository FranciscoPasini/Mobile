using System.Collections.Generic;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows random upgrade cards when the player levels up and applies the one picked.
/// Put this on an object that stays active; it toggles <see cref="popupRoot"/> itself.
/// </summary>
public class LevelUpPopup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Found automatically if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;
    [Tooltip("Panel shown while choosing. Must not be this GameObject, or the level-up event is missed while hidden.")]
    [SerializeField] private GameObject popupRoot;
    [Tooltip("One card per choice. Three cards = three choices.")]
    [SerializeField] private List<UpgradeCard> cards = new List<UpgradeCard>();
    [Tooltip("Always-visible town heal. Created at runtime if left empty.")]
    [SerializeField] private Button healButton;
    [SerializeField] private TMP_Text healButtonLabel;

    [Header("Behaviour")]
    [Tooltip("Freezes the game while the player chooses.")]
    [SerializeField] private bool pauseWhileChoosing = true;
    [SerializeField, Range(0.05f, 1f)] private float townHealPercent = 0.5f;

    [Header("Upgrade Pool")]
    [Tooltip("Upgrades that can appear. Entries sharing a type never show at the same time.")]
    [SerializeField] private List<Upgrade_Class> upgrades = new List<Upgrade_Class>
    {
        new Upgrade_Class(UpgradeType.WeaponDamage, "Damage", "Your bullets hit harder."),
        new Upgrade_Class(UpgradeType.WeaponFireRate, "Fire Rate", "Shoot more often."),
        new Upgrade_Class(UpgradeType.WeaponBulletSpeed, "Bullet Speed", "Bullets reach their target faster."),
        new Upgrade_Class(UpgradeType.WeaponRange, "Range", "Hit enemies from farther away."),
        new Upgrade_Class(UpgradeType.WeaponAmmo, "Ammo", "Picked-up weapons hold more ammo."),
        new Upgrade_Class(UpgradeType.PlayerSpeed, "Move Speed", "Run faster."),
        new Upgrade_Class(UpgradeType.PlayerPickupRange, "Pickup Range", "Collect coins from farther away."),
        new Upgrade_Class(UpgradeType.TownHealth, "Town Health", "The town gains max health and heals the added amount."),
        new Upgrade_Class(UpgradeType.ExperienceGain, "Experience Gain", "Kills grant more experience, so you level up faster."),
        new Upgrade_Class(UpgradeType.PickupDropChance, "Lucky Drops", "One-time pickups drop more often. +3% per level, up to +15%."),
    };

    private readonly List<int> candidateBuffer = new List<int>();
    private readonly List<UpgradeType> shownTypes = new List<UpgradeType>();
    private int pendingLevelUps;
    private bool isShowing;
    private bool pausedByPopup;
    private float timeScaleBeforePause = 1f;

    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }

        if (popupRoot == gameObject)
        {
            Debug.LogWarning($"{name}: popupRoot is this GameObject, so the popup stops listening once hidden. Use a child panel.", this);
        }

        EnsureUpgradeInPool(UpgradeType.WeaponRange, "Range", "Hit enemies from farther away.");
        EnsureUpgradeInPool(UpgradeType.ExperienceGain, "Experience Gain", "Kills grant more experience, so you level up faster.");
        EnsureUpgradeInPool(UpgradeType.PickupDropChance, "Lucky Drops", "One-time pickups drop more often. +3% per level, up to +15%.");
        EnsureHealButton();

        HidePopup();
    }

    private void EnsureUpgradeInPool(UpgradeType type, string upgradeName, string description)
    {
        for (int i = 0; i < upgrades.Count; i++)
        {
            if (upgrades[i] != null && upgrades[i].UpgradeType == type) return;
        }

        upgrades.Add(new Upgrade_Class(type, upgradeName, description));
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onPlayerLevelUp -= HandleLevelUp;
        }

        // Never leave the game frozen if the popup goes away mid-choice.
        Unpause();
    }

    private void HandleLevelUp()
    {
        // Several levels from one big experience reward are shown one after another.
        pendingLevelUps++;
        if (!isShowing) ShowChoices();
    }

    [Button("Show Choices (Debug)")]
    private void ShowChoices()
    {
        if (playerStats == null)
        {
            Debug.LogWarning($"{name}: no Player_ExperienceAndStats found, can't offer upgrades.", this);
            return;
        }

        int shown = FillCards();
        if (shown == 0 && healButton == null)
        {
            Debug.LogWarning($"{name}: no upgrades or cards to show.", this);
            pendingLevelUps = 0;
            HidePopup();
            return;
        }

        isShowing = true;
        if (popupRoot != null) popupRoot.SetActive(true);
        RefreshHealButton();
        Pause();
    }

    /// <summary>
    /// Deals a random upgrade with a different type to each card and hides any cards left over.
    /// </summary>
    private int FillCards()
    {
        candidateBuffer.Clear();
        for (int i = 0; i < upgrades.Count; i++)
        {
            Upgrade_Class entry = upgrades[i];
            if (entry == null) continue;
            if (playerStats != null && playerStats.IsUpgradeCapped(entry.UpgradeType)) continue;
            candidateBuffer.Add(i);
        }

        // Fisher-Yates, then take entries in order while skipping repeated types.
        for (int i = candidateBuffer.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            (candidateBuffer[i], candidateBuffer[swap]) = (candidateBuffer[swap], candidateBuffer[i]);
        }

        shownTypes.Clear();
        int candidate = 0;
        int shown = 0;

        for (int c = 0; c < cards.Count; c++)
        {
            UpgradeCard card = cards[c];
            if (card == null) continue;

            Upgrade_Class upgrade = null;
            while (candidate < candidateBuffer.Count && upgrade == null)
            {
                Upgrade_Class next = upgrades[candidateBuffer[candidate++]];
                if (!shownTypes.Contains(next.UpgradeType)) upgrade = next;
            }

            if (upgrade == null)
            {
                card.Hide();
                continue;
            }

            shownTypes.Add(upgrade.UpgradeType);
            card.Show(upgrade, playerStats.GetUpgradeLevel(upgrade.UpgradeType), HandlePicked);
            shown++;
        }

        return shown;
    }

    private void HandlePicked(Upgrade_Class upgrade)
    {
        if (!isShowing) return;

        playerStats.ApplyUpgrade(upgrade.UpgradeType);
        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

        if (pendingLevelUps > 0)
        {
            FillCards();
            return;
        }

        HidePopup();
    }

    private void HandleHealPicked()
    {
        if (!isShowing) return;

        if (healButton != null) healButton.interactable = false;
        if (playerStats != null) playerStats.HealTownPercent(townHealPercent);

        pendingLevelUps = Mathf.Max(0, pendingLevelUps - 1);

        if (pendingLevelUps > 0)
        {
            FillCards();
            RefreshHealButton();
            return;
        }

        HidePopup();
    }

    private void HidePopup()
    {
        isShowing = false;

        foreach (UpgradeCard card in cards)
        {
            if (card != null) card.Hide();
        }

        if (popupRoot != null && popupRoot != gameObject) popupRoot.SetActive(false);
        Unpause();
    }

    private void OnDestroy()
    {
        if (healButton != null)
        {
            healButton.onClick.RemoveListener(HandleHealPicked);
        }
    }

    private void RefreshHealButton()
    {
        if (healButton == null) return;

        healButton.gameObject.SetActive(true);
        healButton.interactable = true;
        RefreshHealLabel();
    }

    private void RefreshHealLabel()
    {
        if (healButtonLabel != null)
        {
            healButtonLabel.text = $"Heal {Mathf.RoundToInt(townHealPercent * 100f)}%";
        }
    }

    private void EnsureHealButton()
    {
        if (healButton == null)
        {
            CreateHealButton();
        }

        if (healButton != null)
        {
            healButton.onClick.AddListener(HandleHealPicked);
        }

        RefreshHealLabel();
    }

    private void CreateHealButton()
    {
        if (popupRoot == null) return;

        Transform cardsRow = popupRoot.transform.Find("Cards");
        if (cardsRow is RectTransform cardsRect)
        {
            cardsRect.anchoredPosition = new Vector2(cardsRect.anchoredPosition.x, -90f);
        }

        GameObject go = new GameObject("HealTownButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.layer = popupRoot.layer;
        go.transform.SetParent(popupRoot.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 250f);
        rect.sizeDelta = new Vector2(980f, 100f);

        if (cardsRow != null)
        {
            go.transform.SetSiblingIndex(cardsRow.GetSiblingIndex());
        }

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.28f, 0.72f, 0.38f, 1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.4f, 0.85f, 0.48f, 1f);
        colors.pressedColor = new Color(0.2f, 0.55f, 0.28f, 1f);
        button.colors = colors;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
        labelGo.layer = popupRoot.layer;
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 48f;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;
        label.raycastTarget = false;

        healButton = button;
        healButtonLabel = label;
    }

    private void Pause()
    {
        if (!pauseWhileChoosing || pausedByPopup) return;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;
        pausedByPopup = true;
    }

    private void Unpause()
    {
        if (!pausedByPopup) return;

        Time.timeScale = timeScaleBeforePause;
        pausedByPopup = false;
    }
}
