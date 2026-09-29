using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One card in the level-up popup. Filled in by LevelUpPopup, reports back when its button is pressed.
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [Tooltip("Optional. Hidden when the upgrade has no icon.")]
    [SerializeField] private Image iconImage;
    [Tooltip("Found on this GameObject if left empty.")]
    [SerializeField] private Button button;
    [Tooltip("{0} is the current level, {1} the level after picking this card.")]
    [SerializeField] private string levelFormat = "Lv {0}";

    private Upgrade_Class upgrade;
    private Action<Upgrade_Class> onPicked;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }
        else
        {
            Debug.LogWarning($"{name}: no Button assigned, this card can't be picked.", this);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    public void Show(Upgrade_Class upgrade, int currentLevel, Action<Upgrade_Class> onPicked)
    {
        this.upgrade = upgrade;
        this.onPicked = onPicked;

        if (nameText != null) nameText.text = upgrade.UpgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.UpgradeDescription;
        if (levelText != null) levelText.text = string.Format(levelFormat, currentLevel, currentLevel + 1);

        if (iconImage != null)
        {
            iconImage.sprite = upgrade.UpgradeIcon;
            iconImage.enabled = upgrade.UpgradeIcon != null;
        }

        if (button != null) button.interactable = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        upgrade = null;
        onPicked = null;
        gameObject.SetActive(false);
    }

    private void HandleClick()
    {
        if (upgrade == null) return;

        // Blocks a double tap from applying the upgrade twice before the popup closes.
        if (button != null) button.interactable = false;

        Upgrade_Class picked = upgrade;
        Action<Upgrade_Class> callback = onPicked;
        upgrade = null;
        callback?.Invoke(picked);
    }
}
