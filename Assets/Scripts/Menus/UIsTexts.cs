using UnityEngine;

public class UIsTexts : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI waveText;
    [SerializeField] private TMPro.TextMeshProUGUI coinsText;
    [SerializeField] private TMPro.TextMeshProUGUI ammoText;

    [Header("Feedback (optional)")]
    [SerializeField] private UIPunch coinsPunch;
    [SerializeField] private UIPunch wavePunch;
    [SerializeField] private Color coinsGainedColor = new Color(0.45f, 1f, 0.45f);
    [SerializeField] private Color coinsSpentColor = new Color(1f, 0.4f, 0.4f);

    [SerializeField] private TownManager townManager;
    [SerializeField] private PlayerWeaponSystem playerWeaponSystem;

    private int lastShownAmmo = int.MinValue;
    private bool lastShownInfinite;
    private int lastCoins;

    void OnEnable()
    {
        if (townManager == null) townManager = FindFirstObjectByType<TownManager>();

        TownManager.OnCoinsChanged += OnCoinsChanged;
        townManager.OnWaveChanged += OnWaveChanged;

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

        if (playerWeaponSystem != null)
        {
            playerWeaponSystem.OnActiveWeaponChanged -= RefreshAmmo;
            playerWeaponSystem.OnWeaponStatsChanged -= RefreshAmmo;
        }
    }

    void Start()
    {
        lastCoins = TownManager.Coins;
        coinsText.text = lastCoins.ToString();
        waveText.text = townManager.CurrentWave.ToString();
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

        if (coinsPunch != null && coins != lastCoins)
        {
            coinsPunch.Play(coins > lastCoins ? coinsGainedColor : coinsSpentColor);
        }
        lastCoins = coins;
    }

    private void OnWaveChanged()
    {
        waveText.text = townManager.CurrentWave.ToString();
        if (wavePunch != null) wavePunch.Play();
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
        ammoText.text = infinite ? "INF" : ammo.ToString();
    }
}
