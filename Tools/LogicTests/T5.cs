using System; using System.Linq; using UnityEngine; using EndlessCarChase;
// Task 5: AdMediator ( rotation / fallback used by AdsManagerNew ) with stand-in networks. Simulated, not real SDKs
public class FakeNet : IAdNetwork {
  public AdNetworkId id; public bool rewardedReady = true, interstitialReady = true, loadingFlag, throwOnShow; public int shows, loads; public AdShowCallbacks last;
  public AdNetworkId Id { get { return id; } }
  public bool IsReady(AdFormat f) { return f == AdFormat.Rewarded ? rewardedReady : interstitialReady; }
  public bool IsLoading(AdFormat f) { return loadingFlag; }
  public void Load(AdFormat f) { loads++; }
  public void Show(AdFormat f, AdShowCallbacks c) { if (throwOnShow) throw new Exception("no sdk"); shows++; last = c; }
}
public static partial class T {
  static AdOutcome? outcome; static int outcomes;
  static bool Req(AdMediator m, AdFormat f = AdFormat.Rewarded, float t = 0) { outcome = null; outcomes = 0; return m.Request(f, t, o => { outcome = o; outcomes++; }); }
  static void TestMediatorRotation(){
    var store = new MemoryRotationStore(); var ad = new FakeNet{id=AdNetworkId.Adivery}; var tp = new FakeNet{id=AdNetworkId.Tapsell};
    var m = new AdMediator(store, tp, ad);
    Check(m.Preferred == AdNetworkId.Adivery, "Adivery preferred first");
    Req(m); Check(ad.shows==1 && tp.shows==0, "first request shown by Adivery");
    ad.last.Displayed(); ad.last.Rewarded(); ad.last.Closed();
    Check(outcome==AdOutcome.Rewarded && outcomes==1, "verified reward reported once");
    Check(m.Preferred == AdNetworkId.Tapsell, "after Adivery displayed, Tapsell preferred");
    Req(m, AdFormat.Interstitial); Check(tp.shows==1, "rotation shared with interstitial: Tapsell shows next");
    tp.last.Displayed(); tp.last.Closed();
    Check(outcome==AdOutcome.ClosedNoReward && m.Preferred==AdNetworkId.Adivery, "after Tapsell displayed, Adivery preferred again");
  }
  static void TestMediatorFallback(){
    var ad = new FakeNet{id=AdNetworkId.Adivery}; var tp = new FakeNet{id=AdNetworkId.Tapsell}; var store = new MemoryRotationStore();
    var m = new AdMediator(store, ad, tp);
    ad.rewardedReady=false; Req(m); Check(tp.shows==1 && ad.shows==0, "Adivery not ready: Tapsell for the same request");
    tp.last.Failed(); Check(outcome==AdOutcome.Unavailable && outcomes==1 && store.LastShown==-1, "Tapsell fails before display, Adivery not ready: unavailable, no rotation");
    ad.rewardedReady=true; Req(m); ad.last.Failed();
    Check(tp.shows==2 && outcomes==0, "Adivery fails before display: Tapsell tried for the same request");
    tp.last.Displayed(); tp.last.Rewarded(); tp.last.Closed(); Check(outcome==AdOutcome.Rewarded && store.LastShown==(int)AdNetworkId.Tapsell, "fallback reward + rotation by the network that displayed");
    // Tapsell preferred now; it throws ( missing library ) -> Adivery
    tp.throwOnShow=true; Req(m); Check(ad.shows==2 && outcomes==0, "Tapsell throws before display: Adivery tried");
    ad.last.Displayed(); ad.last.Closed(); Check(outcome==null, "closed without reward: waits a grace time for a late reward");
    ad.last.Rewarded(); Check(outcome==AdOutcome.Rewarded && outcomes==1, "late reward within grace still pays once");
    ad.rewardedReady=false; tp.rewardedReady=false; Req(m); Check(outcome==AdOutcome.Unavailable, "both unavailable: Unavailable ( NoAds )");
  }
  static void TestMediatorDuplicatesAndSkips(){
    var ad = new FakeNet{id=AdNetworkId.Adivery}; var tp = new FakeNet{id=AdNetworkId.Tapsell};
    var m = new AdMediator(new MemoryRotationStore(), ad, tp);
    Req(m); var c = ad.last;
    Check(!m.Request(AdFormat.Rewarded, 0, o => outcomes+=100), "second request while busy refused ( no two full-screen ads )");
    c.Displayed(); c.Rewarded(); c.Closed(); c.Rewarded(); c.Closed(); c.Failed();
    Check(outcomes==1 && outcome==AdOutcome.Rewarded, "duplicate SDK callbacks ignored");
    Req(m, AdFormat.Rewarded, 0); tp.last.Displayed(); tp.last.Closed(); m.Tick(1.5f);
    Check(outcome==AdOutcome.ClosedNoReward && outcomes==1, "skipped video: no reward after the grace time");
    Req(m, AdFormat.Rewarded, 10); ad.last.Displayed(); ad.last.Failed();
    Check(outcome==AdOutcome.FailedAfterDisplay && tp.shows==1, "failure after display: no second ad, no reward");
    Req(m, AdFormat.Rewarded, 20); m.NoteAppPaused(); var x = tp.last; x.Failed();
    Check(outcome==AdOutcome.FailedAfterDisplay && ad.shows==2, "app paused by the ad counts as displayed: late error never starts another ad");
    Req(m, AdFormat.Rewarded, 30); m.Tick(30 + m.PreDisplayTimeoutSeconds + 1);
    Check(outcome==AdOutcome.FailedAfterDisplay && !m.Busy, "no display signal: request released without a reward");
  }
  static void TestAdsUnavailableShowsNoAds(){
    var fake = FakeAds.Install(ECCAdResult.Unavailable); int results=0; bool last=true;
    ECCAds.ShowRewarded("t", r=>{results++; last=r;}); Coroutine.Run(2.3f);
    Check(results==1 && !last && !ECCAds.IsBusy, "provider unavailable at show time: no reward, input restored");
    Check(FindAllRoots().Any(t=>t.gameObject.name=="CanvasNoAds" && t.gameObject.activeSelf), "NoAds panel shown");
    ECCNoAdsPanel.Hide(); fake.result = ECCAdResult.Failed; ECCAds.ShowRewarded("t", r=>{results++; last=r;}); Coroutine.Run(2.3f);
    Check(results==2 && !last && !FindAllRoots().Any(t=>t.gameObject.name=="CanvasNoAds" && t.gameObject.activeSelf), "failure after display: no reward and no NoAds");
  }
}
public static partial class T {
  static void TestInterstitialEveryFourNextLevels(){
    ECCSave.Data.recommendShownDay = ECCDaily.Today; ECCSave.Data.reviewPromptShown = true; // keep the other transition popups away
    var fake = FakeAds.Install(); fake.interstitialReady = true; int navigations = 0;
    for (int i = 0; i < 3; i++) { ECCPostVictoryFlow.Leave(true, () => navigations++); Coroutine.Run(0.1f); }
    Check(navigations==3 && fake.interstitials==0 && !ECCAdLoadingPopup.IsOpen, "Next Level 1-3: no interstitial, no loading popup");
    ECCPostVictoryFlow.Leave(false, () => navigations++); Check(navigations==4 && fake.interstitials==0, "Retry/Menu after a win doesn't count or show");
    ECCPostVictoryFlow.Leave(true, () => navigations++);
    Check(navigations==4 && ECCAdLoadingPopup.IsOpen, "4th Next Level: loading popup, navigation waits for the ad");
    ECCPostVictoryFlow.Leave(true, () => navigations++); Check(navigations==4, "repeated tap while the ad runs ignored");
    Coroutine.Run(2.3f); Check(fake.interstitials==1 && navigations==5 && ECCSave.Data.levelsSinceInterstitial==0, "interstitial shown once, then the next level loads, counter reset");
    Restart(); Check(ECCSave.Data.levelsSinceInterstitial==0, "counter saved");
    for (int i = 0; i < 3; i++) ECCPostVictoryFlow.Leave(true, () => navigations++);
    fake.interstitialReady = false; ECCPostVictoryFlow.Leave(true, () => navigations++);
    Check(navigations==9 && fake.interstitials==1 && !ECCAdLoadingPopup.IsOpen, "due but not ready: skipped quietly, no waiting");
    Restart(); fake.interstitialReady = true; ECCPostVictoryFlow.Leave(true, () => navigations++); Coroutine.Run(2.3f);
    Check(fake.interstitials==2 && navigations==10, "still due after a restart: shown on the next Next Level");
  }
}
