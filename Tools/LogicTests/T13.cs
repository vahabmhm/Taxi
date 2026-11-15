using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Levels;
// Tests for the 2026-11-03 features: achievements, star chests, car customization, car trial, tutorial, haptics,
// level catalog ( new goals, night, helicopter, cash targets )
public static partial class T {
  static void TestAchievementsCountAndClaimOnce(){
    PlayerPrefs.SetInt("Money",0);
    var cops100 = ECCAchievements.All.First(a=>a.id=="passengers_50");
    ECCMissions.Report(ECCMissionType.DeliverPassengers, 30);
    ECCMissions.Report(ECCMissionType.DeliverPassengers, 19);
    Check(ECCAchievements.GetValue(ECCStat.DeliverPassengers)==49 && !ECCAchievements.CanClaim(cops100), "achievement counts through the mission reports, not done at 49");
    Restart();
    ECCMissions.Report(ECCMissionType.DeliverPassengers, 1);
    Check(ECCAchievements.GetValue(ECCStat.DeliverPassengers)==50 && ECCAchievements.CanClaim(cops100) && ECCAchievements.ClaimableCount>=1, "progress kept after restart, ready at 50");
    Check(ECCAchievements.Claim(cops100)==cops100.reward && ECCWallet.Balance==cops100.reward, "reward paid");
    Check(ECCAchievements.Claim(cops100)==0 && ECCWallet.Balance==cops100.reward, "second claim refused");
    Restart();
    Check(ECCAchievements.IsClaimed(cops100) && ECCAchievements.Claim(cops100)==0 && ECCWallet.Balance==cops100.reward, "claimed state survives restart");
    ECCAchievements.Report(ECCStat.DeliverInOneShift, 12); ECCAchievements.Report(ECCStat.DeliverInOneShift, 6);
    Check(ECCAchievements.GetValue(ECCStat.DeliverInOneShift)==12, "best-value achievements keep the best");
    ECCSave.Data.levels.Add(new ECCLevelRecord{id="x1",stars=3}); ECCSave.Data.levels.Add(new ECCLevelRecord{id="x2",stars=2});
    Check(ECCAchievements.GetValue(ECCStat.TotalStars)==5, "total stars read from the level progress");
    Check(ECCAchievements.All.Select(a=>a.id).Distinct().Count()==ECCAchievements.All.Length && ECCAchievements.All.Any(a=>a.stat==ECCStat.DeliverPassengers && a.target==1000), "unique ids, includes 1000 passengers");
  }
  static void TestAchievementsScreen(){
    ECCMissions.Report(ECCMissionType.PerfectStops, 50);
    ECCAchievementsUI.Open();
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasAchievements");
    string texts = Texts(root);
    Check(texts.Contains(ECCPersianText.Fix("دستاوردها")) && texts.Contains(ECCPersianText.Fix("دریافت جایزه")) && texts.Contains(ECCPersianText.Fix("در حال انجام")), "achievements screen: title and states");
    int before = ECCWallet.Balance;
    var claim = FindAll<Button>(root,"Claim").First(b=>b.interactable); claim.onClick.Invoke(); claim.onClick.Invoke();
    Check(ECCWallet.Balance==before+ECCAchievements.All.First(a=>a.id=="perfect_50").reward, "claimed once from the screen");
  }
  static void TestStarChests(){
    PlayerPrefs.SetInt("Money",0);
    var catalog = ECCLevelCatalog.Instance; var levels = catalog.chapters[0].levels;
    Check(!ECCStarChests.CanOpen(0,0) && ECCStarChests.Open(0,0)==0 && ECCWallet.Balance==0, "chest locked without stars");
    for (int i=0;i<4;i++) ECCSave.SetStars(levels[i].id, 3);
    Check(ECCStarChests.CanOpen(0,0) && !ECCStarChests.CanOpen(0,1) && ECCStarChests.ReadyCount==1, "10 stars opens the first chest only");
    int paid = ECCStarChests.Open(0,0);
    Check(paid==ECCStarChests.Reward(0,0) && ECCWallet.Balance==paid, "chest pays once");
    Restart();
    Check(ECCStarChests.IsOpened(0,0) && ECCStarChests.Open(0,0)==0 && ECCWallet.Balance==paid, "opened chest stays opened after restart");
    Check(ECCStarChests.Reward(5,2) > ECCStarChests.Reward(0,2) && ECCStarChests.Needed(0,2)==30, "later chapters pay more; last chest needs all stars");
  }
  static void TestCarStyleBuyAndEquip(){
    PlayerPrefs.SetInt("Money",250);
    var red = ECCCarStyle.Find(ECCStyleCategory.Paint,"red");
    Check(!ECCCarStyle.IsOwned(red) && !ECCCarStyle.Buy(red) && ECCWallet.Balance==250, "not enough coins: nothing bought");
    Check(!ECCCarStyle.Equip("Sedan", red), "can't equip what isn't owned");
    PlayerPrefs.SetInt("Money",1000);
    Check(ECCCarStyle.Buy(red) && ECCWallet.Balance==1000-red.price && ECCCarStyle.IsOwned(red), "bought once");
    Check(ECCCarStyle.Buy(red) && ECCWallet.Balance==1000-red.price, "buying again costs nothing");
    Check(ECCCarStyle.Equip("Sedan(Clone)", red) && ECCCarStyle.GetChoice("Sedan", ECCStyleCategory.Paint)=="red" && ECCCarStyle.GetChoice("Van", ECCStyleCategory.Paint)=="", "each car keeps its own choice");
    Restart();
    Check(ECCCarStyle.IsOwned(red) && ECCCarStyle.GetChoice("Sedan", ECCStyleCategory.Paint)=="red", "ownership and choice survive restart");
    Check(ECCCarStyle.Equip("Sedan", ECCCarStyle.Find(ECCStyleCategory.Paint,"")) && ECCCarStyle.GetChoice("Sedan", ECCStyleCategory.Paint)=="", "factory paint is free");
    foreach (ECCStyleCategory c in Enum.GetValues(typeof(ECCStyleCategory))) Check(ECCCarStyle.InCategory(c).Count>=6 && ECCCarStyle.InCategory(c)[0].price==0, "category "+c+": free default and options");
    // Applying the look on a bare object doesn't fail
    var car = new GameObject("Sedan"); ECCCarStyle.ApplyTo(car.transform, "Sedan");
    Check(car.GetComponent<ECCCarStyleState>()!=null, "look applied to a car object");
  }
  static void TestCarTrialBuy(){
    PlayerPrefs.SetInt("Money",5000);
    // Built without OnEnable ( the stub has no scene for the shop's extra cars )
    var shop = (ECCShop)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(ECCShop)); shop.currentPlayerprefs = "CurrentCar";
    var icon = new GameObject("Kenney_Taxi"); icon.transform.name = "Kenney_Taxi"; // the stub keeps separate names
    shop.items = new[]{ new EndlessCarChase.Types.ShopItem{ itemIcon=icon.transform, lockState=0, price=2500 } };
    Check(ECCCarTrial.Buy(shop,0) && ECCWallet.Balance==2500 && PlayerPrefs.GetInt("Kenney_Taxi(Clone)",0)==1 && shop.items[0].lockState==1, "trial offer buys the car with the shop's lock key");
    PlayerPrefs.SetInt("Money",100); shop.items[0].lockState=0; PlayerPrefs.SetInt("Kenney_Taxi(Clone)",0);
    Check(!ECCCarTrial.Buy(shop,0) && ECCWallet.Balance==100 && shop.items[0].lockState==0, "not enough coins: car stays locked");
    Check(!ECCCarTrial.IsTrialRun, "no trial without a watched video");
  }
  static void TestTutorialOnlyForNewPlayers(){
    // A fresh install
    Check(ECCTutorial.IsNeeded && ECCSave.Data.tutorialStep==0, "new player gets the tutorial");
    ECCSave.Data.tutorialStep = ECCTutorial.StepCount; ECCSave.Save(); Restart();
    Check(!ECCTutorial.IsNeeded, "finished tutorial stays finished");
    // A player from before this update
    Wipe(); PlayerPrefs.SetInt("Money",300); Restart();
    Check(!ECCTutorial.IsNeeded, "existing players skip the tutorial");
  }
  static void TestHapticsSetting(){
    Check(ECCHaptics.Enabled, "vibration on by default");
    ECCHaptics.Enabled = false; Restart();
    Check(!ECCHaptics.Enabled, "vibration off is saved");
    ECCHaptics.Vibrate(ECCHaptics.Kind.Heavy);
    Check(ECCHaptics.Duration(ECCHaptics.Kind.Heavy) > ECCHaptics.Duration(ECCHaptics.Kind.Light), "crash buzz is longer than a light tap");
  }
  static void TestCatalogNewGoals(){
    var catalog = ECCLevelCatalog.Instance;
    var all = catalog.chapters.SelectMany(c=>c.levels).ToList();
    // Taxi game: every level is a taxi goal in the city ( by day, at night or in the rain )
    Check(all.All(l=>ECCObjectives.IsTaxi(l.objective)), "every level is a taxi level");
    Check(all.All(l=>l.theme==ECCLevelTheme.City || l.theme==ECCLevelTheme.Rain), "every level is in the city");
    foreach (var kind in new[]{ECCObjectiveType.Passengers,ECCObjectiveType.RaceRivals,ECCObjectiveType.Vip,ECCObjectiveType.Earn,ECCObjectiveType.PerfectStops,ECCObjectiveType.CleanRecord,ECCObjectiveType.SmoothRide})
      Check(catalog.chapters[0].levels.Any(l=>l.objective==kind), "chapter 1 has a " + kind + " level");
    Check(catalog.chapters[0].levels[0].rivals==0 && catalog.chapters[0].levels[0].timeLimit==0, "the first level has no rivals and no clock");
    Check(all.Where(l=>l.objective==ECCObjectiveType.RaceRivals || l.objective==ECCObjectiveType.Vip).All(l=>l.rivals>=1), "race and VIP levels have rivals");
    Check(all.Any(l=>l.night), "night levels exist");
    Check(!all.Any(l=>l.helicopter || l.spikeStrips || l.roadblocks), "taxi levels have no chase traps ( the police bring them )");
    var c1 = catalog.chapters[0].levels.First(l=>l.objective==ECCObjectiveType.Passengers && l.timeLimit>0);
    var c30 = catalog.chapters[catalog.chapters.Length-1].levels.First(l=>l.objective==ECCObjectiveType.Passengers && l.timeLimit>0);
    Check(c30.target > c1.target && c30.timeLimit/c30.target < c1.timeLimit/c1.target, "later chapters need more passengers in less time each");
    Check(all.All(l=>l.target>0 && !string.IsNullOrEmpty(l.title)), "every level has a goal and a title");
    Check(all.Select(l=>l.ObjectiveDescription).All(d=>!string.IsNullOrEmpty(d)), "every goal has a Persian description");
    Check(ECCObjectives.UsesDestination(ECCObjectiveType.Heist) && !ECCObjectives.UsesDestination(ECCObjectiveType.CollectCash), "destination goals");
  }
  static void TestRecordDistanceSaved(){
    Check(ECCSave.Data.endlessBestDistance==0, "no record at first");
    ECCSave.Data.endlessBestDistance = 1234; ECCSave.Save(); Restart();
    Check(ECCSave.Data.endlessBestDistance==1234, "endless record distance saved");
  }
  static void TestCarStyleSheet(){
    PlayerPrefs.SetInt("Money",1000);
    ECCCarStyleUI.Open("Sedan", null);
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasCarStyle");
    Check(ECCCarStyleUI.IsOpen && ECCModalTracker.AnyOpen, "customization sheet open");
    Check(root.kids[0].gameObject.name=="Sheet", "sheet background behind the content ( first child )");
    Check(FindAll<Button>(root).Count(b=>b.gameObject.name.StartsWith("Tab"))==4, "four tabs");
    var red = FindAll<Button>(root).First(b=>b.gameObject.name=="Option_red");
    red.onClick.Invoke();
    Check(ECCWallet.Balance==1000 && FindAll<Button>(root,"ButtonBuy").Count==1, "unbought option previews first, buy button shown");
    FindAll<Button>(root,"ButtonBuy")[0].onClick.Invoke();
    Check(ECCWallet.Balance==1000-ECCCarStyle.Find(ECCStyleCategory.Paint,"red").price && ECCCarStyle.GetChoice("Sedan",ECCStyleCategory.Paint)=="red", "bought and put on the car from the sheet");
  }
}
