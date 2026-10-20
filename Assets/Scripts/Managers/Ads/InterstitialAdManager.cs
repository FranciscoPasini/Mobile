using UnityEngine;
//using GoogleMobileAds.Api;

public class InterstitialAdManager : MonoBehaviour
{
    [Header("Configuración de Anuncio")]
    [Tooltip("Si está tildado, usa el ID de prueba de Google. Destildalo SOLO al subir el juego a la tienda.")]
    [SerializeField] private bool useTestAdUnit = true;

    // ID de prueba oficial de Google para Intersticiales en Android
    private const string TEST_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";

    // Tu ID REAL de la unidad de anuncio intersticial (el que me pasaste)
    private const string REAL_AD_UNIT_ID = "ca-app-pub-9482470954655619/6420041799";

  //  private InterstitialAd _interstitialAd;

    // Esto decide automáticamente qué ID usar según el tilde del Inspector
    private string AdUnitId => useTestAdUnit ? TEST_AD_UNIT_ID : REAL_AD_UNIT_ID;

    private void Start()
    {
        //MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[AdMob] SDK Inicializado.");
            //LoadInterstitialAd();
        }
    }

    /*
    public void LoadInterstitialAd()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("[AdMob] Cargando anuncio intersticial con ID: " + AdUnitId);

        var adRequest = new AdRequest();

        InterstitialAd.Load(AdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("[AdMob] Error al cargar el anuncio intersticial: " + error?.GetMessage());
                return;
            }

            Debug.Log("[AdMob] Anuncio intersticial cargado con éxito.");
            _interstitialAd = ad;

            RegisterEventHandlers(_interstitialAd);
        });
    }

    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("[AdMob] Mostrando anuncio intersticial.");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMob] El anuncio intersticial aún no está listo.");
            LoadInterstitialAd();
        }
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        // Evento cuando el usuario cierra el anuncio
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdMob] Anuncio cerrado. Cargando el siguiente...");
            LoadInterstitialAd();
        };

        // Evento si falla al mostrarse en pantalla completa
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("[AdMob] Error al mostrar el anuncio en pantalla: " + error.GetMessage());
            LoadInterstitialAd();
        };
    }

    private void OnDestroy()
    {
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
        }
    }

    */
}