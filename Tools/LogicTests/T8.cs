using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static void AddGames(int n){ var c = ECCGameCatalog.Instance; c.games.Clear(); for (int i=0;i<n;i++) c.games.Add(new ECCGameEntry{packageName="com.example.g"+i, title="بازی "+i, verified=true}); }
  static void TestRecommendationRules(){
    AddGames(3);
    int navigated=0;
    // Install day: the challenge popup never shows today, so the recommendation may show after a normal win
    Check(ECCDaily.IsInstallDay && ECCGameRecommendation.IsDueToday, "due after the challenge popup is not pending");
    NextDay();
    Check(ECCDailyChallenge.ShouldAutoShowPopup && !ECCGameRecommendation.IsDueToday, "daily challenge popup has priority");
    ECCDailyChallenge.MarkPopupShown();
    Check(ECCGameRecommendation.IsDueToday, "due once the challenge popup was dealt with");
    ECCChallengePopup.Open(new GameObject("gc").AddComponent<ECCGameController>());
    ECCPostVictoryFlow.Leave(true, ()=>navigated++);
    Check(navigated==1 && !ECCGameRecommendationUI.IsOpen, "no stacking over another popup: navigation continues");
    Wipe(); AddGames(3); NextDay(); ECCDailyChallenge.MarkPopupShown();
    var game = ECCGameRecommendation.TodayGame; Restart();
    Check(ECCGameRecommendation.TodayGame.packageName==game.packageName, "today's game kept through restart");
    ECCPostVictoryFlow.Leave(true, ()=>navigated++);
    Check(ECCGameRecommendationUI.IsOpen && navigated==1 && ECCDaily.IsToday(ECCSave.Data.recommendShownDay), "shown on leaving a normal win, before navigation");
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasGameRecommendation");
    Check(Texts(root).Contains(ECCPersianText.Fix("در کافه بازار نصب کنید")) && Texts(root).Contains(ECCPersianText.Fix(game.title)) && Texts(root).Contains(ECCPersianText.Fix("پیشنهاد بازی امروز")), "popup texts");
    FindAll<Button>(root,"ButtonClose")[0].onClick.Invoke();
    Check(!ECCGameRecommendationUI.IsOpen && navigated==2, "closing continues the navigation");
    ECCPostVictoryFlow.Leave(true, ()=>navigated++); Restart(); ECCPostVictoryFlow.Leave(true, ()=>navigated++);
    Check(!ECCGameRecommendationUI.IsOpen && navigated==4, "only once per day, also after restart");
    string prev = ECCSave.Data.recommendPackage; bool differs = true;
    for (int d=0; d<10; d++){ NextDay(); ECCDailyChallenge.MarkPopupShown(); var g = ECCGameRecommendation.TodayGame; if (g.packageName==prev) differs=false; prev=g.packageName; }
    Check(differs, "never the same game on consecutive days with 2+ games");
    NextDay(); ECCDailyChallenge.MarkPopupShown(); Check(ECCGameRecommendation.IsDueToday, "a new recommendation on a later day");
  }
  static void TestRecommendationEmptyCatalog(){
    ECCGameCatalog.Instance.games.Clear(); int navigated=0;
    ECCPostVictoryFlow.Leave(true, ()=>navigated++);
    Check(navigated==1 && !ECCGameRecommendationUI.IsOpen && !ECCDaily.IsToday(ECCSave.Data.recommendShownDay), "no verified game: skipped silently");
    var c = ECCGameCatalog.Instance; c.games.Add(new ECCGameEntry{packageName="com.example.x", title="x", verified=false});
    Check(ECCGameRecommendation.TodayGame==null, "unverified games are never recommended");
    c.games.Clear();
  }
}
