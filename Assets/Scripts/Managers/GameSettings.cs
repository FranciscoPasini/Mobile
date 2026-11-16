using System;
using UnityEngine;

/// <summary>
/// Player settings saved in PlayerPrefs: control mode and music/sfx mute.
/// </summary>
public static class GameSettings
{
    private const string GyroKey = "Settings.UseGyro";
    private const string MusicKey = "Settings.MusicMuted";
    private const string SfxKey = "Settings.SfxMuted";

    /// <summary>Fired after any setting changes.</summary>
    public static event Action Changed;

    public static bool UseGyro
    {
        get => PlayerPrefs.GetInt(GyroKey, 0) == 1;
        set => Save(GyroKey, value);
    }

    public static bool MusicMuted
    {
        get => PlayerPrefs.GetInt(MusicKey, 0) == 1;
        set => Save(MusicKey, value);
    }

    public static bool SfxMuted
    {
        get => PlayerPrefs.GetInt(SfxKey, 0) == 1;
        set => Save(SfxKey, value);
    }

    private static void Save(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
        GameAudio.ApplyMutes();
        Changed?.Invoke();
    }
}
