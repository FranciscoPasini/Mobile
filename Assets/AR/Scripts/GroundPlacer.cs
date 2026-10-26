using UnityEngine;
using UnityEngine.EventSystems;
using Vuforia;

public class GroundPlacer : MonoBehaviour
{
    [SerializeField] ObjectSelector selector;
    [SerializeField] GameObject[] prefabs;
    [SerializeField] PlaneFinderBehaviour groundPlaneFinder;
    [SerializeField] GameObject previewGhost;

    bool hasHit;
    Pose hitPose;
    GameObject ghost;

    void OnEnable()
    {
        if (groundPlaneFinder != null)
        {
            groundPlaneFinder.OnInteractiveHitTest.AddListener(OnHitTest);
        }
    }

    void OnDisable()
    {
        if (groundPlaneFinder != null)
        {
            groundPlaneFinder.OnInteractiveHitTest.RemoveListener(OnHitTest);
        }
    }

    // Vuforia nos informa del punto del plano real que apunta el centro de la pantalla
    void OnHitTest(HitTestResult result)
    {
        hasHit = result != null;
        if (!hasHit) return;
        hitPose = new Pose(result.Position, result.Rotation);
    }

    void Update()
    {
        if (selector == null) return;

        UpdateGhost();

        // Detección de toque (móvil) o clic izquierdo (Editor de Unity)
        bool isClickedOrTouched = false;

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
            {
                isClickedOrTouched = true;
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && !EventSystem.current.IsPointerOverGameObject())
            {
                isClickedOrTouched = true;
            }
        }

        if (isClickedOrTouched && hasHit)
        {
            PlaceSelected(hitPose);
        }
    }

    void PlaceSelected(Pose pose)
    {
        if (selector == null || prefabs == null) return;
        int idx = selector.SelectedIndex;
        if (idx < 0 || idx >= prefabs.Length || prefabs[idx] == null) return;
        Instantiate(prefabs[idx], pose.position, pose.rotation);
    }

    void UpdateGhost()
    {
        if (selector == null || prefabs == null) return;
        int idx = selector.SelectedIndex;
        if (idx < 0 || idx >= prefabs.Length || prefabs[idx] == null) return;

        string expectedGhostName = prefabs[idx].name + "(Ghost)";
        if (ghost == null || ghost.name != expectedGhostName)
        {
            if (ghost) Destroy(ghost);
            ghost = Instantiate(prefabs[idx]);
            ghost.name = expectedGhostName;
            foreach (var c in ghost.GetComponentsInChildren<Collider>()) c.enabled = false;
        }
        ghost.SetActive(hasHit);
        if (hasHit) ghost.transform.SetPositionAndRotation(hitPose.position, hitPose.rotation);
    }
}