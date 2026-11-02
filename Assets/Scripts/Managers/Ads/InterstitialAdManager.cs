using UnityEngine;
using GoogleMobileAds.Api;
using System;


public class InterstitialAdManager : MonoBehaviour
{
    [Header("Configuración de Anuncio")]
    [SerializeField] private bool useTestAdUnit = true;

    // ID de prueba oficial de Google para Intersticiales en Android
    private const string TEST_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";
    // Tu ID REAL de la unidad de anuncio intersticial
    private const string REAL_AD_UNIT_ID = "ca-app-pub-9482470954655619/6420041799";

    private string AdUnitId => useTestAdUnit ? TEST_AD_UNIT_ID : REAL_AD_UNIT_ID;

    private InterstitialAd interstitialAd;
    private Action onAdClosedCallback;

    private void Start()
    {
        MobileAds.Initialize(initStatus => { });
        LoadInterstitial();
    }

    private void LoadInterstitial()
    {
        // Destruir el anuncio anterior si existe
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        var adRequest = new AdRequest();

        // Usar el método estático Load (v8.x+)
        InterstitialAd.Load(AdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogError("Error al cargar anuncio intersticial: " + error);
                return;
            }

            interstitialAd = ad;
            Debug.Log("Anuncio intersticial cargado correctamente.");

            // Registrar eventos
            RegisterEventHandlers();
        });
    }

    private void RegisterEventHandlers()
    {
        interstitialAd.OnAdFullScreenContentOpened += HandleOnAdOpened;
        interstitialAd.OnAdFullScreenContentClosed += HandleOnAdClosed;
        interstitialAd.OnAdFullScreenContentFailed += HandleOnAdFailedToOpen;
    }

    public void ShowInterstitial(Action onClosed = null)
    {
        onAdClosedCallback = onClosed;

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Mostrando anuncio intersticial...");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("Anuncio intersticial no disponible.");
            onAdClosedCallback?.Invoke();
        }
    }

    private void HandleOnAdOpened()
    {
        Debug.Log("Anuncio intersticial abierto.");
    }

    private void HandleOnAdClosed()
    {
        Debug.Log("Anuncio intersticial cerrado.");
        onAdClosedCallback?.Invoke();
        LoadInterstitial();
    }

    private void HandleOnAdFailedToOpen(AdError error)
    {
        Debug.LogError("Error al mostrar anuncio intersticial: " + error);
        onAdClosedCallback?.Invoke();
    }
}