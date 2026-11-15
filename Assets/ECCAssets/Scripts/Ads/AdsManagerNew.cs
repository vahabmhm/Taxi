using System;
using System.Collections.Generic;
using AdiveryUnity;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Central Adivery + Tapsell Mediation manager ( Task 5 ). Adapted from the developer's Crowd Rush AdsManagerNew
    /// ( itself from ConnectBalls ): same SDK calls, same Adivery-first rotation and same saved rotation key.
    ///
    /// It plugs into ECCAds as its provider, so every rewarded / interstitial request of the game ( coin gift, daily
    /// challenge +30 seconds and 2X, power-ups, revive, level 2X and skip, car trial ) still goes through ECCAds:
    /// ECCAds shows the loading popup ( at least 2 seconds ), allows one flow at a time and shows NoAds;
    /// AdMediator picks the network, falls back to the other one before display and ignores late / duplicate callbacks.
    /// Rewards are paid by the callers, only after a verified rewarded completion, never here.
    ///
    /// Created automatically at startup. The SDKs only run on an Android device; in the Editor no network is created
    /// and every request ends in the NoAds panel.
    /// </summary>
    public class AdsManagerNew : MonoBehaviour, IECCAdProvider, IECCInterstitialProvider
    {
        public static AdsManagerNew Instance { get; private set; }

        //====================================================
        // Endless Car Chase ad IDs ( package com.car.chase.vp )
        //====================================================
        // Fill these with THIS game's IDs from the Adivery and Tapsell panels. An empty ID turns that network / format off.
        // The Tapsell Mediation app key is set in Project Settings > Tapsell Mediation ( ProjectSettings/TapsellMediationSettings.json ).

        public const string AdiveryAppId = "8dbf9682-3798-4896-a73d-3ec73110a79b";
        public const string AdiveryRewardedId = "b7519ad2-294e-4ba4-bf97-345ae26aea6b";
        public const string AdiveryInterstitialId = "70c4c90e-9be8-4c8c-b394-8cf9dd2421be";

        public const string TapsellRewardedZoneId = "6aea00e6babd4f496a771cab";
        public const string TapsellInterstitialZoneId = "6aea013176df8661d1cadf04";

        // Network that last displayed an ad ( same key as ConnectBalls / Crowd Rush )
        const string LastShownNetworkKey = "Ads_LastShownNetwork";

        // A load request without any answer after this long counts as failed
        const float LoadAnswerSeconds = 30;

        // How often missing ads are requested again in the background
        const float ReloadInterval = 30;

        // Minimum real-time seconds between two interstitials ( and after app start before the first one )
        public const float InterstitialMinInterval = 90;

        AdMediator mediator;
        float lastInterstitialAt;

        // SDK callbacks may come from Java threads; they are run in Update
        readonly List<Action> pending = new List<Action>();
        readonly List<Action> running = new List<Action>();
        volatile bool hasPending;

        public bool AdsEnabled
        {
            get { return mediator != null && mediator.HasNetworks; }
        }

        /// <summary>
        /// The network that will be tried first for the next ad ( Adivery at first, then alternating after each display )
        /// </summary>
        public AdNetworkId Preferred
        {
            get { return mediator != null ? mediator.Preferred : AdNetworkId.Adivery; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance == null) new GameObject("AdsManagerNew").AddComponent<AdsManagerNew>();
        }

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
            lastInterstitialAt = Time.realtimeSinceStartup;

            if (Application.platform != RuntimePlatform.Android)
            {
                Debug.Log("[Ads] Adivery and Tapsell run only on Android devices; ad requests show NoAds here.");
                return;
            }

            UseNetworks(new PrefsRotationStore(), AdiveryNetwork.TryCreate(this), TapsellNetwork.TryCreate(this));
        }

        /// <summary>
        /// Starts the mediation with the given networks ( also usable by tests with stand-in networks )
        /// </summary>
        public void UseNetworks(IAdRotationStore store, params IAdNetwork[] networks)
        {
            CancelInvoke("ReloadMissing");

            mediator = new AdMediator(store, networks);

            if (mediator.HasNetworks == false)
            {
                Debug.LogWarning("[Ads] No ad network is configured ( IDs in AdsManagerNew.cs ); every rewarded request shows NoAds.");
                return;
            }

            mediator.LoadAll(AdFormat.Rewarded);
            mediator.LoadAll(AdFormat.Interstitial);

            ECCAds.RegisterProvider(this);

            InvokeRepeating("ReloadMissing", ReloadInterval, ReloadInterval);
        }

        void ReloadMissing()
        {
            if (mediator == null || mediator.Busy) return;

            if (mediator.IsReady(AdFormat.Rewarded) == false) mediator.LoadAll(AdFormat.Rewarded);
            if (mediator.IsReady(AdFormat.Interstitial) == false) mediator.LoadAll(AdFormat.Interstitial);
        }

        void Update()
        {
            if (hasPending)
            {
                lock (pending)
                {
                    running.AddRange(pending);
                    pending.Clear();
                    hasPending = false;
                }

                foreach (Action action in running)
                {
                    try
                    {
                        if (action != null) action();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }

                running.Clear();
            }

            // Reward grace time, lost callbacks and timeouts
            if (mediator != null && mediator.Busy) mediator.Tick(Time.realtimeSinceStartup);
        }

        void OnApplicationPause(bool paused)
        {
            // The ad activity covering the game counts as "displayed", so a late error can't start a second ad for the same request
            if (paused && mediator != null) mediator.NoteAppPaused();
        }

        /// <summary>
        /// Runs an SDK callback on the main thread
        /// </summary>
        void Post(Action action)
        {
            if (action == null) return;

            lock (pending)
            {
                pending.Add(action);
                hasPending = true;
            }
        }

        //====================================================
        // Rewarded ( IECCAdProvider, called only by ECCAds )
        //====================================================

        public string Name
        {
            get { return "AdsManagerNew"; }
        }

        public bool IsRewardedReady
        {
            get { return mediator != null && mediator.Busy == false && mediator.IsReady(AdFormat.Rewarded); }
        }

        public bool HasRewardedFailed
        {
            get { return mediator == null || (mediator.IsReady(AdFormat.Rewarded) == false && mediator.IsLoading(AdFormat.Rewarded) == false); }
        }

        public void RequestRewarded()
        {
            if (mediator != null && mediator.Busy == false && mediator.IsReady(AdFormat.Rewarded) == false) mediator.LoadAll(AdFormat.Rewarded);
        }

        public void ShowRewarded(Action<ECCAdResult> done)
        {
            if (mediator == null || mediator.HasNetworks == false)
            {
                done(ECCAdResult.Unavailable);
                return;
            }

            Debug.Log("[Ads] Rewarded request, preferred " + mediator.Preferred);

            bool started = mediator.Request(AdFormat.Rewarded, Time.realtimeSinceStartup, delegate (AdOutcome outcome)
            {
                Debug.Log("[Ads] Rewarded -> " + outcome);

                done(ToResult(outcome));
            });

            // An interstitial is still on screen: never stack a second ad
            if (started == false) done(ECCAdResult.Unavailable);
        }

        static ECCAdResult ToResult(AdOutcome outcome)
        {
            switch (outcome)
            {
                case AdOutcome.Rewarded: return ECCAdResult.Rewarded;
                case AdOutcome.ClosedNoReward: return ECCAdResult.Skipped;
                case AdOutcome.FailedAfterDisplay: return ECCAdResult.Failed;
                default: return ECCAdResult.Unavailable;
            }
        }

        //====================================================
        // Interstitial ( IECCInterstitialProvider )
        //====================================================

        public bool IsInterstitialReady
        {
            get
            {
                return mediator != null && mediator.Busy == false && Time.realtimeSinceStartup - lastInterstitialAt >= InterstitialMinInterval
                    && mediator.IsReady(AdFormat.Interstitial);
            }
        }

        public void ShowInterstitial(Action<bool> done)
        {
            bool started = mediator != null && mediator.Request(AdFormat.Interstitial, Time.realtimeSinceStartup, delegate (AdOutcome outcome)
            {
                if (outcome != AdOutcome.Unavailable) lastInterstitialAt = Time.realtimeSinceStartup;

                Debug.Log("[Ads] Interstitial -> " + outcome);

                done(outcome != AdOutcome.Unavailable);
            });

            if (started == false) done(false);
        }

        //====================================================
        // Rotation store
        //====================================================

        sealed class PrefsRotationStore : IAdRotationStore
        {
            public int LastShown
            {
                get { return PlayerPrefs.GetInt(LastShownNetworkKey, -1); }
                set
                {
                    PlayerPrefs.SetInt(LastShownNetworkKey, value);
                    PlayerPrefs.Save();
                }
            }
        }

        //====================================================
        // Adivery
        //====================================================

        sealed class AdiveryNetwork : IAdNetwork
        {
            readonly AdsManagerNew owner;
            AdiveryListener listener;

            // If the Adivery Android library is missing from the build every wrapper call throws;
            // Adivery is then switched off and Tapsell keeps working
            bool available = true;

            AdShowCallbacks showing;
            string showingPlacement;

            // Placement -> time its Prepare call was sent; cleared by the loaded / error answer
            readonly Dictionary<string, float> loading = new Dictionary<string, float>();

            AdiveryNetwork(AdsManagerNew owner)
            {
                this.owner = owner;
            }

            public AdNetworkId Id
            {
                get { return AdNetworkId.Adivery; }
            }

            public static IAdNetwork TryCreate(AdsManagerNew owner)
            {
                if (string.IsNullOrEmpty(AdiveryAppId))
                {
                    Debug.LogWarning("[Ads] Adivery App ID is empty in AdsManagerNew; Adivery is off.");
                    return null;
                }

                if (string.IsNullOrEmpty(AdiveryRewardedId) && string.IsNullOrEmpty(AdiveryInterstitialId))
                {
                    Debug.LogWarning("[Ads] No Adivery placement ID in AdsManagerNew; Adivery is off.");
                    return null;
                }

                AdiveryNetwork network = new AdiveryNetwork(owner);

                try
                {
                    network.Configure();
                }
                catch (Exception exception)
                {
                    Debug.LogError("[Ads] Adivery configure failed ( is com.adivery:sdk in the Gradle build? ): " + exception);
                    return null;
                }

                return network;
            }

            void Configure()
            {
                Adivery.Configure(AdiveryAppId);
                Adivery.SetLoggingEnabled(Debug.isDebugBuild);

                listener = new AdiveryListener();
                listener.OnError += OnError;
                listener.OnRewardedAdLoaded += OnLoaded;
                listener.OnInterstitialAdLoaded += OnLoaded;
                listener.OnRewardedAdShown += OnShown;
                listener.OnInterstitialAdShown += OnShown;
                listener.OnRewardedAdClosed += OnRewardedClosed;
                listener.OnInterstitialAdClosed += OnInterstitialClosed;
                Adivery.AddListener(listener);
            }

            string Placement(AdFormat format)
            {
                return format == AdFormat.Rewarded ? AdiveryRewardedId : AdiveryInterstitialId;
            }

            public bool IsReady(AdFormat format)
            {
                string placement = Placement(format);

                if (available == false || string.IsNullOrEmpty(placement)) return false;

                try
                {
                    return Adivery.IsLoaded(placement);
                }
                catch (Exception exception)
                {
                    Disable("IsLoaded", exception);
                    return false;
                }
            }

            public bool IsLoading(AdFormat format)
            {
                string placement = Placement(format);
                float at;

                return available && string.IsNullOrEmpty(placement) == false && loading.TryGetValue(placement, out at)
                    && Time.realtimeSinceStartup - at < LoadAnswerSeconds;
            }

            public void Load(AdFormat format)
            {
                string placement = Placement(format);

                if (available == false || string.IsNullOrEmpty(placement) || IsReady(format)) return;

                loading[placement] = Time.realtimeSinceStartup;

                try
                {
                    if (format == AdFormat.Rewarded) Adivery.PrepareRewardedAd(placement);
                    else Adivery.PrepareInterstitialAd(placement);
                }
                catch (Exception exception)
                {
                    Disable("Prepare", exception);
                }
            }

            public void Show(AdFormat format, AdShowCallbacks callbacks)
            {
                showing = callbacks;
                showingPlacement = Placement(format);

                try
                {
                    Adivery.Show(showingPlacement);
                }
                catch (Exception exception)
                {
                    showing = null;
                    Disable("Show", exception);

                    // Nothing was presented: the mediator tries Tapsell
                    throw;
                }
            }

            void Disable(string operation, Exception exception)
            {
                available = false;
                loading.Clear();

                Debug.LogError("[Ads] Adivery disabled (" + operation + "): the Adivery Android library is probably missing from the build. " + exception);
            }

            AdShowCallbacks For(string placement)
            {
                return showing != null && placement == showingPlacement ? showing : null;
            }

            void OnLoaded(object caller, string placement)
            {
                owner.Post(() => loading.Remove(placement));
            }

            void OnError(object caller, AdiveryError error)
            {
                Debug.LogWarning("[Ads] Adivery error " + error.PlacementId + ": " + error.Reason);

                string placement = error.PlacementId;
                owner.Post(() => { if (placement != null) loading.Remove(placement); });

                AdShowCallbacks callbacks = For(error.PlacementId);

                if (callbacks == null) return;

                showing = null;
                owner.Post(callbacks.Failed);
            }

            void OnShown(object caller, string placement)
            {
                AdShowCallbacks callbacks = For(placement);

                if (callbacks != null) owner.Post(callbacks.Displayed);
            }

            void OnRewardedClosed(object caller, AdiveryReward reward)
            {
                AdShowCallbacks callbacks = reward != null ? For(reward.PlacementId) : showing;

                if (callbacks == null) return;

                showing = null;

                if (reward != null && reward.IsRewarded) owner.Post(callbacks.Rewarded);

                owner.Post(callbacks.Closed);
            }

            void OnInterstitialClosed(object caller, string placement)
            {
                AdShowCallbacks callbacks = For(placement);

                if (callbacks == null) return;

                showing = null;
                owner.Post(callbacks.Closed);
            }
        }

        //====================================================
        // Tapsell Mediation
        //====================================================

        sealed class TapsellNetwork : IAdNetwork
        {
            readonly AdsManagerNew owner;

            string rewardedAdId = "";
            string interstitialAdId = "";
            bool rewardedLoading;
            bool interstitialLoading;
            float rewardedLoadAt;
            float interstitialLoadAt;

            // Without the Tapsell Android libraries ( Gradle templates / Android Resolver ) every call throws
            bool available = true;

            TapsellNetwork(AdsManagerNew owner)
            {
                this.owner = owner;
            }

            public AdNetworkId Id
            {
                get { return AdNetworkId.Tapsell; }
            }

            public static IAdNetwork TryCreate(AdsManagerNew owner)
            {
                if (string.IsNullOrEmpty(TapsellRewardedZoneId) && string.IsNullOrEmpty(TapsellInterstitialZoneId))
                {
                    Debug.LogWarning("[Ads] No Tapsell zone ID in AdsManagerNew; Tapsell is off.");
                    return null;
                }

                return new TapsellNetwork(owner);
            }

            public bool IsReady(AdFormat format)
            {
                return available && string.IsNullOrEmpty(format == AdFormat.Rewarded ? rewardedAdId : interstitialAdId) == false;
            }

            void Disable(string operation, Exception exception)
            {
                available = false;
                rewardedLoading = interstitialLoading = false;
                rewardedAdId = interstitialAdId = "";

                Debug.LogError("[Ads] Tapsell disabled (" + operation + "): the Tapsell Mediation Android libraries are probably missing from the build " +
                               "( custom Gradle templates + Android Resolver ). " + exception);
            }

            /// <summary>
            /// A request that never answered stops counting ( and blocking reloads )
            /// </summary>
            public bool IsLoading(AdFormat format)
            {
                return format == AdFormat.Rewarded
                    ? rewardedLoading && Time.realtimeSinceStartup - rewardedLoadAt < LoadAnswerSeconds
                    : interstitialLoading && Time.realtimeSinceStartup - interstitialLoadAt < LoadAnswerSeconds;
            }

            public void Load(AdFormat format)
            {
                if (available == false) return;

                try
                {
                    LoadNow(format);
                }
                catch (Exception exception)
                {
                    Disable("Request", exception);
                }
            }

            void LoadNow(AdFormat format)
            {
                if (format == AdFormat.Rewarded)
                {
                    if (IsLoading(format) || IsReady(format) || string.IsNullOrEmpty(TapsellRewardedZoneId)) return;

                    rewardedLoading = true;
                    rewardedLoadAt = Time.realtimeSinceStartup;

                    Tapsell.Mediation.Tapsell.RequestRewardedAd(
                        TapsellRewardedZoneId,
                        adId => owner.Post(() => { rewardedLoading = false; rewardedAdId = adId ?? ""; }),
                        error => owner.Post(() => { rewardedLoading = false; Debug.LogWarning("[Ads] Tapsell rewarded load error: " + error); }));
                }
                else
                {
                    if (IsLoading(format) || IsReady(format) || string.IsNullOrEmpty(TapsellInterstitialZoneId)) return;

                    interstitialLoading = true;
                    interstitialLoadAt = Time.realtimeSinceStartup;

                    Tapsell.Mediation.Tapsell.RequestInterstitialAd(
                        TapsellInterstitialZoneId,
                        adId => owner.Post(() => { interstitialLoading = false; interstitialAdId = adId ?? ""; }),
                        error => owner.Post(() => { interstitialLoading = false; Debug.LogWarning("[Ads] Tapsell interstitial load error: " + error); }));
                }
            }

            public void Show(AdFormat format, AdShowCallbacks callbacks)
            {
                try
                {
                    ShowNow(format, callbacks);
                }
                catch (Exception exception)
                {
                    Disable("Show", exception);

                    // Nothing was presented: the mediator tries Adivery
                    throw;
                }
            }

            void ShowNow(AdFormat format, AdShowCallbacks callbacks)
            {
                // A loaded Tapsell ad can be shown only once
                if (format == AdFormat.Rewarded)
                {
                    string adId = rewardedAdId;
                    rewardedAdId = "";

                    Tapsell.Mediation.Tapsell.ShowRewardedAd(
                        adId,
                        () => owner.Post(callbacks.Displayed),
                        () => { },
                        state => owner.Post(callbacks.Closed),
                        error => { Debug.LogWarning("[Ads] Tapsell rewarded show error: " + error); owner.Post(callbacks.Failed); },
                        () => owner.Post(callbacks.Rewarded));
                }
                else
                {
                    string adId = interstitialAdId;
                    interstitialAdId = "";

                    Tapsell.Mediation.Tapsell.ShowInterstitialAd(
                        adId,
                        () => owner.Post(callbacks.Displayed),
                        () => { },
                        state => owner.Post(callbacks.Closed),
                        error => { Debug.LogWarning("[Ads] Tapsell interstitial show error: " + error); owner.Post(callbacks.Failed); });
                }
            }
        }
    }
}
