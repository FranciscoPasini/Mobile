using UnityEngine;

/// <summary>
/// Short cone burst at a fire point. Can sit on a transform, or be played anywhere with PlayAt.
/// </summary>
public class MuzzleFlashEmitter : MonoBehaviour
{
    [SerializeField] private Color flashColor = new Color(1f, 0.75f, 0.25f, 1f);
    [SerializeField] private float size = 0.22f;

    private ParticleSystem flash;
    private static MuzzleFlashEmitter shared;

    private void Awake()
    {
        flash = Build();
    }

    public void Play()
    {
        if (flash == null) flash = Build();
        flash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        flash.Play();
    }

    public void Play(Vector3 origin, Vector3 direction)
    {
        transform.SetPositionAndRotation(origin, Quaternion.LookRotation(
            direction.sqrMagnitude > 0.0001f ? direction : Vector3.forward));
        Play();
    }

    /// <summary>
    /// One shared emitter for weapons that are not scene objects (player Base_Weapon).
    /// </summary>
    public static void PlayAt(Vector3 origin, Vector3 direction)
    {
        if (shared == null)
        {
            var go = new GameObject("MuzzleFlash");
            shared = go.AddComponent<MuzzleFlashEmitter>();
        }

        shared.Play(origin, direction);
    }

    private ParticleSystem Build()
    {
        ParticleSystem existing = GetComponent<ParticleSystem>();
        ParticleSystem ps = existing != null ? existing : gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.08f;
        main.startLifetime = 0.08f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = flashColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0f;
        main.maxParticles = 18;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.02f;
        shape.length = 0.1f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(flashColor, 0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        SimpleFx.ApplyMaterial(ps.GetComponent<ParticleSystemRenderer>(), true);
        return ps;
    }
}
