using UnityEngine;

public class UIsTexts : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI waveText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;
    [SerializeField] private TMPro.TextMeshProUGUI townHealthText;
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
        coinsText.text = "Coins: " + TownManager.Coins.ToString();
        waveText.text = "Wave: " + townManager.CurrentWave.ToString();
        RefreshTownHealth();
        RefreshAmmo();
    }

    void Update()
    {
        // Pickup ammo drops every shot; those shots do not fire a weapon-changed event.
        if (playerWeaponSystem != null && playerWeaponSystem.HasPickedUpWeapon)
        {
            RefreshAmmo();
        }
    }

    private void OnCoinsChanged(int coins)
    {
        coinsText.text = "Coins: " + coins.ToString();
    }

    private void OnWaveChanged()
    {
        waveText.text = "Wave: " + townManager.CurrentWave.ToString();
    }

    private void RefreshTownHealth()
    {
        townHealthText.text = "Town Health: " + townManager.GetTownHealth().ToString() + " / " + townManager.GetTownMaxHealth().ToString();
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
        ammoText.text = infinite ? "Ammo: Infinite" : "Ammo: " + ammo.ToString();
    }
}
