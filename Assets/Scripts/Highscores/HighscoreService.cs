using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Firebase leaderboard. Each phone signs in anonymously and gets its own permanent id, which is the
/// id of its document in the "scores" collection. Only the phone's best run is stored.
/// The best run is cached on the phone and queued for upload; while it's unsent, sign-in and upload
/// are retried every few seconds until there's a connection.
/// </summary>
public static class HighscoreService
{
    private const string Collection = "scores";
    private const string BestKey = "Highscore.Best";
    private const string PendingKey = "Highscore.Pending";
    private const float RetrySeconds = 5f;

    private static bool ready;
    private static bool starting;
    private static bool uploading;
    private static float nextRetry;
    private static Action<bool> onReady;

    /// <summary>This phone's id. Empty until signed in at least once.</summary>
    public static string PlayerId { get; private set; } = "";

    /// <summary>True while a better run is waiting to be uploaded.</summary>
    public static bool HasPendingUpload => PlayerPrefs.GetInt(PendingKey, 0) == 1;

    public static RunResult LocalBest
    {
        get
        {
            string json = PlayerPrefs.GetString(BestKey, "");
            if (string.IsNullOrEmpty(json)) return null;

            RunResult best = JsonUtility.FromJson<RunResult>(json);
            best.playerId = PlayerId;
            best.name = DisplayName;
            return best;
        }
    }

    /// <summary>Stable name from the phone's id, like "Camper4821".</summary>
    public static string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(PlayerId)) return "Camper";

            int hash = 17;
            foreach (char c in PlayerId) hash = unchecked(hash * 31 + c);
            return "Camper" + (Mathf.Abs(hash) % 10000).ToString("0000");
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        var go = new GameObject("Highscore Service");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<Runner>();
        Init(null);
    }

    /// <summary>
    /// Connects and signs in once. The callback gets false if Firebase isn't reachable.
    /// </summary>
    public static void Init(Action<bool> done)
    {
        if (ready)
        {
            done?.Invoke(true);
            return;
        }

        if (done != null) onReady += done;
        if (starting) return;
        starting = true;

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(dependencies =>
        {
            if (dependencies.IsFaulted || dependencies.Result != DependencyStatus.Available)
            {
                Fail("Firebase dependencies not available");
                return;
            }

            // Signed in before: the id is cached on the phone, so this works offline too.
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser != null)
            {
                SignedIn(auth.CurrentUser.UserId);
                return;
            }

            auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(signIn =>
            {
                if (signIn.IsFaulted || signIn.IsCanceled)
                {
                    Fail("anonymous sign-in failed: " + signIn.Exception?.GetBaseException().Message);
                    return;
                }
                SignedIn(signIn.Result.User.UserId);
            });
        });
    }

    /// <summary>
    /// Caches the run if it beats this phone's best and queues it for upload. False if it isn't a new best.
    /// </summary>
    public static bool SubmitRun(RunResult run)
    {
        if (run == null || !run.IsBetterThan(LocalBest)) return false;

        PlayerPrefs.SetString(BestKey, JsonUtility.ToJson(run));
        PlayerPrefs.SetInt(PendingKey, 1);
        PlayerPrefs.Save();

        Init(ok => { if (ok) Upload(); });
        return true;
    }

    /// <summary>
    /// Top runs from the server, ordered by wave. The callback gets null when offline.
    /// Other players' scores are never cached, only this phone's best.
    /// </summary>
    public static void FetchTop(int count, Action<List<RunResult>> done)
    {
        Init(ok =>
        {
            if (!ok)
            {
                done(null);
                return;
            }

            // Server only, so the Firestore offline cache never serves other players' scores.
            // Ordered by wave only, so no composite index is needed. Ties are sorted below.
            FirebaseFirestore.DefaultInstance.Collection(Collection)
                .OrderByDescending("wave").Limit(count)
                .GetSnapshotAsync(Source.Server).ContinueWithOnMainThread(query =>
                {
                    if (query.IsFaulted || query.IsCanceled)
                    {
                        Debug.LogWarning("Leaderboard: load failed: " + query.Exception?.GetBaseException().Message);
                        done(null);
                        return;
                    }

                    var results = new List<RunResult>();
                    foreach (DocumentSnapshot doc in query.Result.Documents)
                    {
                        results.Add(new RunResult
                        {
                            playerId = doc.Id,
                            name = doc.TryGetValue("name", out string name) ? name : "Camper",
                            wave = GetInt(doc, "wave"),
                            survivedSeconds = GetInt(doc, "survivedSeconds"),
                            coins = GetInt(doc, "coins"),
                            kills = GetInt(doc, "kills"),
                            upgrades = GetInt(doc, "upgrades"),
                            level = GetInt(doc, "level"),
                        });
                    }

                    MergeLocalBest(results, count);
                    done(results);
                });
        });
    }

    /// <summary>
    /// If this phone's best isn't on the server yet, shows it in place of the older server entry.
    /// </summary>
    private static void MergeLocalBest(List<RunResult> results, int count)
    {
        RunResult local = LocalBest;
        if (local != null && HasPendingUpload)
        {
            results.RemoveAll(r => r.playerId == PlayerId);
            results.Add(local);
        }

        results.Sort((a, b) => a.IsBetterThan(b) ? -1 : b.IsBetterThan(a) ? 1 : 0);
        if (results.Count > count) results.RemoveRange(count, results.Count - count);
    }

    private static void SignedIn(string uid)
    {
        PlayerId = uid;
        ready = true;
        starting = false;

        if (HasPendingUpload) Upload();

        Action<bool> callbacks = onReady;
        onReady = null;
        callbacks?.Invoke(true);
    }

    private static void Fail(string reason)
    {
        Debug.LogWarning("Leaderboard: " + reason);
        starting = false;

        Action<bool> callbacks = onReady;
        onReady = null;
        callbacks?.Invoke(false);
    }

    private static void Upload()
    {
        RunResult best = LocalBest;
        if (best == null || uploading) return;

        // Offline, Firestore holds the write and sends it when the connection returns.
        uploading = true;
        FirebaseFirestore.DefaultInstance.Collection(Collection).Document(PlayerId)
            .SetAsync(best.ToDictionary()).ContinueWithOnMainThread(write =>
            {
                uploading = false;
                if (write.IsFaulted || write.IsCanceled)
                {
                    // Denied means the server already has a better run, so retrying won't help.
                    var error = write.Exception?.GetBaseException() as FirestoreException;
                    if (error != null && error.ErrorCode == FirestoreError.PermissionDenied) ClearPending();
                    Debug.LogWarning("Leaderboard: upload failed: " + write.Exception?.GetBaseException().Message);
                    return;
                }
                ClearPending();
            });
    }

    private static void ClearPending()
    {
        PlayerPrefs.SetInt(PendingKey, 0);
        PlayerPrefs.Save();
    }

    /// <summary>Retries sign-in and the queued upload once there's a connection.</summary>
    private static void Retry()
    {
        if (Time.unscaledTime < nextRetry) return;
        nextRetry = Time.unscaledTime + RetrySeconds;

        if (Application.internetReachability == NetworkReachability.NotReachable) return;
        if (!ready) Init(null);
        else if (HasPendingUpload) Upload();
    }

    private static int GetInt(DocumentSnapshot doc, string field)
    {
        return doc.TryGetValue(field, out long value) ? (int)value : 0;
    }

    private class Runner : MonoBehaviour
    {
        private void Update() => Retry();
    }
}
