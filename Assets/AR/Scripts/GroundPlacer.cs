using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

public class GroundPlacer : MonoBehaviour
{
    [SerializeField] ObjectSelector selector;
    [SerializeField] GameObject[] prefabs;
    [SerializeField] PlaneFinderBehaviour groundPlaneFinder;
    [SerializeField] GameObject previewGhost;
    [SerializeField] Color editingColor = new Color(1f, 0.85f, 0.15f, 1f);

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    bool hasHit;
    Pose hitPose;
    GameObject ghost;
    readonly List<GameObject> spawned = new List<GameObject>();
    readonly Dictionary<GameObject, Vector3> baseScales = new Dictionary<GameObject, Vector3>();
    GameObject lastSpawned;
    int lastPrefabIndex = -1;

    void OnEnable()
    {
        if (groundPlaneFinder != null)
        {
            groundPlaneFinder.OnAutomaticHitTest.AddListener(OnAutomaticHitTest);
            groundPlaneFinder.OnInteractiveHitTest.AddListener(OnInteractiveHitTest);
        }

        if (selector != null)
        {
            selector.OnSelectionChanged += ReplaceLastWithSelection;
            selector.OnClearAll += ClearAll;
            selector.OnScaleChanged += ApplyScaleToAll;
        }
    }

    void OnDisable()
    {
        if (groundPlaneFinder != null)
        {
            groundPlaneFinder.OnAutomaticHitTest.RemoveListener(OnAutomaticHitTest);
            groundPlaneFinder.OnInteractiveHitTest.RemoveListener(OnInteractiveHitTest);
        }

        if (selector != null)
        {
            selector.OnSelectionChanged -= ReplaceLastWithSelection;
            selector.OnClearAll -= ClearAll;
            selector.OnScaleChanged -= ApplyScaleToAll;
        }
    }

    // Runs every frame while the screen center points at a plane, drives the preview.
    void OnAutomaticHitTest(HitTestResult result)
    {
        hasHit = result != null;
        if (!hasHit) return;
        hitPose = new Pose(result.Position, result.Rotation);
    }

    // Vuforia calls this when the user taps the screen on a plane.
    void OnInteractiveHitTest(HitTestResult result)
    {
        if (result == null || selector == null || IsPointerOverUI()) return;
        PlaceSelected(new Pose(result.Position, result.Rotation));
    }

    void Update()
    {
        if (selector == null) return;
        UpdateGhost();
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    void PlaceSelected(Pose pose)
    {
        int idx = selector.SelectedIndex;
        if (!IsValidIndex(idx)) return;
        SpawnAt(prefabs[idx], pose, idx);
    }

    void ReplaceLastWithSelection(int idx)
    {
        if (lastSpawned == null || !IsValidIndex(idx) || idx == lastPrefabIndex) return;

        Pose pose = new Pose(lastSpawned.transform.position, lastSpawned.transform.rotation);
        spawned.Remove(lastSpawned);
        baseScales.Remove(lastSpawned);
        Destroy(lastSpawned);
        lastSpawned = null;
        SpawnAt(prefabs[idx], pose, idx);
    }

    void SpawnAt(GameObject prefab, Pose pose, int idx)
    {
        SetEditing(lastSpawned, false);

        GameObject instance = Instantiate(prefab, pose.position, pose.rotation);
        baseScales[instance] = prefab.transform.localScale;
        instance.transform.localScale = prefab.transform.localScale * selector.Scale;
        spawned.Add(instance);
        lastSpawned = instance;
        lastPrefabIndex = idx;
        SetEditing(instance, true);
    }

    void ApplyScaleToAll(float scale)
    {
        foreach (var pair in baseScales)
        {
            if (pair.Key != null) pair.Key.transform.localScale = pair.Value * scale;
        }
    }

    void ClearAll()
    {
        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }

        spawned.Clear();
        baseScales.Clear();
        lastSpawned = null;
        lastPrefabIndex = -1;
    }

    bool IsValidIndex(int idx)
    {
        return prefabs != null && idx >= 0 && idx < prefabs.Length && prefabs[idx] != null;
    }

    void SetEditing(GameObject obj, bool editing)
    {
        if (obj == null) return;

        Color color = editing ? editingColor : Color.white;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            renderer.SetPropertyBlock(block);
        }
    }

    void UpdateGhost()
    {
        if (!IsValidIndex(selector.SelectedIndex)) return;

        string expectedGhostName = prefabs[selector.SelectedIndex].name + "(Ghost)";
        if (ghost == null || ghost.name != expectedGhostName)
        {
            if (ghost) Destroy(ghost);
            ghost = Instantiate(prefabs[selector.SelectedIndex]);
            ghost.name = expectedGhostName;
            foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        }

        ghost.transform.localScale = prefabs[selector.SelectedIndex].transform.localScale * selector.Scale;
        ghost.SetActive(hasHit);
        if (hasHit) ghost.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
    }
}
