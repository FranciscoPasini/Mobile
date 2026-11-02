using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Damages, stuns, and knocks back living enemies around the player, and flashes the screen.
/// </summary>
public class OneTimePickup_Shockwave : Base_OneTimePickup
{
    [Header("Screen Flash")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.75f);
    [SerializeField] private float flashDuration = 0.4f;

    private readonly Collider[] hits = new Collider[32];
    private readonly List<Base_Enemy> hitEnemies = new List<Base_Enemy>(16);

    // Shared by all shockwave pickups; recreated after a scene reload.
    private static ScreenFlash screenFlash;

    protected override void ApplyEffect()
    {
        OneTimePickup_Shockwave_Data blastData = pickupData as OneTimePickup_Shockwave_Data;
        float radius = blastData != null ? blastData.Radius : 8f;
        int wave = GetCurrentWave();
        float damage = blastData != null ? blastData.GetDamage(wave) : 8f;
        float stun = blastData != null ? blastData.StunDuration : 1.5f;
        float knockback = blastData != null ? blastData.Knockback : 1.2f;
        LayerMask mask = blastData != null ? blastData.HitMask : (LayerMask)~0;

        Vector3 origin = PlayerTransform != null ? PlayerTransform.position : transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, radius, hits, mask, QueryTriggerInteraction.Collide);

        hitEnemies.Clear();
        for (int i = 0; i < count; i++)
        {
            Base_Enemy enemy = hits[i] != null ? hits[i].GetComponentInParent<Base_Enemy>() : null;
            if (enemy == null || !enemy.IsAlive || hitEnemies.Contains(enemy)) continue;
            hitEnemies.Add(enemy);
        }

        for (int i = 0; i < hitEnemies.Count; i++)
        {
            Base_Enemy enemy = hitEnemies[i];
            if (damage > 0f) enemy.TakeDamage(damage);
            if (!enemy.IsAlive) continue;
            enemy.Knockback(origin, knockback, stun);
        }

        FlashScreen();
    }

    private static int GetCurrentWave()
    {
        TownManager townManager = FindFirstObjectByType<TownManager>();
        return townManager != null ? Mathf.Max(1, townManager.Wave) : 1;
    }

    private void FlashScreen()
    {
        // The pickup is disabled right after this, so the fade runs on its own overlay object.
        if (screenFlash == null)
        {
            var root = new GameObject("ShockwaveScreenFlash");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            var imageObject = new GameObject("Flash", typeof(RectTransform));
            imageObject.transform.SetParent(root.transform, false);
            var rect = (RectTransform)imageObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;

            screenFlash = root.AddComponent<ScreenFlash>();
            screenFlash.image = image;
        }

        screenFlash.Play(flashColor, flashDuration);
    }

    private class ScreenFlash : MonoBehaviour
    {
        public Image image;

        private Color color;
        private float duration;
        private float time = float.MaxValue;

        public void Play(Color flashColor, float flashDuration)
        {
            color = flashColor;
            duration = Mathf.Max(0.01f, flashDuration);
            time = 0f;
            image.enabled = true;
        }

        private void Update()
        {
            if (!image.enabled) return;

            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            // Fast fall-off reads as a bright pop rather than a slow fade.
            float alpha = color.a * (1f - t) * (1f - t);
            image.color = new Color(color.r, color.g, color.b, alpha);

            if (t >= 1f) image.enabled = false;
        }
    }
}
