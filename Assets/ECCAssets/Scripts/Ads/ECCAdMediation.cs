// Provider rotation / fallback for AdsManagerNew ( Task 5 ). Ported unchanged in logic from the developer's Crowd Rush project
// ( Crowd1/Assets/GameSource/_Scripts/_Ads/AdMediation.cs ), which was itself adapted from ConnectBalls.
using System;
using System.Collections.Generic;

namespace EndlessCarChase
{
    public enum AdFormat
    {
        Rewarded,
        Interstitial
    }

    // Values match the developer's ConnectBalls / Crowd Rush AdsManagerNew so the saved rotation key keeps
    // the same meaning.
    public enum AdNetworkId
    {
        Tapsell = 0,
        Adivery = 1
    }

    public enum AdOutcome
    {
        Rewarded,          // shown and the SDK confirmed the reward
        ClosedNoReward,    // shown, closed without reward (skip / interstitial end)
        FailedAfterDisplay,// error once the ad was (or may have been) on screen
        Unavailable        // no network could present an ad
    }

    // Callbacks a network reports for ONE show attempt. Stale attempts are
    // ignored by the mediator, so a network may call them late or twice.
    public sealed class AdShowCallbacks
    {
        public Action Displayed;
        public Action Rewarded;
        public Action Closed;
        public Action Failed;
    }

    public interface IAdNetwork
    {
        AdNetworkId Id { get; }
        bool IsReady(AdFormat format);
        // A load request is still waiting for its loaded / error answer.
        bool IsLoading(AdFormat format);
        void Load(AdFormat format);
        void Show(AdFormat format, AdShowCallbacks callbacks);
    }

    // Where the last network that actually displayed an ad is saved.
    public interface IAdRotationStore
    {
        int LastShown { get; set; } // -1 = none yet
    }

    public sealed class MemoryRotationStore : IAdRotationStore
    {
        public int LastShown { get; set; } = -1;
    }

    // Optional loading phase before the ad (Task 10's pre-ad popup).
    public sealed class AdWait
    {
        public float MinSeconds;      // the popup stays at least this long
        public float MaxSeconds;      // give up waiting for a load after this
        public Action BeforeShow;     // close the popup right before presenting
    }

    // Provider rotation and fallback for every full-screen ad (rewarded and
    // interstitial share one rotation state). Pure C# so it can be tested
    // without Unity or the SDKs.
    //
    // - Adivery is preferred first. After a network DISPLAYS an ad the other
    //   network becomes preferred. Requests, load failures and unavailable
    //   ads never rotate.
    // - If the preferred network is not ready, or fails before presenting,
    //   the other network is tried for the same request.
    // - Once an ad may be on screen it is never replaced by a second one.
    // - Exactly one outcome is reported per request.
    // - With an AdWait, nothing is shown before MinSeconds; after that the ad
    //   is shown as soon as a network is ready, or the request ends as
    //   Unavailable once no network is still loading (or MaxSeconds passed).
    public sealed class AdMediator
    {
        // Reward callbacks sometimes arrive just after the close callback.
        public float RewardGraceSeconds = 1f;

        // No display signal at all: the request is released as a failure,
        // without trying the other network (it might still appear).
        public float PreDisplayTimeoutSeconds = 30f;

        // A lost close callback must not block ads forever.
        public float RequestTimeoutSeconds = 300f;

        private readonly List<IAdNetwork> networks = new List<IAdNetwork>();
        private readonly IAdRotationStore store;

        private bool active;
        private AdFormat format;
        private Action<AdOutcome> onDone;
        private readonly HashSet<AdNetworkId> tried = new HashSet<AdNetworkId>();
        private IAdNetwork current;
        private int attempt;
        private bool displayed;
        private bool rewarded;
        private AdWait wait;
        private float requestedAt;
        private float startedAt;
        private float closedAt = -1f;
        private float now;

        public AdMediator(IAdRotationStore store, params IAdNetwork[] networks)
        {
            this.store = store ?? new MemoryRotationStore();
            foreach (IAdNetwork network in networks ?? new IAdNetwork[0])
                if (network != null)
                    this.networks.Add(network);
        }

        public bool HasNetworks { get { return networks.Count > 0; } }

        public bool Busy { get { return active; } }

        // True during the loading phase, before any network was asked to show.
        public bool Waiting { get { return active && wait != null; } }

        public AdNetworkId Preferred
        {
            get { return store.LastShown == (int)AdNetworkId.Adivery ? AdNetworkId.Tapsell : AdNetworkId.Adivery; }
        }

        // Network of the attempt in progress (only meaningful while Busy).
        public AdNetworkId? CurrentNetwork { get { return active && current != null ? current.Id : (AdNetworkId?)null; } }

        public bool IsReady(AdFormat adFormat)
        {
            foreach (IAdNetwork network in networks)
                if (SafeReady(network, adFormat))
                    return true;
            return false;
        }

        public bool IsLoading(AdFormat adFormat)
        {
            foreach (IAdNetwork network in networks)
                if (SafeLoading(network, adFormat))
                    return true;
            return false;
        }

        public void LoadAll(AdFormat adFormat)
        {
            foreach (IAdNetwork network in networks)
                SafeLoad(network, adFormat);
        }

