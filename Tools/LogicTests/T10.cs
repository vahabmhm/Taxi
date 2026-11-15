using System; using System.Linq; using UnityEngine; using EndlessCarChase;
public static partial class T {
  static void TestLoadingPopupTiming(){
    var fake = FakeAds.Install(); int results=0; bool last=false;
    Check(ECCAds.ShowRewarded("t", r=>{results++; last=r;}), "request accepted");
    Check(!ECCAds.ShowRewarded("t", r=>results++), "second request while busy refused (no duplicate)");
    Check(ECCAdLoadingPopup.IsOpen && fake.shows==0, "loading popup immediately, video not yet");
    Coroutine.Run(1.9f); Check(ECCAdLoadingPopup.IsOpen && fake.shows==0, "still loading before 2 seconds even though ready");
    Coroutine.Run(0.3f); Check(!ECCAdLoadingPopup.IsOpen && fake.shows==1 && results==1 && last, "after 2 seconds: popup closes, one video, verified reward");
    Check(!ECCAds.IsBusy, "flow finished");
  }
  static void TestLoadingPopupFallback(){
    var a = FakeAds.Install(); a.ready=false; var b = FakeAds.Install(); bool last=false;
    ECCAds.ShowRewarded("t", r=>last=r); Coroutine.Run(2.5f);
    Check(a.shows==0 && b.shows==1 && last, "preferred not ready: the other provider is used for the same request");
    b.ready=false; a.ready=false; ECCNoAdsPanel.Hide();
    ECCAds.ShowRewarded("t", r=>last=r); Coroutine.Run(4);
    Check(ECCAdLoadingPopup.IsOpen, "not ready and no failure reported: keeps waiting after 2 seconds");
    a.ready=true; Coroutine.Run(0.3f);
    Check(a.shows==1 && !ECCAdLoadingPopup.IsOpen, "video shown as soon as it becomes ready");
  }
  static void TestLoadingPopupAllFail(){
    var a = FakeAds.Install(); a.ready=false; a.failed=true; var b = FakeAds.Install(); b.ready=false; b.failed=true;
    int results=0; bool last=true;
    ECCAds.ShowRewarded("t", r=>{results++; last=r;}); Coroutine.Run(2.2f);
    Check(!ECCAdLoadingPopup.IsOpen && results==1 && !last && !ECCAds.IsBusy, "both failed: popup closes at 2 seconds without waiting, no reward");
    Check(FindAllRoots().Any(t=>t.gameObject.name=="CanvasNoAds" && t.gameObject.activeSelf), "NoAds panel shown");
    Wipe(); var c = FakeAds.Install(); c.ready=false; results=0;
    ECCAds.ShowRewarded("t", r=>{results++; last=r;}); Coroutine.Run(ECCAds.ReadyTimeout+0.5f);
    Check(results==1 && !last && !ECCAdLoadingPopup.IsOpen && !ECCAds.IsBusy, "never ready: timeout, NoAds, input restored");
  }
  static void TestInterstitialPath(){
    var fake = FakeAds.Install(); fake.interstitialReady=true; bool shown=false;
    ECCAds.ShowInterstitial("t", r=>shown=r);
    Check(ECCAds.IsInterstitialScheduled && ECCAdLoadingPopup.IsOpen, "interstitial: scheduled flag and loading popup");
    Coroutine.Run(2.3f); Check(shown && fake.interstitials==1 && !ECCAds.IsInterstitialScheduled && !ECCAds.IsBusy, "interstitial shown once after 2 seconds");
    fake.interstitialReady=false; shown=true; ECCAds.ShowInterstitial("t", r=>shown=r); Coroutine.Run(7);
    Check(!shown && !FindAllRoots().Any(t=>t.gameObject.name=="CanvasNoAds" && t.gameObject.activeSelf) && !ECCAds.IsInterstitialScheduled, "unavailable interstitial skipped quietly");
  }
  static System.Collections.Generic.List<Transform> FindAllRoots(){ return UnityEngine.Object.FindAllRoots(); }
}
