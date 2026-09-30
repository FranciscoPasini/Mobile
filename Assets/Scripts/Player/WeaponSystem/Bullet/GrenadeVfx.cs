using UnityEngine;

/// <summary>
/// Runtime particle burst for the grenade explosion.
/// Built in code so it does not depend on a ParticleSystem prefab.
/// </summary>
public static class GrenadeVfx
{
    public static void SpawnExplosion(Vector3 origin, float radius)
    {
        float scale = Mathf.Max(0.4f, radius / 3.5f);
        GameObject root = new GameObject("GrenadeExplosion");
        root.transform.position = origin;

        CreateBurst(
            root.transform,
            "Fire",
            count: 28,
            lifetime: new Vector2(0.22f, 0.4f),
            speed: new Vector2(2.5f, 6.5f) * scale,
            size: new Vector2(0.22f, 0.5f) * scale,
            startA: new Color(1f, 0.85f, 0.3f, 1f),
            startB: new Color(1f, 0.35f, 0.05f, 1f),
            endColor: new Color(0.25f, 0.05f, 0.01f, 0f),
            shapeRadius: 0.15f * scale,
            gravity: 0.2f,
            additive: true);

        CreateBurst(
            root.transform,
            "Sparks",
            count: 18,
            lifetime: new Vector2(0.18f, 0.35f),
            speed: new Vector2(6f, 12f) * scale,
            size: new Vector2(0.04f, 0.08f) * scale,
            startA: new Color(1f, 0.95f, 0.6f, 1f),
            startB: new Color(1f, 0.55f, 0.1f, 1f),
            endColor: new Color(1f, 0.2f, 0f, 0f),
            shapeRadius: 0.08f * scale,
            gravity: 1.4f,
            additive: true);

        CreateBurst(
            root.transform,
            "Smoke",
            count: 14,
            lifetime: new Vector2(0.5f, 0.85f),
            speed: new Vector2(0.6f, 1.8f) * scale,
            size: new Vector2(0.45f, 0.9f) * scale,
            startA: new Color(0.28f, 0.26f, 0.24f, 0.55f),
            startB: new Color(0.18f, 0.17f, 0.16f, 0.4f),
            endColor: new Color(0.12f, 0.12f, 0.12f, 0f),
            shapeRadius: 0.25f * scale,
            gravity: -0.25f,
            additive: false);

        Object.Destroy(root, 1.2f);
    }

    private static void CreateBurst(
        Transform parent,
        string name,
        short count,
        Vector2 lifetime,
        Vector2 speed,
        Vector2 size,
        Color startA,
        Color startB,
        Color endColor,
        float shapeRadius,
        float gravity,
        bool additive)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.15f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new ParticleSystem.MinMaxGradient(startA, startB);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravity;
        main.maxParticles = count + 4;
        main.stopAction = ParticleSystemStopAction.None;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = shapeRadius;
        shape.radiusThickness = 1f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startA, 0f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startA.a, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, additive ? 0.2f : 1.4f));

        ApplyParticleMaterial(go.GetComponent<ParticleSystemRenderer>(), additive);
        ps.Play();
    }

    private static Material additiveParticleMaterial;
    private static Material alphaParticleMaterial;

    private static void ApplyParticleMaterial(ParticleSystemRenderer renderer, bool additive)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = additive ? GetAdditiveMaterial() : GetAlphaMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Material GetAdditiveMaterial()
    {
        if (additiveParticleMaterial == null)
        {
            additiveParticleMaterial = CreateParticleMaterial(true);
        }

        return additiveParticleMaterial;
    }

    private static Material GetAlphaMaterial()
    {
        if (alphaParticleMaterial == null)
        {
            alphaParticleMaterial = CreateParticleMaterial(false);
        }

        return alphaParticleMaterial;
    }

    private static Material CreateParticleMaterial(bool additive)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.SetTexture("_MainTex", Texture2D.whiteTexture);
        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetColor("_Color", Color.white);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 1f : 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cutoff", 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", additive
            ? (int)UnityEngine.Rendering.BlendMode.One
            : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        return material;
    }
}
