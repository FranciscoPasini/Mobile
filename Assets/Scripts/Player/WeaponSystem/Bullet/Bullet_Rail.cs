using UnityEngine;

/// <summary>
/// Hitscan beam: damages and pierces enemies along a line, then draws a short trail.
/// Drop this on the railgun bullet prefab. Add a LineRenderer if you want to style the beam.
/// </summary>
public class Bullet_Rail : Base_Bullet
{
    [SerializeField, Min(1)] private int maxPierce = 8;
    [SerializeField, Min(1f)] private float maxDistance = 40f;
    [SerializeField, Min(0.02f)] private float trailDuration = 0.12f;
    [SerializeField] private float trailWidth = 0.12f;
    [SerializeField] private Color trailColor = new Color(0.4f, 0.9f, 1f, 1f);
    [SerializeField] private LayerMask hitMask = ~0;

    private LineRenderer trail;
    private bool fired;
    private float trailTimer;
    private Color startColor;
    private Color endColor;

    protected override void OnInitialized()
    {
        fired = false;
        trailTimer = 0f;
        FireBeam();
    }

    protected override void Update()
    {
        if (!fired) return;

        trailTimer += Time.deltaTime;
        float t = trailDuration > 0f ? trailTimer / trailDuration : 1f;
        if (trail != null)
        {
            float fade = Mathf.Clamp01(1f - t);
            Color fadedStart = startColor;
            Color fadedEnd = endColor;
            fadedStart.a *= fade;
            fadedEnd.a *= fade;
            trail.startColor = fadedStart;
            trail.endColor = fadedEnd;
        }

        if (trailTimer >= trailDuration)
        {
            DestroyBullet();
        }
    }

    protected override void MoveBullet() { }

    protected override void OnTriggerEnter(Collider other) { }

    private void FireBeam()
    {
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        float distance = maxDistance;
        Vector3 end = origin + direction * distance;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxDistance, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        int pierced = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col.CompareTag("Player")) continue;

            if (TryGetEnemy(col, out _))
            {
                DealDamage(col);
                pierced++;
                if (pierced >= maxPierce)
                {
                    end = hits[i].point;
                    break;
                }
                continue;
            }

            // Solid world geometry stops the beam.
            if (!col.isTrigger)
            {
                end = hits[i].point;
                break;
            }
        }

        DrawTrail(origin, end);
        fired = true;
    }

    private void DrawTrail(Vector3 origin, Vector3 end)
    {
        trail = GetComponent<LineRenderer>();
        if (trail == null)
        {
            trail = gameObject.AddComponent<LineRenderer>();
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.useWorldSpace = true;
            trail.positionCount = 2;
            trail.numCapVertices = 4;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) trail.material = new Material(shader);
        }

        trail.enabled = true;
        trail.useWorldSpace = true;
        trail.positionCount = 2;
        trail.SetPosition(0, origin);
        trail.SetPosition(1, end);
        trail.startWidth = trailWidth;
        trail.endWidth = trailWidth * 0.4f;
        startColor = trailColor;
        endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0.15f);
        trail.startColor = startColor;
        trail.endColor = endColor;
    }
}
