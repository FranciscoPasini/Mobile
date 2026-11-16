using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Owns the GameAudio mixer (Music and SFX groups) and plays pooled 3D sound effects through the SFX group.
/// Scene music sources are routed to the Music group in the scene itself.
/// </summary>
public class GameAudio : MonoBehaviour
{
    private const string MixerPath = "Audio/GameAudio";
    private const float MutedDb = -80f;
    private const int PoolSize = 16;

    private static GameAudio instance;
    private static AudioMixer mixer;
    private static AudioMixerGroup sfxGroup;
    private static AudioMixerGroup musicGroup;

    private readonly AudioSource[] pool = new AudioSource[PoolSize];
    private int next;

    public static AudioMixerGroup SfxGroup { get { LoadMixer(); return sfxGroup; } }
    public static AudioMixerGroup MusicGroup { get { LoadMixer(); return musicGroup; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (instance != null) return;

        var go = new GameObject("Game Audio");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<GameAudio>();
    }

    private void Awake()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            var child = new GameObject("Sfx");
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.outputAudioMixerGroup = SfxGroup;
            pool[i] = source;
        }
    }

    // Mixer values set in Awake don't stick, so the saved mutes are applied here.
    private void Start()
    {
        ApplyMutes();
    }

    /// <summary>
    /// 3D one-shot through the SFX group. Drop-in for AudioSource.PlayClipAtPoint.
    /// </summary>
    public static void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        if (instance == null)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
            return;
        }

        // Round robin, so the oldest sound gets cut when the pool is busy.
        AudioSource source = instance.pool[instance.next];
        instance.next = (instance.next + 1) % PoolSize;
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.Play();
    }

    public static void ApplyMutes()
    {
        LoadMixer();
        if (mixer == null) return;

        mixer.SetFloat("MusicVolume", GameSettings.MusicMuted ? MutedDb : 0f);
        mixer.SetFloat("SfxVolume", GameSettings.SfxMuted ? MutedDb : 0f);
    }

    private static void LoadMixer()
    {
        if (mixer != null) return;

        mixer = Resources.Load<AudioMixer>(MixerPath);
        if (mixer == null)
        {
            Debug.LogError($"GameAudio: mixer not found at Resources/{MixerPath}.");
            return;
        }

        AudioMixerGroup[] music = mixer.FindMatchingGroups("Music");
        AudioMixerGroup[] sfx = mixer.FindMatchingGroups("SFX");
        musicGroup = music.Length > 0 ? music[0] : null;
        sfxGroup = sfx.Length > 0 ? sfx[0] : null;
    }
}
