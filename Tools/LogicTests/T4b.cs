using System; using System.IO; using System.Linq; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static string GamesJson { get { return File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("ECC_ROOT") ?? "/home/user/EndlessCar", "Assets/ECCAssets/Resources/ECCOtherGames.json")); } }
  static void UseJsonCatalog(){ Resources.loads["ECCOtherGames"] = new TextAsset{ text = GamesJson }; typeof(ECCGameCatalog).GetField("instance",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).SetValue(null,null); }
  static void TestCatalogFromJson(){
    UseJsonCatalog();
    var games = ECCGameCatalog.Instance.EligibleGames;
    Check(games.Count==12 && games.All(g=>g.verified && !string.IsNullOrEmpty(g.title) && ECCGameIcons.IsAllowedUrl(g.iconUrl)), "12 verified games from the developer's file");
    Check(games.Any(g=>g.packageName=="com.mhmpourgame.connectballs" && g.title=="وصلینو") && games.Any(g=>g.packageName=="com.vahab.tapIt"), "packages kept exactly ( incl. upper case )");
    Check(!games.Any(g=>g.packageName=="com.car.chase.vp"), "this game excluded");
    var bad = ECCGameCatalog.ParseJson("{\"verifiedOn\":\"2026-10-27\",\"games\":[{\"packageName\":\"com.a.b\",\"title\":\"x\",\"description\":\"\",\"iconUrl\":\"\",\"bazaarDeepLink\":\"\",\"bazaarUrl\":\"https://cafebazaar.ir/app/com.other\"}]}");
    Check(bad.Count==1 && !bad[0].verified, "listing link not matching the package: not verified");
    var noDate = ECCGameCatalog.ParseJson("{\"verifiedOn\":\"\",\"games\":[{\"packageName\":\"com.a.b\",\"title\":\"x\",\"description\":\"\",\"iconUrl\":\"\",\"bazaarDeepLink\":\"\",\"bazaarUrl\":\"\"}]}");
    Check(noDate.Count==1 && !noDate[0].verified, "file without a verification date: not verified");
    Check(ECCGameCatalog.ParseJson("not json").Count==0, "broken file: empty list, no crash");
  }
  static void TestOtherGamesScreenFromJson(){
    UseJsonCatalog(); ECCOtherGamesUI.Open();
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasOtherGames");
    var cards = FindAll<Button>(root).Where(b=>b.gameObject.name.StartsWith("Game_")).ToList();
    Check(cards.Count==12 && FindAll<Button>(root,"ButtonDeveloper").Count==0, "12 cards");
    Check(Texts(root).Contains(ECCPersianText.Fix("سودوکو")) && Texts(root).Contains(ECCPersianText.Fix("آجرشکن")), "titles shown");
    Check(FindAll<Text>(root,"Placeholder").Count==12, "offline: placeholders until the icons download");
    Texture2D tex = new Texture2D(4,4); Resources.loads["ECCOtherGames/Icons/com.mhmpourgame.sudoku"] = tex;
    Check(ECCGameIcons.GetCached(ECCGameCatalog.Instance.Find("com.mhmpourgame.sudoku"))!=null, "imported icon used without download");
  }
  static void TestRecommendationFromJson(){
    UseJsonCatalog(); NextDay(); ECCDailyChallenge.MarkPopupShown();
    var g = ECCGameRecommendation.TodayGame;
    Check(g!=null && ECCGameCatalog.Instance.Find(g.packageName)!=null, "recommendation picks a game from the file");
    int nav=0; ECCPostVictoryFlow.Leave(true, ()=>nav++);
    Check(ECCGameRecommendationUI.IsOpen && nav==0, "recommendation popup shown after a normal win");
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasGameRecommendation");
    Application.lastUrl=null; FindAll<Button>(root,"ButtonInstall")[0].onClick.Invoke();
    Check(Application.lastUrl=="https://cafebazaar.ir/app/"+g.packageName && nav==1, "install opens the game's Bazaar page ( web fallback outside Android ) and continues");
  }
}
