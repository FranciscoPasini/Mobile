using UnityEngine;
using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;

public class InterstitialAdManager : MonoBehaviour
{
    [Header("Configuración de Anuncio")]
    [SerializeField] private bool useTestAdUnit = true;

    private const string TEST_AD_UNIT_ID = "ca-app-pub-3940256099942544/1033173712";
    private const string REAL_AD_UNIT_ID = "ca-app-pub-9482470954655619/6420041799";

    private string AdUnitId => useTestAdUnit ? TEST_AD_UNIT_ID : REAL_AD_UNIT_ID;

    private InterstitialAd interstitialAd;
    private Action onAdClosedCallback;
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private static bool adsInitialized;

    private void Start()
    {
        if (adsInitialized)
        {
            LoadInterstitial();
            return;
        }

        MobileAds.Initialize(_ =>
        {
            adsInitialized = true;
            RunOnMainThread(LoadInterstitial);
        });
    }

    private void Update()
    {
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                mainThreadActions.Dequeue()?.Invoke();
            }
        }
    }

    private void OnDestroy()
    {
        DestroyAd();
    }

    private void LoadInterstitial()
    {
        if (this == null) return;

        DestroyAd();

        InterstitialAd.Load(AdUnitId, new AdRequest(), (InterstitialAd ad, LoadAdError error) =>
        {
            RunOnMainThread(() =>
            {
                if (this == null) return;

                if (error != null || ad == null)
                {
                    Debug.LogWarning("Interstitial failed to load: " + error);
                    return;
                }

                interstitialAd = ad;
                interstitialAd.OnAdFullScreenContentClosed += HandleOnAdClosed;
                interstitialAd.OnAdFullScreenContentFailed += HandleOnAdFailedToOpen;
            });
        });
    }

    public void ShowInterstitial(Action onClosed = null)
    {
        onAdClosedCallback = onClosed;

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
            return;
        }

        FinishAndContinue(reloadAd: false);
    }

    private void HandleOnAdClosed()
    {
        RunOnMainThread(() => FinishAndContinue(reloadAd: true));
    }

    private void HandleOnAdFailedToOpen(AdError error)
    {
        Debug.LogWarning("Interstitial failed to open: " + error);
        RunOnMainThread(() => FinishAndContinue(reloadAd: true));
    }

    private void FinishAndContinue(bool reloadAd)
    {
        Action callback = onAdClosedCallback;
        onAdClosedCallback = null;
        DestroyAd();
        callback?.Invoke();

        // LoadScene destroys this object. Don't touch ads after the scene is gone.
        if (reloadAd && this != null) LoadInterstitial();
    }

    private void DestroyAd()
    {
        if (interstitialAd == null) return;

        interstitialAd.OnAdFullScreenContentClosed -= HandleOnAdClosed;
        interstitialAd.OnAdFullScreenContentFailed -= HandleOnAdFailedToOpen;
        interstitialAd.Destroy();
        interstitialAd = null;
    }

    private void RunOnMainThread(Action action)
    {
        if (action == null) return;

        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }
}
