using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-shot particle bursts built in code, same approach as GrenadeVfx.
/// </summary>
public static class SimpleFx
{
    public static void DeathBurst(Vector3 origin)
    {
        GameObject root = CreateRoot("DeathBurst", origin);

        Burst(root.transform, "Sparks", 16, new Vector2(0.18f, 0.32f), new Vector2(3f, 7f),
            new Vector2(0.05f, 0.1f), new Color(1f, 0.55f, 0.2f), new Color(1f, 0.2f, 0.05f),
            new Color(0.2f, 0.05f, 0f, 0f), 0.1f, 1.2f, true);

        Burst(root.transform, "Puff", 8, new Vector2(0.25f, 0.45f), new Vector2(0.6f, 1.6f),
            new Vector2(0.25f, 0.5f), new Color(0.25f, 0.2f, 0.18f, 0.6f), new Color(0.15f, 0.12f, 0.1f, 0.4f),
            new Color(0.1f, 0.1f, 0.1f, 0f), 0.2f, -0.15f, false);

        Object.Destroy(root, 0.8f);
    }

    public static void BuyBurst(Vector3 origin)
    {
        GameObject root = CreateRoot("BuyBurst", origin + Vector3.up * 0.4f);

        Burst(root.transform, "Gold", 20, new Vector2(0.25f, 0.45f), new Vector2(2f, 5f),
            new Vector2(0.08f, 0.16f), new Color(1f, 0.9f, 0.35f), new Color(1f, 0.6f, 0.15f),
            new Color(1f, 0.4f, 0f, 0f), 0.25f, 0.4f, true);

        Burst(root.transform, "Ring", 12, new Vector2(0.3f, 0.5f), new Vector2(1.2f, 2.4f),
            new Vector2(0.18f, 0.35f), new Color(1f, 0.85f, 0.4f, 0.8f), new Color(1f, 0.7f, 0.2f, 0.5f),
            new Color(1f, 0.5f, 0.1f, 0f), 0.4f, 0f, true);

        Object.Destroy(root, 0.9f);
    }

    public static void Burst(
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
        main.duration = 0.12f;
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

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(startA, 0f), new GradientColorKey(endColor, 1f) },
            new[] { new GradientAlphaKey(startA.a, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLife = ps.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, additive ? 0.15f : 1.3f));

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        ApplyMaterial(renderer, additive);
        if (renderer != null && renderer.sharedMaterial == null) return;
        ps.Play();
    }

    public static void ApplyMaterial(ParticleSystemRenderer renderer, bool additive)
    {
        if (renderer == null) return;

        renderer.sharedMaterial = additive ? GetAdditiveMaterial() : GetAlphaMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static GameObject CreateRoot(string name, Vector3 origin)
    {
        var root = new GameObject(name);
        root.transform.position = origin;
        return root;
    }

    private static Material additiveMaterial;
    private static Material alphaMaterial;

    public static Material GetAdditiveMaterial()
    {
        if (additiveMaterial == null) additiveMaterial = CreateMaterial(true);
        return additiveMaterial;
    }

    public static Material GetAlphaMaterial()
    {
        if (alphaMaterial == null) alphaMaterial = CreateMaterial(false);
        return alphaMaterial;
    }

    private static Material CreateMaterial(bool additive)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("SimpleFx: no particle shader found, skipping burst.");
            return null;
        }

        Material material = new Material(shader);
        material.SetTexture("_MainTex", Texture2D.whiteTexture);
        material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        material.SetColor("_Color", Color.white);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", additive ? 1f : 0f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", additive
            ? (int)UnityEngine.Rendering.BlendMode.One
            : (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
        return material;
    }
}
