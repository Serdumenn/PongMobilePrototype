using System;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

public sealed class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Ad Unit Ids (Use Test Ids In Development)")]
#if UNITY_ANDROID
    [SerializeField] private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#elif UNITY_IOS
    [SerializeField] private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    [SerializeField] private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
#endif

    [Header("Rules")]
    [SerializeField] private int CooldownSeconds = 180; // 3 minutes
    [SerializeField] private bool EnableAdsInEditor = false;

    private const string LastInterstitialClosedUtcKey = "LastInterstitialClosedUtc";

#if UNITY_ANDROID || UNITY_IOS
    private InterstitialAd InterstitialAd;
#endif

    private bool IsInitialized;
    private bool IsLoading;
    private bool IsShowing;

    private Action PendingOnComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        DestroyAd();
    }

    public void ShowInterstitialThen(Action OnComplete)
    {
        if (OnComplete == null) OnComplete = () => { };

        if (!CanShowInterstitialNow())
        {
            OnComplete.Invoke();
            InitializeIfNeeded();
            LoadInterstitialIfNeeded();
            return;
        }

        if (IsShowing)
        {
            OnComplete.Invoke();
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        if (InterstitialAd == null || !InterstitialAd.CanShowAd())
        {
            OnComplete.Invoke();
            LoadInterstitialIfNeeded();
            return;
        }

        IsShowing = true;
        PendingOnComplete = OnComplete;
        InterstitialAd.Show();
#else
        OnComplete.Invoke();
#endif
    }

    public bool CanShowInterstitialNow()
    {
        if (Application.isEditor && !EnableAdsInEditor) return false;
        if (!IsInitialized) return false;
        if (IsCooldownActive()) return false;
        if (IsShowing) return false;

#if UNITY_ANDROID || UNITY_IOS
        return InterstitialAd != null && InterstitialAd.CanShowAd();
#else
        return false;
#endif
    }

    private void InitializeIfNeeded()
    {
        if (IsInitialized) return;

#if UNITY_ANDROID || UNITY_IOS
        MobileAds.Initialize(_ =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsInitialized = true;
                LoadInterstitialIfNeeded();
            });
        });
#else
        IsInitialized = true;
#endif
    }

    private void LoadInterstitialIfNeeded()
    {
        if (!IsInitialized) return;
        if (IsLoading) return;
        if (IsShowing) return;

#if UNITY_ANDROID || UNITY_IOS
        IsLoading = true;

        DestroyAd();

        AdRequest Request = new AdRequest();
        InterstitialAd.Load(InterstitialAdUnitId, Request, (InterstitialAd Ad, LoadAdError Error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsLoading = false;

                if (Error != null || Ad == null)
                    return;

                InterstitialAd = Ad;
                HookEvents(InterstitialAd);
            });
        });
#else
        IsLoading = false;
#endif
    }

#if UNITY_ANDROID || UNITY_IOS
    private void HookEvents(InterstitialAd Ad)
    {
        Ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsShowing = false;

                WriteLastInterstitialClosedUtcNow();
                InvokeAndClearPending();

                LoadInterstitialIfNeeded();
            });
        };

        Ad.OnAdFullScreenContentFailed += (AdError _) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsShowing = false;

                InvokeAndClearPending();
                LoadInterstitialIfNeeded();
            });
        };
    }
#endif

    private void InvokeAndClearPending()
    {
        Action Callback = PendingOnComplete;
        PendingOnComplete = null;
        Callback?.Invoke();
    }

    private bool IsCooldownActive()
    {
        long Now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long Last = ReadLastInterstitialClosedUtc();
        if (Last <= 0) return false;

        long Elapsed = Now - Last;
        return Elapsed < CooldownSeconds;
    }

    private long ReadLastInterstitialClosedUtc()
    {
        string Raw = PlayerPrefs.GetString(LastInterstitialClosedUtcKey, "0");
        return long.TryParse(Raw, out long Value) ? Value : 0;
    }

    private void WriteLastInterstitialClosedUtcNow()
    {
        long Now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        PlayerPrefs.SetString(LastInterstitialClosedUtcKey, Now.ToString());
        PlayerPrefs.Save();
    }

    private void DestroyAd()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (InterstitialAd != null)
        {
            InterstitialAd.Destroy();
            InterstitialAd = null;
        }
#endif
    }
}