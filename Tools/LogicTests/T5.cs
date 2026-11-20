using EndlessCarChase.Levels;
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
  static void PassLevels(int count){ var all = ECCLevelCatalog.Instance.AllLevels; for (int i=0;i<count;i++) ECCSave.SetStars(all[i].id, 1); }
  static void TestInterstitialSchedule(){
    ECCSave.Data.recommendShownDay = ECCDaily.Today; ECCSave.Data.reviewPromptShown = true; // keep the other transition popups away
    var fake = FakeAds.Install(); fake.interstitialReady = true; int nav = 0;
    Check(ECCInterstitialSchedule.IntervalFor(0)==0 && ECCInterstitialSchedule.IntervalFor(10)==0 && ECCInterstitialSchedule.IntervalFor(11)==4 && ECCInterstitialSchedule.IntervalFor(50)==4 && ECCInterstitialSchedule.IntervalFor(51)==3, "tiers: none through 10, every 4 up to 50, every 3 after");
    // Levels 1-10: never
    for (int i=1;i<=10;i++){ PassLevels(i); ECCInterstitialSchedule.RecordWin("w"+i); ECCPostVictoryFlow.Leave(true, ()=>nav++); Coroutine.Run(2.5f); }
    Check(fake.interstitials==0 && nav==10 && ECCSave.Data.levelsSinceInterstitial==0, "no interstitial through level 10, wins there don't count");
    // After level 10: every 4 completed levels
    for (int i=11;i<=13;i++){ PassLevels(i); ECCInterstitialSchedule.RecordWin("w"+i); ECCPostVictoryFlow.Leave(true, ()=>nav++); Coroutine.Run(2.5f); }
    Check(fake.interstitials==0 && !ECCInterstitialSchedule.IsDue, "levels 11-13: not yet");
    PassLevels(14); ECCInterstitialSchedule.RecordWin("w14"); ECCInterstitialSchedule.RecordWin("w14");
    Check(ECCInterstitialSchedule.IsDue && ECCSave.Data.levelsSinceInterstitial==4, "4th completed level after 10: due ( a duplicate report counted once )");
    ECCPostVictoryFlow.Leave(false, ()=>nav++); Check(ECCAdLoadingPopup.IsOpen && ECCPostVictoryFlow.InterstitialSelected, "shown on any transition out of the win ( Menu too ), loading popup first");
    ECCPostVictoryFlow.Leave(false, ()=>nav++); Coroutine.Run(2.5f);
    Check(fake.interstitials==1 && ECCSave.Data.levelsSinceInterstitial==0 && nav==14, "shown once, counter reset only after the display, repeated tap ignored");
    Restart(); Check(ECCSave.Data.levelsSinceInterstitial==0, "counter saved");
    // Failure: silent skip, stays due, no NoAds
    for (int i=15;i<=18;i++){ PassLevels(i); ECCInterstitialSchedule.RecordWin("w"+i); }
    fake.interstitialReady = false; ECCPostVictoryFlow.Leave(true, ()=>nav++); Coroutine.Run(3);
    Check(fake.interstitials==1 && ECCInterstitialSchedule.IsDue && nav==15 && !FindAllRoots().Any(t=>t.gameObject.name=="CanvasNoAds" && t.gameObject.activeSelf), "not ready: skipped silently, no NoAds, still due");
    Restart(); Check(ECCInterstitialSchedule.IsDue, "still due after a restart");
    fake.interstitialReady = true;
    ECCPostVictoryFlow.Leave(true, ()=>nav++, true); Coroutine.Run(2.5f);
    Check(fake.interstitials==1 && nav==16 && ECCInterstitialSchedule.IsDue, "never right after a rewarded video of the same transition");
    ECCPostVictoryFlow.Leave(true, ()=>nav++); Coroutine.Run(2.5f);
    Check(fake.interstitials==2 && !ECCInterstitialSchedule.IsDue, "shown at the next transition");
    // Replaying early levels follows the active tier
    for (int i=0;i<4;i++) ECCInterstitialSchedule.RecordWin("replay"+i);
    Check(ECCInterstitialSchedule.IsDue, "replay wins of early levels count once past level 10");
    // After level 50: every 3
    ECCInterstitialSchedule.MarkShown(); PassLevels(51);
    for (int i=0;i<3;i++) ECCInterstitialSchedule.RecordWin("late"+i);
    Check(ECCInterstitialSchedule.CurrentInterval==3 && ECCInterstitialSchedule.IsDue, "after level 50: every 3 completed levels");
  }
  static void TestInterstitialScheduleMigrationAndEvents(){
    // A player who updates while already past level 50 follows the late tier at once
    PassLevels(60); Check(ECCInterstitialSchedule.CurrentInterval==3, "update past level 50: late tier at once");
    Wipe(); PassLevels(12); Check(ECCInterstitialSchedule.CurrentInterval==4, "update past level 10: early tier at once");
    // Only normal wins count: not losses, the daily challenge ( own result ) or skips
    var level = ECCLevelCatalog.Instance.AllLevels[12];
    ECCGameController gc; var mode = MakeLevelRun(out gc, level);
    mode.ShowResultScreen(false); FindButton("ButtonMenu").onClick.Invoke();
    Check(ECCSave.Data.levelsSinceInterstitial==0, "a loss doesn't count");
    mode.ShowResultScreen(false); PlayerPrefs.SetInt("Money", ECCLevelProgress.SkipPrice+5); FindButton("ButtonSkip").onClick.Invoke();
    Check(ECCSave.Data.levelsSinceInterstitial==0, "a skip doesn't count");
    mode = MakeLevelRun(out gc, level); mode.ShowResultScreen(true); mode.ShowResultScreen(true);
    Check(ECCSave.Data.levelsSinceInterstitial==1, "a normal win counts once per run");
  }
}
