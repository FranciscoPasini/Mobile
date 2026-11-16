using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings panel: touch or tilt controls, and music/sfx mute. Used in the main menu and the pause menu.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private Button touchButton;
    [SerializeField] private Button tiltButton;

    [Header("Audio")]
    [SerializeField] private Button musicButton;
    [SerializeField] private TextMeshProUGUI musicState;
    [SerializeField] private Button sfxButton;
    [SerializeField] private TextMeshProUGUI sfxState;

    [Header("Look")]
    [SerializeField] private Color onColor = new Color(1f, 0.55f, 0.22f, 1f);
    [SerializeField] private Color offColor = new Color(0.16f, 0.18f, 0.24f, 1f);

    private void Awake()
    {
        if (touchButton != null) touchButton.onClick.AddListener(() => GameSettings.UseGyro = false);
        if (tiltButton != null) tiltButton.onClick.AddListener(() => GameSettings.UseGyro = true);
        if (musicButton != null) musicButton.onClick.AddListener(() => GameSettings.MusicMuted = !GameSettings.MusicMuted);
        if (sfxButton != null) sfxButton.onClick.AddListener(() => GameSettings.SfxMuted = !GameSettings.SfxMuted);
    }

    private void OnEnable()
    {
        GameSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameSettings.Changed -= Refresh;
    }

    private void Refresh()
    {
        bool gyro = GameSettings.UseGyro;
        Tint(touchButton, !gyro);
        Tint(tiltButton, gyro);

        bool musicOn = !GameSettings.MusicMuted;
        bool sfxOn = !GameSettings.SfxMuted;
        Tint(musicButton, musicOn);
        Tint(sfxButton, sfxOn);
        if (musicState != null) musicState.text = musicOn ? "ON" : "OFF";
        if (sfxState != null) sfxState.text = sfxOn ? "ON" : "OFF";
    }

    private void Tint(Button button, bool on)
    {
        if (button != null && button.targetGraphic != null) button.targetGraphic.color = on ? onColor : offColor;
    }
}
