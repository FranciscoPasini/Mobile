using UnityEngine;
using UnityEngine.UI;

public class ObjectSelector : MonoBehaviour
{
    [SerializeField] Button[] selectionButtons;
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color selectedColor = Color.green;

    public int SelectedIndex { get; private set; } = -1;
    public event System.Action<int> OnSelectionChanged;

    void Start()
    {
        for (int i = 0; i < selectionButtons.Length; i++)
        {
            int index = i; // copia local para el listener
            selectionButtons[i].onClick.AddListener(() => Select(index));
        }
        Select(0); // selección inicial
    }

    // Mantiene SIEMPRE un único objeto seleccionado
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
}
