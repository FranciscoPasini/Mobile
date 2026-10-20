using UnityEngine;
using GoogleMobileAds.Api;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Inicializar el SDK de Google Mobile Ads
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("AdMob Inicializado correctamente.");
        });
    }
}