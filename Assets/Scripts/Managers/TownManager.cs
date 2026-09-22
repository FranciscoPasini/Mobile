using UnityEngine;

public class TownManager : MonoBehaviour
{
    [SerializeField] private GameObject town;
    [SerializeField] private float townLife = 100;
    [SerializeField] private GameObject losePanel;

    public static TownManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        Lose();
    }

    public void TakeDamage(float damageAmount)
    {
        townLife -= damageAmount;
        Debug.Log("La base recibió daño. Vida actual: " + townLife);
    }

    private void Lose()
    {
        if (townLife <= 0)
        {
            OpenPanel(losePanel);
        }
    }

    private void OpenPanel(GameObject panel)
    {
        losePanel.SetActive(false);
        panel.SetActive(true);
    }
}