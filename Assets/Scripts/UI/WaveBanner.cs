using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a big "WAVE N" banner when a wave starts and fills a bar counting down to the next wave.
/// </summary>
public class WaveBanner : MonoBehaviour
{
    [SerializeField] private TownManager townManager;

    [Header("Banner")]
    [SerializeField] private CanvasGroup banner;
    [SerializeField] private TextMeshProUGUI bannerText;
    [SerializeField] private float popTime = 0.25f;
    [SerializeField] private float holdTime = 1.2f;
    [SerializeField] private float fadeTime = 0.5f;

    [Header("Next Wave Timer (optional)")]
    [SerializeField] private Image nextWaveFill;

    private float time = float.MaxValue;

    private void Awake()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (banner != null) banner.alpha = 0f;
    }

    private void OnEnable()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();
        if (townManager != null) townManager.OnWaveChanged += Show;
    }

    private void OnDisable()
    {
        if (townManager != null) townManager.OnWaveChanged -= Show;
    }

    private void Show()
    {
        if (bannerText != null && townManager != null) bannerText.text = $"WAVE {townManager.CurrentWave}";
        time = 0f;
    }

    private void Update()
    {
        if (nextWaveFill != null && townManager != null) nextWaveFill.fillAmount = townManager.WaveTimerPercent;

        if (banner == null || time > popTime + holdTime + fadeTime) return;

        time += Time.deltaTime;

        float alpha;
        float scale;
        if (time < popTime)
        {
            float t = time / popTime;
            alpha = t;
            scale = Mathf.Lerp(1.6f, 1f, 1f - (1f - t) * (1f - t));
        }
        else
        {
            float fade = Mathf.Clamp01((time - popTime - holdTime) / fadeTime);
            alpha = 1f - fade;
            scale = 1f + fade * 0.15f;
        }

        banner.alpha = alpha;
        banner.transform.localScale = Vector3.one * scale;
    }
}
