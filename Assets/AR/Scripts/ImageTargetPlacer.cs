using UnityEngine;
using Vuforia;

public class ImageTargetPlacer : MonoBehaviour
{
    [SerializeField] ObjectSelector selector;
    [SerializeField] GameObject[] prefabs;        // mismo orden que los botones
    [SerializeField] ObserverBehaviour imageTarget;

    GameObject current;   // solo un objeto colocado a la vez
    bool tracked;

    void OnEnable()
    {
        imageTarget.OnTargetStatusChanged += OnStatusChanged;
    }
    void OnDisable()
    {
        imageTarget.OnTargetStatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged(ObserverBehaviour obs, TargetStatus status)
    {
        tracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }

    void Update()
    {
        // Al pulsar en pantalla, se coloca el objeto seleccionado sobre el target
        if (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (!tracked) return;
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            SpawnSelected();
        }
    }

    void SpawnSelected()
    {
        int idx = selector.SelectedIndex;
        if (idx < 0 || idx >= prefabs.Length) return;

        if (current) Destroy(current);  // un solo objeto a la vez
        current = Instantiate(prefabs[idx], imageTarget.transform);
        current.transform.localPosition = new Vector3(0, 0.05f, 0);
        current.transform.localRotation = Quaternion.identity;
    }
}
