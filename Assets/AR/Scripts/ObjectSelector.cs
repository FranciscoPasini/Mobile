using UnityEngine;
using UnityEngine.UI;

public class ObjectSelector : MonoBehaviour
{
    [SerializeField] Button[] selectionButtons;
    [SerializeField] Button deleteAllButton;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = Color.green;

    [Header("Scale")]
    [Tooltip("Created under the Delete All button's panel if left empty.")]
    [SerializeField] Slider scaleSlider;
    [SerializeField] float minScale = 0.1f;
    [SerializeField] float maxScale = 2f;
    [SerializeField] float startScale = 1f;

    public int SelectedIndex { get; private set; } = -1;
    public float Scale { get; private set; } = 1f;
    public event System.Action<int> OnSelectionChanged;
    public event System.Action OnClearAll;
    public event System.Action<float> OnScaleChanged;

    void Start()
    {
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            int index = i;
            selectionButtons[i].onClick.AddListener(() => Select(index));
        }

        if (deleteAllButton != null)
        {
            deleteAllButton.onClick.AddListener(ClearAll);
        }

        SetupScaleSlider();
        Select(0);
    }

    public void Select(int index)
    {
        SelectedIndex = index;
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            var colors = selectionButtons[i].colors;
            colors.normalColor = (i == index) ? selectedColor : normalColor;
            selectionButtons[i].colors = colors;
        }
        OnSelectionChanged?.Invoke(index);
    }

    public void ClearAll()
    {
        OnClearAll?.Invoke();
    }

    public void SetScale(float scale)
    {
        Scale = scale;
        OnScaleChanged?.Invoke(scale);
    }

    void SetupScaleSlider()
    {
        if (scaleSlider == null && deleteAllButton != null)
        {
            scaleSlider = CreateSlider(deleteAllButton.transform.parent);
        }

        Scale = startScale;
        if (scaleSlider == null) return;

        scaleSlider.minValue = minScale;
        scaleSlider.maxValue = maxScale;
        scaleSlider.value = startScale;
        scaleSlider.onValueChanged.AddListener(SetScale);
    }

    static Slider CreateSlider(Transform parent)
    {
        GameObject sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = "ScaleSlider";
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.GetComponent<Slider>();
        SetImageColor(sliderObject.transform.Find("Background"), new Color(0.2f, 0.2f, 0.2f, 1f));
        SetImageColor(slider.fillRect, new Color(0.2f, 0.8f, 0.3f, 1f));
        SetImageColor(slider.handleRect, Color.white);
        if (slider.handleRect != null) slider.handleRect.sizeDelta = new Vector2(60f, 0f);
        return slider;
    }

    static void SetImageColor(Transform target, Color color)
    {
        if (target == null) return;
        Image image = target.GetComponent<Image>();
        if (image != null) image.color = color;
    }
}
