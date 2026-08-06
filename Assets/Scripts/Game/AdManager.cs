using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    // Official Google Test Ad Unit IDs. 
    // Replace with your actual Ad Unit IDs from the AdMob dashboard ONLY before publishing.
#if UNITY_ANDROID
    private string _interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712"; 
#elif UNITY_IPHONE
    private string _interstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    private string _interstitialAdUnitId = "unused";
#endif

    private InterstitialAd _interstitialAd;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus => {
            Debug.Log("AdMob Initialized.");
            LoadInterstitialAd();
        });
    }

    // Logic to suppress ads if the user bought the Remove Ads IAP
    public bool HasRemovedAds()
    {
        // Replace with your actual logic to check if "Remove Ads" was purchased.
        return PlayerPrefs.GetInt("AdsRemoved", 0) == 1; 
    }

    public void LoadInterstitialAd()
    {
        if (HasRemovedAds()) return;

        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading AdMob interstitial ad...");
        var adRequest = new AdRequest();

        InterstitialAd.Load(_interstitialAdUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                    return;
                }

                Debug.Log("Interstitial ad loaded successfully.");
                _interstitialAd = ad;
                RegisterEventHandlers(_interstitialAd);
            });
    }

    public void ShowInterstitialAd()
    {
        if (HasRemovedAds())
        {
            Debug.Log("Ads removed by user. Skipping interstitial.");
            return;
        }

        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");
        }
    }

    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is closed.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial Ad closed.");
            // Reload the ad so that we can show another as soon as possible.
            LoadInterstitialAd();
        };
        
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open with error : " + error);
            LoadInterstitialAd();
        };
    }
}