        // Returns false (and never calls done) while another ad is in progress.
        public bool Request(AdFormat adFormat, float time, Action<AdOutcome> done, AdWait loading = null)
        {
            if (active)
                return false;

            active = true;
            format = adFormat;
            onDone = done;
            tried.Clear();
            current = null;
            displayed = false;
            rewarded = false;
            closedAt = -1f;
            requestedAt = time;
            startedAt = time;
            now = time;
            wait = loading;
            if (wait == null)
            {
                TryNext();
                return true;
            }

            if (!IsReady(format))
                LoadAll(format);
            EvaluateWait();
            return true;
        }

        // Call regularly while Busy with a real-time clock.
        public void Tick(float time)
        {
            now = time;
            if (!active)
                return;

            if (wait != null)
            {
                EvaluateWait();
                return;
            }

            if (closedAt >= 0f && time - closedAt >= RewardGraceSeconds)
            {
                Finish(rewarded ? AdOutcome.Rewarded : AdOutcome.ClosedNoReward);
                return;
            }

            if (!displayed && time - startedAt >= PreDisplayTimeoutSeconds)
            {
                Finish(AdOutcome.FailedAfterDisplay);
                return;
            }

            if (time - startedAt >= RequestTimeoutSeconds)
                Finish(rewarded ? AdOutcome.Rewarded : AdOutcome.FailedAfterDisplay);
        }

        // The app lost focus during an attempt: an ad activity is on screen.
        public void NoteAppPaused()
        {
            if (active && current != null)
                MarkDisplayed();
        }

        private void EvaluateWait()
        {
            float elapsed = now - requestedAt;
            if (elapsed < wait.MinSeconds)
                return;

            if (IsReady(format))
            {
                Action beforeShow = wait.BeforeShow;
                wait = null;
                startedAt = now;
                beforeShow?.Invoke();
                if (active)
                    TryNext();
                return;
            }

            // Every network answered "no ad" (or took too long): stop waiting.
            if (!IsLoading(format) || elapsed >= wait.MaxSeconds)
            {
                wait = null;
                Finish(AdOutcome.Unavailable);
            }
        }

        private void TryNext()
        {
            IAdNetwork preferred = null;
            IAdNetwork other = null;
            foreach (IAdNetwork network in networks)
            {
                if (tried.Contains(network.Id) || !SafeReady(network, format))
                    continue;
                if (network.Id == Preferred)
                    preferred = network;
                else if (other == null)
                    other = network;
            }

            IAdNetwork next = preferred ?? other;
            if (next == null)
            {
                LoadAll(format);
                Finish(AdOutcome.Unavailable);
                return;
            }

            tried.Add(next.Id);
            current = next;
            int id = ++attempt;
            var callbacks = new AdShowCallbacks
            {
                Displayed = () => { if (Is(id)) MarkDisplayed(); },
                Rewarded = () => OnRewarded(id),
                Closed = () => OnClosed(id),
                Failed = () => OnFailed(id)
            };

            try
            {
                next.Show(format, callbacks);
            }
            catch (Exception)
            {
                // Threw before anything could be presented.
                if (Is(id) && !displayed)
                {
                    SafeLoad(next, format);
                    TryNext();
                }
            }
        }

        private bool Is(int id)
        {
            return active && id == attempt;
        }

        private void MarkDisplayed()
        {
            if (displayed)
                return;
            displayed = true;
            store.LastShown = (int)current.Id;
        }

        private void OnRewarded(int id)
        {
            if (!Is(id) || format != AdFormat.Rewarded)
                return;
            rewarded = true;
            if (closedAt >= 0f)
                Finish(AdOutcome.Rewarded);
        }

        private void OnClosed(int id)
        {
            if (!Is(id) || closedAt >= 0f)
                return;
            MarkDisplayed();
            SafeLoad(current, format);
            if (rewarded || format == AdFormat.Interstitial)
            {
                Finish(rewarded ? AdOutcome.Rewarded : AdOutcome.ClosedNoReward);
                return;
            }

            closedAt = now;
        }

        private void OnFailed(int id)
        {
            if (!Is(id) || closedAt >= 0f)
                return;
            SafeLoad(current, format);
            if (!displayed)
            {
                TryNext();
                return;
            }

            Finish(rewarded ? AdOutcome.Rewarded : AdOutcome.FailedAfterDisplay);
        }

        private void Finish(AdOutcome outcome)
        {
            if (!active)
                return;
            active = false;
            wait = null;
            attempt++; // every later callback of this request is stale
            Action<AdOutcome> done = onDone;
            onDone = null;
            done?.Invoke(outcome);
        }

        private static bool SafeReady(IAdNetwork network, AdFormat adFormat)
        {
            try
            {
                return network.IsReady(adFormat);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool SafeLoading(IAdNetwork network, AdFormat adFormat)
        {
            try
            {
                return network.IsLoading(adFormat);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void SafeLoad(IAdNetwork network, AdFormat adFormat)
        {
            try
            {
                network.Load(adFormat);
            }
            catch (Exception)
            {
                // a broken network simply stays not-ready
            }
        }
    }
}
