using System;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using GoogleMobileAds;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
#endif

public sealed class AdManager : MonoBehaviour
{
    public static AdManager Instance { get; private set; }

    [Header("Ad Unit Ids")]
#if UNITY_IOS
    [SerializeField] private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    [SerializeField] private string RewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    [SerializeField] private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField] private string RewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
#endif

    [Header("Rules")]
    [SerializeField] private int CooldownSeconds = 180;
    [SerializeField] private bool EnableAdsInEditor = false;
    [SerializeField] private bool SimulateRewardedInEditor = true;

    private const string LastInterstitialClosedUtcKey = "LastInterstitialClosedUtc";

#if UNITY_ANDROID || UNITY_IOS
    private InterstitialAd InterstitialAd;
    private RewardedAd RewardedAd;
#endif

    private bool IsInitialized;
    private bool IsLoading;
    private bool IsShowing;
    private bool IsRewardedLoading;

    private Action PendingOnComplete;
    private Action PendingOnRewarded;
    private Action<bool> PendingOnRewardedClosed;
    private bool RewardEarned;

    public bool InterstitialsDisabled { get; set; }

    private bool consentRequested;
    private bool consentReady;

    public event Action PrivacyChanged;

    public bool PrivacyOptionsRequired
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return consentReady && ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;
#else
            return false;
#endif
        }
    }

    public void ShowPrivacyOptions(Action<bool> onDone)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        ConsentForm.ShowPrivacyOptionsForm(error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (error != null) Debug.LogWarning($"[Ads] Privacy options failed: {error.Message}");
            PrivacyChanged?.Invoke();
            if (ConsentInformation.CanRequestAds()) InitializeIfNeeded();
            onDone?.Invoke(error == null);
        }));
#else
        onDone?.Invoke(false);
#endif
    }

    private void RequestConsent()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (consentRequested) return;
        consentRequested = true;

        ConsentInformation.Update(new ConsentRequestParameters(), updateError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {
            if (updateError != null) Debug.LogWarning($"[Ads] Consent info failed: {updateError.Message}");
            ConsentForm.LoadAndShowConsentFormIfRequired(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (formError != null) Debug.LogWarning($"[Ads] Consent form failed: {formError.Message}");
                consentReady = true;
                PrivacyChanged?.Invoke();
                if (ConsentInformation.CanRequestAds()) InitializeIfNeeded();
            }));
        }));

        if (ConsentInformation.CanRequestAds())
        {
            consentReady = true;
            InitializeIfNeeded();
        }
#else
        consentReady = true;
#endif
    }

    public bool IsRewardedReady
    {
        get
        {
            if (UseEditorSimulation) return true;
#if UNITY_ANDROID || UNITY_IOS
            return IsInitialized && RewardedAd != null && RewardedAd.CanShowAd();
#else
            return false;
#endif
        }
    }

    private bool UseEditorSimulation => Application.isEditor && !EnableAdsInEditor && SimulateRewardedInEditor;

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
        DestroyRewarded();
    }

    public void ShowInterstitialThen(Action OnComplete)
    {
        if (OnComplete == null) OnComplete = () => { };

        if (InterstitialsDisabled)
        {
            OnComplete.Invoke();
            return;
        }

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

    public void ShowRewarded(Action OnRewarded, Action<bool> OnClosed)
    {
        if (UseEditorSimulation)
        {
            Debug.Log("[Ads] Rewarded ad simulated in Editor.");
            OnRewarded?.Invoke();
            OnClosed?.Invoke(true);
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        if (IsShowing || RewardedAd == null || !RewardedAd.CanShowAd())
        {
            LoadRewardedIfNeeded();
            OnClosed?.Invoke(false);
            return;
        }

        IsShowing = true;
        RewardEarned = false;
        PendingOnRewarded = OnRewarded;
        PendingOnRewardedClosed = OnClosed;
        RewardedAd.Show(_ => MobileAdsEventExecutor.ExecuteInUpdate(() => RewardEarned = true));
#else
        OnClosed?.Invoke(false);
#endif
    }

    public bool CanShowInterstitialNow()
    {
        if (InterstitialsDisabled) return false;
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

    private bool initializing;

    private void InitializeIfNeeded()
    {
        if (IsInitialized || initializing) return;

        if (!consentReady)
        {
            RequestConsent();
            if (!consentReady) return;
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (!ConsentInformation.CanRequestAds()) return;
#endif

#if UNITY_ANDROID || UNITY_IOS
        initializing = true;
        MobileAds.Initialize(_ =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                initializing = false;
                IsInitialized = true;
                LoadInterstitialIfNeeded();
                LoadRewardedIfNeeded();
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
        if (InterstitialsDisabled) return;

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

    private void LoadRewardedIfNeeded()
    {
        if (!IsInitialized) return;
        if (IsRewardedLoading) return;

#if UNITY_ANDROID || UNITY_IOS
        if (RewardedAd != null && RewardedAd.CanShowAd()) return;

        IsRewardedLoading = true;
        DestroyRewarded();

        RewardedAd.Load(RewardedAdUnitId, new AdRequest(), (RewardedAd Ad, LoadAdError Error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsRewardedLoading = false;

                if (Error != null || Ad == null)
                    return;

                RewardedAd = Ad;
                HookRewardedEvents(RewardedAd);
            });
        });
#else
        IsRewardedLoading = false;
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

    private void HookRewardedEvents(RewardedAd Ad)
    {
        Ad.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsShowing = false;
                FinishRewarded();
                LoadRewardedIfNeeded();
            });
        };

        Ad.OnAdFullScreenContentFailed += (AdError _) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                IsShowing = false;
                RewardEarned = false;
                FinishRewarded();
                LoadRewardedIfNeeded();
            });
        };
    }
#endif

    private void FinishRewarded()
    {
        Action OnRewarded = PendingOnRewarded;
        Action<bool> OnClosed = PendingOnRewardedClosed;
        bool Earned = RewardEarned;

        PendingOnRewarded = null;
        PendingOnRewardedClosed = null;
        RewardEarned = false;

        if (Earned) OnRewarded?.Invoke();
        OnClosed?.Invoke(Earned);
    }

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

    private void DestroyRewarded()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (RewardedAd != null)
        {
            RewardedAd.Destroy();
            RewardedAd = null;
        }
#endif
    }
}
