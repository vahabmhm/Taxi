using System; using System.Linq; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static void TestGameCatalog(){
    var cat = ECCGameCatalog.Instance;
    Check(cat.games.Count==0 && cat.EligibleGames.Count==0, "default catalog has no invented games");
    Check(cat.developerPageUrl=="https://cafebazaar.ir/developer/774612137738", "developer page from the scene");
    var test = ScriptableObject.CreateInstance<ECCGameCatalog>();
    test.games.Add(new ECCGameEntry{packageName="com.example.one", title="یک", verified=true});
    test.games.Add(new ECCGameEntry{packageName="com.example.one", title="یک تکراری", verified=true});
    test.games.Add(new ECCGameEntry{packageName="com.car.chase.vp", title="همین بازی", verified=true});
    test.games.Add(new ECCGameEntry{packageName="com.example.two", title="دو", verified=false});
    test.games.Add(new ECCGameEntry{packageName="not a package", title="بد", verified=true});
    test.games.Add(new ECCGameEntry{packageName="ir.example.three", title="سه", verified=true});
    var e = test.EligibleGames.Select(g=>g.packageName).ToList();
    Check(e.SequenceEqual(new[]{"com.example.one","ir.example.three"}), "eligible: verified, valid, unique, not this game ("+string.Join(",",e)+")");
    Check(ECCStoreLinks.BazaarListingUrl("com.example.one")=="https://cafebazaar.ir/app/com.example.one", "https listing url");
  }
  static void TestOtherGamesScreen(){
    ECCOtherGamesUI.Open();
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasOtherGames");
    Check(Texts(root).Contains(ECCPersianText.Fix("سایر بازی‌ها")) && FindAll<Button>(root,"ButtonDeveloper").Count==1, "empty catalog: title and developer page button");
    Wipe();
    var cat = ECCGameCatalog.Instance;
    for (int i=0;i<5;i++) cat.games.Add(new ECCGameEntry{packageName="com.example.g"+i, title="بازی "+i, description="توضیح کوتاه", verified=true});
    ECCOtherGamesUI.Open();
    root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasOtherGames");
    var grid = FindAll<GridLayoutGroup>(root)[0];
    Check(grid.constraintCount==2 && grid.startCorner==GridLayoutGroup.Corner.UpperRight, "two columns, right to left");
    Check(FindAll<Button>(root).Count(b=>b.gameObject.name.StartsWith("Game_"))==5 && FindAll<Scrollbar>(root).Count==1, "a card per game and a scrollbar");
    cat.games.Clear();
  }
}
