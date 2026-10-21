using UnityEngine;
using UnityEngine.UI;

public class UIsTexts : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI waveText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;
    [SerializeField] private Image townHealthFillImage;
    [SerializeField] private TMPro.TextMeshProUGUI ammoText;

    [SerializeField] private TownManager townManager;
    [SerializeField] private PlayerWeaponSystem playerWeaponSystem;

    private int lastShownAmmo = int.MinValue;
    private bool lastShownInfinite;

    void OnEnable()
    {
        TownManager.OnCoinsChanged += OnCoinsChanged;
        townManager.OnWaveChanged += OnWaveChanged;
        townManager.OnTownHealthChanged += RefreshTownHealth;

        if (playerWeaponSystem == null)
        {
            playerWeaponSystem = FindFirstObjectByType<PlayerWeaponSystem>();
        }

        if (playerWeaponSystem != null)
        {
            playerWeaponSystem.OnActiveWeaponChanged += RefreshAmmo;
            playerWeaponSystem.OnWeaponStatsChanged += RefreshAmmo;
        }
    }

    void OnDisable()
    {
        TownManager.OnCoinsChanged -= OnCoinsChanged;
        townManager.OnWaveChanged -= OnWaveChanged;
        townManager.OnTownHealthChanged -= RefreshTownHealth;

        if (playerWeaponSystem != null)
        {
            playerWeaponSystem.OnActiveWeaponChanged -= RefreshAmmo;
            playerWeaponSystem.OnWeaponStatsChanged -= RefreshAmmo;
        }
    }

    void Start()
    {
        coinsText.text = TownManager.Coins.ToString();
        waveText.text = townManager.CurrentWave.ToString();
        RefreshTownHealth();
        RefreshAmmo();
    }

    void Update()
    {
        if (playerWeaponSystem != null && playerWeaponSystem.HasPickedUpWeapon)
        {
            RefreshAmmo();
        }
    }

    private void OnCoinsChanged(int coins)
    {
        coinsText.text = coins.ToString();
    }

    private void OnWaveChanged()
    {
        waveText.text = townManager.CurrentWave.ToString();
    }

    private void RefreshTownHealth()
    {
        if (townHealthFillImage == null) return;

        float currentHealth = townManager.GetTownHealth();
        float maxHealth = townManager.GetTownMaxHealth();

        if (maxHealth > 0)
        {
            townHealthFillImage.fillAmount = currentHealth / maxHealth;
        }
        else
        {
            townHealthFillImage.fillAmount = 0f;
        }
    }

    private void RefreshAmmo()
    {
        if (ammoText == null) return;

        bool infinite = playerWeaponSystem == null || !playerWeaponSystem.HasPickedUpWeapon;
        int ammo = 0;
        if (!infinite)
        {
            Base_Weapon weapon = playerWeaponSystem.GetActiveWeapon();
            ammo = weapon != null ? weapon.GetCurrentAmmo() : 0;
        }

        if (infinite == lastShownInfinite && ammo == lastShownAmmo) return;

        lastShownInfinite = infinite;
        lastShownAmmo = ammo;
        ammoText.text = infinite ? "Infinite" : ammo.ToString();
    }
}