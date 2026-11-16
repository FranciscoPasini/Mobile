using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loads the top runs when opened and lists them. This phone's row is highlighted.
/// </summary>
public class LeaderboardPanel : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    [Tooltip("Inactive row with Rank, Name, Wave and Details texts.")]
    [SerializeField] private GameObject rowTemplate;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField, Min(1)] private int count = 20;

    [Header("Look")]
    [SerializeField] private Color rowColor = new Color(0.16f, 0.18f, 0.24f, 1f);
    [SerializeField] private Color ownRowColor = new Color(1f, 0.55f, 0.22f, 1f);

    [Tooltip("Seconds between reload attempts while offline.")]
    [SerializeField] private float retrySeconds = 5f;

    private readonly List<GameObject> rows = new List<GameObject>();
    private int request;
    private bool loading;
    private bool loaded;
    private float retryTimer;

    private void OnEnable()
    {
        loaded = false;

        // This phone's cached best shows right away; everyone else's comes from the server.
        RunResult local = HighscoreService.LocalBest;
        ShowRows(local != null ? new List<RunResult> { local } : new List<RunResult>(), false);
        SetStatus("LOADING...");
        Load();
    }

    private void Update()
    {
        if (loaded || loading) return;

        retryTimer -= Time.unscaledDeltaTime;
        if (retryTimer <= 0f) Load();
    }

    public void Close() => gameObject.SetActive(false);

    private void Load()
    {
        loading = true;

        // Ignores answers from an earlier open if the panel was closed and reopened meanwhile.
        int id = ++request;
        HighscoreService.FetchTop(count, results =>
        {
            if (this == null || !isActiveAndEnabled || id != request) return;
            loading = false;

            if (results == null)
            {
                retryTimer = retrySeconds;
                SetStatus(rows.Count > 0 ? "OFFLINE - SHOWING YOUR BEST" : "OFFLINE - WAITING FOR CONNECTION");
                return;
            }

            loaded = true;
            ShowRows(results, true);
            SetStatus(results.Count == 0 ? "NO SCORES YET" : "");
        });
    }

    /// <summary>Ranked rows come from the server; the offline row has no known rank.</summary>
    private void ShowRows(List<RunResult> results, bool ranked)
    {
        ClearRows();
        for (int i = 0; i < results.Count; i++)
        {
            RunResult run = results[i];
            GameObject row = Instantiate(rowTemplate, content);
            row.SetActive(true);
            rows.Add(row);

            bool own = run.playerId == HighscoreService.PlayerId;
            if (row.TryGetComponent(out Image background)) background.color = own ? ownRowColor : rowColor;

            SetText(row, "Rank", ranked ? $"#{i + 1}" : "-");
            SetText(row, "Name", own ? $"{run.name} (YOU)" : run.name);
            SetText(row, "Wave", $"WAVE {run.wave}");
            SetText(row, "Details",
                $"{run.SurvivedText}   {run.kills} KILLS   {run.coins} COINS   {run.upgrades} UPGRADES   LV {run.level}");
        }
    }

    private void ClearRows()
    {
        foreach (GameObject row in rows) Destroy(row);
        rows.Clear();
    }

    private void SetStatus(string text)
    {
        if (statusText == null) return;
        statusText.text = text;
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    private static void SetText(GameObject row, string child, string text)
    {
        Transform target = row.transform.Find(child);
        if (target != null && target.TryGetComponent(out TextMeshProUGUI label)) label.text = text;
    }
}
