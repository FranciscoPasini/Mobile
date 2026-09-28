using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

/// <summary>
/// Shakes the camera and flashes a red vignette when the town takes damage.
/// Goes on the Main Camera. The CinemachineBrain overwrites the camera transform every frame,
/// so the shake goes through Cinemachine impulses instead of moving the camera directly.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Found in the scene if left empty.")]
    [SerializeField] private TownManager townManager;

    [Header("Shake")]
    [SerializeField] private bool shakeEnabled = true;
    [SerializeField] private CinemachineImpulseDefinition.ImpulseShapes shakeShape = CinemachineImpulseDefinition.ImpulseShapes.Explosion;
    [SerializeField] private float shakeDuration = 0.25f;
    [Tooltip("Shake strength for a hit of Reference Damage.")]
    [SerializeField] private float shakeForce = 0.4f;
    [Tooltip("Hits bigger or smaller than this scale the shake, clamped between the two multipliers below.")]
    [SerializeField] private float referenceDamage = 5f;
    [SerializeField] private float minDamageMultiplier = 0.5f;
    [SerializeField] private float maxDamageMultiplier = 2f;
    [Tooltip("Hits closer together than this don't start a new shake, so a crowd of enemies doesn't stack into a huge one.")]
    [SerializeField] private float minTimeBetweenShakes = 0.1f;
    [Tooltip("Keep shaking while the game is paused, for example on the game over screen.")]
    [SerializeField] private bool ignoreTimeScale = true;

    [Header("Red Flash")]
    [SerializeField] private bool flashEnabled = true;
    [SerializeField] private Color flashColor = new Color(1f, 0f, 0f, 1f);
    [Tooltip("Vignette strength at the peak of the flash.")]
    [Range(0f, 1f)] [SerializeField] private float flashIntensity = 0.35f;
    [Range(0.01f, 1f)] [SerializeField] private float flashSmoothness = 0.6f;
    [SerializeField] private float flashDuration = 0.35f;
    [Tooltip("The vignette needs post-processing on this camera. Turn off if you manage that yourself.")]
    [SerializeField] private bool enablePostProcessingOnCamera = true;

    private CinemachineImpulseSource impulseSource;
    private Volume flashVolume;
    private VolumeProfile flashProfile;
    private float flashTimer;
    private float lastShakeTime = float.NegativeInfinity;

    private void Awake()
    {
        if (townManager == null)
        {
            townManager = FindFirstObjectByType<TownManager>();
        }

        if (townManager == null)
        {
            Debug.LogError($"{name}: CameraShake found no TownManager to listen to.", this);
        }

        SetUpImpulseSource();
        EnsureListenersOnCameras();

        if (flashEnabled)
        {
            SetUpFlashVolume();
        }
    }

    private void OnEnable()
    {
        if (townManager != null) townManager.OnTownDamageTaken += HandleTownDamaged;
    }

    private void OnDisable()
    {
        if (townManager != null) townManager.OnTownDamageTaken -= HandleTownDamaged;
    }

    private void OnDestroy()
    {
        if (flashProfile != null)
        {
            Destroy(flashProfile);
        }
    }

    private void Update()
    {
        if (flashVolume == null || flashTimer <= 0f) return;

        // Unscaled so the flash still fades out if the killing hit pauses the game.
        flashTimer = Mathf.Max(0f, flashTimer - Time.unscaledDeltaTime);
        float t = flashTimer / Mathf.Max(flashDuration, 0.0001f);
        flashVolume.weight = t * t;
    }

    private void HandleTownDamaged(float damage)
    {
        if (shakeEnabled) Shake(damage);
        if (flashVolume != null) Flash();
    }

    public void Shake(float damage)
    {
        if (impulseSource == null) return;
        if (Time.unscaledTime - lastShakeTime < minTimeBetweenShakes) return;
        lastShakeTime = Time.unscaledTime;

        float multiplier = Mathf.Clamp(damage / Mathf.Max(referenceDamage, 0.0001f), minDamageMultiplier, maxDamageMultiplier);
        // A random direction each hit so repeated shakes don't all push the same way.
        impulseSource.GenerateImpulseWithVelocity(Random.onUnitSphere * (shakeForce * multiplier));
    }

    public void Flash()
    {
        if (flashVolume == null) return;

        flashTimer = flashDuration;
        flashVolume.weight = 1f;
    }

    private void SetUpImpulseSource()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
        {
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
        }

        // Set every field explicitly: a source added from code skips the Editor-only Reset() defaults.
        impulseSource.ImpulseDefinition = new CinemachineImpulseDefinition
        {
            ImpulseChannel = 1,
            ImpulseShape = shakeShape,
            CustomImpulseShape = new AnimationCurve(),
            ImpulseDuration = shakeDuration,
            // Uniform ignores distance, so the shake is the same wherever the camera is relative to the town.
            ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            DissipationDistance = 100f,
            DissipationRate = 0.25f,
            PropagationSpeed = 343f,
        };
        impulseSource.ImpulseDefinition.OnValidate();

        CinemachineImpulseManager.Instance.IgnoreTimeScale = ignoreTimeScale;
    }

    private void EnsureListenersOnCameras()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (cameras.Length == 0)
        {
            Debug.LogWarning($"{name}: no CinemachineCamera in the scene, nothing will receive the shake.", this);
            return;
        }

        foreach (CinemachineCamera cinemachineCamera in cameras)
        {
            // Listeners already set up in the Inspector are left as they are.
            if (cinemachineCamera.GetComponent<CinemachineImpulseListener>() != null) continue;

            CinemachineImpulseListener listener = cinemachineCamera.gameObject.AddComponent<CinemachineImpulseListener>();
            // Reset() only runs in the Editor, so a listener added at runtime starts with Gain 0 and no channels.
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.Use2DDistance = false;
            listener.UseCameraSpace = true;
            listener.SignalCombinationMode = CinemachineImpulseListener.SignalCombinationModes.Additive;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                AmplitudeGain = 1f,
                FrequencyGain = 1f,
                Duration = 1f,
            };
        }
    }

    private void SetUpFlashVolume()
    {
        if (enablePostProcessingOnCamera)
        {
            GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        flashProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        Vignette vignette = flashProfile.Add<Vignette>(true);
        vignette.color.Override(flashColor);
        vignette.intensity.Override(flashIntensity);
        vignette.smoothness.Override(flashSmoothness);

        // Its own child object so it can't clash with any Volume you add to the camera later.
        GameObject volumeObject = new GameObject("DamageFlashVolume");
        volumeObject.transform.SetParent(transform, false);

        flashVolume = volumeObject.AddComponent<Volume>();
        flashVolume.isGlobal = true;
        // High priority so it wins over any scene vignette while the flash is visible.
        flashVolume.priority = 100f;
        flashVolume.sharedProfile = flashProfile;
        flashVolume.weight = 0f;
    }
}
