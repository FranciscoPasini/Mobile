using UnityEngine;

/// <summary>
/// Slow spin, bob, and a point-light glow used by weapon and one-time pickups after they land.
/// </summary>
public static class PickupVisual
{
    public static Light EnsureGlow(Transform parent, Color color)
    {
        Transform existing = parent.Find("PickupGlow");
        Light light = existing != null ? existing.GetComponent<Light>() : null;
        if (light == null)
        {
            var go = new GameObject("PickupGlow");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.35f;
            light = go.AddComponent<Light>();
        }

        light.type = LightType.Point;
        light.color = color;
        light.intensity = 1.4f;
        light.range = 2.2f;
        light.shadows = LightShadows.None;
        return light;
    }

    public static void HoverSpin(Transform target, Vector3 restPosition, float spinSpeed, float hoverHeight, float hoverSpeed, float time)
    {
        target.position = restPosition + Vector3.up * (Mathf.Sin(time * hoverSpeed) * hoverHeight);
        if (spinSpeed != 0f) target.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    public static void SetGlowVisible(Light glow, bool visible)
    {
        if (glow != null && glow.enabled != visible) glow.enabled = visible;
    }
}
