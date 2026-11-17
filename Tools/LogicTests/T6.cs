using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static void TestPowerUpInventory(){
    PlayerPrefs.SetInt("Money", 100 + ECCPowerUpInventory.GetPrice(ECCPowerUpType.Nitro));
    foreach (var t in ECCPowerUpInventory.Types) if (ECCPowerUpInventory.GetFreeUses(t)!=1) { Check(false,"one free use each"); return; }
    Check(ECCPowerUpInventory.Types.Length>=3, "at least three power-up types with one free use");
    Restart(); Check(ECCPowerUpInventory.GetFreeUses(ECCPowerUpType.Nitro)==1, "free uses persist, grant not repeated");
    Check(!ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>false) && ECCPowerUpInventory.GetFreeUses(ECCPowerUpType.Nitro)==1, "failed effect keeps the free use");
    Check(ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>true) && ECCPowerUpInventory.GetFreeUses(ECCPowerUpType.Nitro)==0 && ECCWallet.Balance==100+ECCPowerUpInventory.GetPrice(ECCPowerUpType.Nitro), "free use consumed first, no coins");
    Restart(); Check(ECCPowerUpInventory.GetFreeUses(ECCPowerUpType.Nitro)==0, "free use does not regenerate on restart");
    int price = ECCPowerUpInventory.GetPrice(ECCPowerUpType.Nitro);
    Check(!ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>false) && ECCWallet.Balance==100+price, "failed effect costs no coins");
    Check(ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>true) && ECCWallet.Balance==100, "coins charged after free uses");
    PlayerPrefs.SetInt("Money", price-1);
    bool applied=false;
    Check(ECCPowerUpInventory.GetPayMethod(ECCPowerUpType.Nitro)==ECCPowerUpInventory.PayMethod.Video && !ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>applied=true) && !applied && ECCWallet.Balance==price-1, "insufficient coins: video needed, nothing applied or charged");
  }
  static void TestShieldPass(){
    ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Shield, ()=>true);
    PlayerPrefs.SetInt("Money", 0);
    Check(ECCPowerUpInventory.GetPayMethod(ECCPowerUpType.Shield)==ECCPowerUpInventory.PayMethod.Video, "no pass: shield needs a video");
    ECCPowerUpInventory.GrantShieldPass(24);
    Check(ECCPowerUpInventory.IsShieldPassActive && ECCPowerUpInventory.ShieldPassSecondsLeft>86000 && ECCPowerUpInventory.ShieldPassSecondsLeft<=86400, "pass lasts 24h");
    Check(ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Shield, ()=>true), "pass gives a free shield");
    Check(ECCPowerUpInventory.GetPayMethod(ECCPowerUpType.Shield)==ECCPowerUpInventory.PayMethod.Video, "one free shield per chase");
    ECCPowerUpInventory.ResetRun(); Check(ECCPowerUpInventory.HasPassUse(ECCPowerUpType.Shield), "next chase: free shield again");
    Check(!ECCPowerUpInventory.HasPassUse(ECCPowerUpType.Nitro), "pass is only for the shield");
    ECCSave.Data.freeShieldPassUntil = ECCDaily.UnixNow - 1; Check(!ECCPowerUpInventory.IsShieldPassActive && !ECCPowerUpInventory.HasPassUse(ECCPowerUpType.Shield), "pass expires");
  }
  static ECCPowerUpBar MakeChaseWithBar(out ECCGameController gc, out ECCPowerUpManager manager){
    var go = new GameObject("GameController"); gc = go.AddComponent<ECCGameController>();
    gc.gameStarted = true; gc.startDelay = 0;
    gc.playerObject = new GameObject("Player").AddComponent<ECCCar>(); gc.playerObject.health = 10; gc.playerObject.healthMax = 10; typeof(ECCCar).GetField("gameController",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null, gc);
    manager = go.AddComponent<ECCPowerUpManager>();
    typeof(ECCPowerUpManager).GetField("gameController",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager, gc);
    var bar = ECCPowerUpBar.Create(gc);
    typeof(ECCPowerUpBar).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(bar,null);
    return bar;
  }
  static void TestPowerUpBarVideoPath(){
    PlayerPrefs.SetInt("Money", 0);
    ECCPowerUpInventory.TryUseFreeOrCoins(ECCPowerUpType.Nitro, ()=>true); // use the free nitro
    ECCGameController gc; ECCPowerUpManager manager; var bar = MakeChaseWithBar(out gc, out manager);
    var barRoot = UnityEngine.Object.FindAllRoots().Last(r=>FindAll<RectTransform>(r,"PowerUpNitro").Count>0);
    var nitroRoot = FindAll<RectTransform>(barRoot,"PowerUpNitro")[0]; var nitro = nitroRoot.GetComponentInChildren<Button>();
    Check(nitro!=null && Texts(nitroRoot).Contains(ECCPersianText.Fix("ویدئو")), "button shows the video option");
    int used=0; Action<ECCPowerUpType> h = t=>used++; ECCPowerUpManager.PowerUpUsed += h;
    nitro.onClick.Invoke(); Check(ECCPowerUpPopup.IsOpen && gc.isPaused, "no coins: the video popup opens, chase paused");
    FindButton("ButtonWatch").onClick.Invoke(); Coroutine.Run(8);
    Check(used==0 && !manager.IsActive(ECCPowerUpType.Nitro), "no ad provider: NoAds, power-up not granted");
    var fake = FakeAds.Install(ECCAdResult.Skipped);
    FindButton("ButtonWatch").onClick.Invoke(); Coroutine.Run(3);
    Check(used==0 && !manager.IsActive(ECCPowerUpType.Nitro), "skipped video: not granted");
    fake.result = ECCAdResult.Rewarded; fake.doubleCallback = true;
    var watch = FindButton("ButtonWatch"); watch.onClick.Invoke(); watch.onClick.Invoke(); Coroutine.Run(3);
    Check(used==1 && manager.IsActive(ECCPowerUpType.Nitro) && ECCWallet.Balance==0 && !gc.isPaused && !ECCPowerUpPopup.IsOpen, "verified video: granted once, no coins spent, chase resumed");
    ECCPowerUpManager.PowerUpUsed -= h;
    Coroutine.Run(1);
    var emp = FindAll<RectTransform>(nitroRoot.parent,"PowerUpMagnet")[0].GetComponentInChildren<Button>(); emp.onClick.Invoke();
    Check(ECCPowerUpInventory.GetFreeUses(ECCPowerUpType.Magnet)==0 && manager.IsActive(ECCPowerUpType.Magnet), "free magnet used from the bar");
  }
}
