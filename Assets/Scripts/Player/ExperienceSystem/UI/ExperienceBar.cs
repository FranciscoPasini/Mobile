using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Experience progress bar with the player's level in the middle.
/// </summary>
public class ExperienceBar : MonoBehaviour
{
    [Tooltip("Found automatically if left empty.")]
    [SerializeField] private Player_ExperienceAndStats playerStats;
    [Tooltip("Image with Image Type set to Filled, Horizontal.")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private string levelFormat = "Lv {0}";
    [Tooltip("Fill amount per second the bar animates at. 0 snaps instantly.")]
    [SerializeField, Min(0f)] private float fillSpeed = 2f;

    private float targetFill;
    private int shownLevel = -1;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<Player_ExperienceAndStats>();
        }
    }

    private void OnEnable()
    {
        if (playerStats != null)
        {
            playerStats.onExperienceChanged += HandleExperienceChanged;
        }
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.onExperienceChanged -= HandleExperienceChanged;
        }
    }

    private void Start()
    {
        if (playerStats == null)
        {
            Debug.LogWarning($"{name}: no Player_ExperienceAndStats in the scene, the bar stays empty.", this);
            return;
        }

        HandleExperienceChanged(playerStats.GetCurrentPlayerExperience(), playerStats.GetExperienceToNextLevel());
        if (fillImage != null) fillImage.fillAmount = targetFill;
    }

    private void HandleExperienceChanged(int current, int needed)
    {
        targetFill = needed > 0 ? Mathf.Clamp01((float)current / needed) : 0f;

        int level = playerStats.GetCurrentPlayerLevel();
        if (level != shownLevel)
        {
            // New level: refill from empty instead of draining backwards from the old fill.
            if (shownLevel >= 0 && fillImage != null) fillImage.fillAmount = 0f;

            shownLevel = level;
            if (levelText != null) levelText.text = string.Format(levelFormat, level);
        }
    }

    private void Update()
    {
        if (fillImage == null) return;

        // Unscaled so the bar still animates while the level-up popup pauses the game.
        fillImage.fillAmount = fillSpeed > 0f
            ? Mathf.MoveTowards(fillImage.fillAmount, targetFill, fillSpeed * Time.unscaledDeltaTime)
            : targetFill;
    }
}
