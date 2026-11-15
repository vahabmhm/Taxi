using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Modes; using EndlessCarChase.Levels;
public static partial class T {
  static void WinLevels(int n){ var levels = ECCLevelCatalog.Instance.AllLevels; for (int i=0;i<n;i++) ECCReviewPrompt.RecordNormalWin(levels[i].id); }
  static void TestReviewEligibility(){
    WinLevels(2); ECCReviewPrompt.RecordNormalWin(ECCLevelCatalog.Instance.AllLevels[0].id);
    Check(!ECCReviewPrompt.IsEligible, "2 distinct wins (one repeated): not yet");
    int nav=0; WinLevels(3);
    Check(ECCReviewPrompt.IsEligible, "3 distinct normal wins: eligible");
    ECCPostVictoryFlow.Leave(false, ()=>nav++);
    Check(nav==1 && !ECCReviewPromptUI.IsOpen && !ECCSave.Data.reviewPromptShown, "not on Retry/Menu");
    ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(ECCReviewPromptUI.IsOpen && nav==1 && ECCSave.Data.reviewPromptShown, "shown on Next Level, marked when displayed");
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasReviewPrompt");
    Check(Texts(root).Contains(ECCPersianText.Fix("ثبت نظر در کافه‌بازار")) && Texts(root).Contains(ECCPersianText.Fix("نظرت رو در کافه‌بازار ثبت کن")) && Texts(root).Contains(ECCPersianText.Fix("بعداً")), "Bazaar texts");
    FindAll<Button>(root,"ButtonLater")[0].onClick.Invoke();
    Check(!ECCReviewPromptUI.IsOpen && nav==2, "later closes and continues to the next level");
    Restart(); ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(!ECCReviewPromptUI.IsOpen && nav==3, "never again, also after restart");
  }
  static void TestReviewPriority(){
    WinLevels(3); AddGames(2); int nav=0;
    ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(ECCGameRecommendationUI.IsOpen && !ECCReviewPromptUI.IsOpen && !ECCSave.Data.reviewPromptShown, "recommendation on this Next Level: review deferred");
    FindAll<Button>(UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasGameRecommendation"),"ButtonClose")[0].onClick.Invoke();
    Check(!ECCReviewPromptUI.IsOpen && nav==1, "no review stacked after the recommendation");
    typeof(ECCAds).GetProperty("IsInterstitialScheduled").SetValue(null,true);
    ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(!ECCReviewPromptUI.IsOpen && nav==2 && !ECCSave.Data.reviewPromptShown, "interstitial scheduled: review deferred");
    typeof(ECCAds).GetProperty("IsInterstitialScheduled").SetValue(null,false);
    ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(ECCReviewPromptUI.IsOpen && ECCSave.Data.reviewPromptShown, "deferred prompt appears on a later Next Level");
    ECCGameCatalog.Instance.games.Clear();
  }
  static void TestReviewStores(){
    var m = BazaarReviewManager.Instance;
    Check(m.store==ECCReviewStore.Bazaar && m.IntentAction=="android.intent.action.EDIT" && m.IntentData=="bazaar://details?id="+m.PackageName && m.StorePackage=="com.farsitel.bazaar", "Bazaar intent");
    Application.lastUrl=null; Check(!m.OpenReview() && Application.lastUrl=="https://cafebazaar.ir/app/"+m.PackageName, "no Android: Bazaar web page fallback");
    m.store = ECCReviewStore.Myket;
    Check(m.IntentAction=="android.intent.action.VIEW" && m.IntentData=="myket://comment?id="+m.PackageName && m.StorePackage=="ir.mservices.market", "Myket intent");
    Application.lastUrl=null; m.OpenReview(); Check(Application.lastUrl=="https://myket.ir/app/"+m.PackageName, "Myket never falls back to Bazaar");
    WinLevels(3); ECCPostVictoryFlow.Leave(true, ()=>{});
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasReviewPrompt");
    Check(Texts(root).Contains(ECCPersianText.Fix("ثبت نظر در مایکت")) && Texts(root).Contains(ECCPersianText.Fix("نظرت رو در مایکت ثبت کن")), "Myket texts follow the store field");
    Application.lastUrl=null; FindAll<Button>(root,"ButtonReview")[0].onClick.Invoke();
    Check(Application.lastUrl=="https://myket.ir/app/"+m.PackageName && !ECCReviewPromptUI.IsOpen, "review button uses the manager");
    m.store = ECCReviewStore.Bazaar;
  }
  static void TestChallengeWinNotCounted(){
    ECCGameController gc; var mode = MakeChallengeRun(out gc); mode.ShowResultScreen(true);
    Check(ECCSave.Data.reviewWins.Count==0, "daily challenge victory not counted for the review prompt");
  }
}
