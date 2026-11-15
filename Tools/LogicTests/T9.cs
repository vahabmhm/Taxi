using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static void TestLoginStreak(){
    PlayerPrefs.SetInt("Money",0);
    var cfg = ECCEconomyConfig.Instance;
    Check(ECCLoginRewards.CurrentDay==1 && ECCLoginRewards.CanClaimToday, "day 1 claimable");
    Check(ECCLoginRewards.Claim()==cfg.GetLoginReward(1) && ECCWallet.Balance==cfg.GetLoginReward(1), "day 1 paid");
    Check(ECCLoginRewards.Claim()==-1 && ECCWallet.Balance==cfg.GetLoginReward(1), "double claim refused");
    ECCLoginRewards.RegisterLogin(); ECCLoginRewards.RegisterLogin(); Restart();
    Check(ECCLoginRewards.CurrentDay==1 && !ECCLoginRewards.CanClaimToday && ECCLoginRewards.IsDayClaimed(1), "same day relaunch: no advance, claimed state kept");
    int total = cfg.GetLoginReward(1); bool increasing = true; int last = cfg.GetLoginReward(1);
    for (int d=2; d<=9; d++){ NextDay(); if (ECCLoginRewards.CurrentDay!=d) { Check(false,"advance to day "+d); return; } int p = ECCLoginRewards.Claim(); total+=p; if (p<=last) increasing=false; last=p; }
    Check(increasing && ECCWallet.Balance==total, "days 2-9 increase and are paid");
    NextDay(); Check(ECCLoginRewards.CurrentDay==10 && ECCLoginRewards.IsSpecialDay(10), "day 10");
    Check(ECCLoginRewards.Claim()==0 && ECCPowerUpInventory.IsShieldPassActive && ECCWallet.Balance==total, "day 10 starts the shield pass");
    long until = ECCSave.Data.freeShieldPassUntil; ECCSave.Data.loginClaimDay=""; ECCLoginRewards.Claim();
    Check(ECCSave.Data.freeShieldPassUntil==until, "day 10 reward not granted twice");
    NextDay(); Check(ECCLoginRewards.CurrentDay==1 && ECCSave.Data.loginClaimedMask==0, "new cycle after day 10");
    ECCLoginRewards.Claim(); NextDay(); NextDay();
    int before = ECCWallet.Balance;
    Check(ECCLoginRewards.CurrentDay==1 && ECCWallet.Balance==before && ECCPowerUpInventory.IsShieldPassActive, "missed day resets to day 1, coins and pass kept");
    ECCSave.Data.freeShieldPassUntil = ECCDaily.UnixNow - 5; Check(!ECCPowerUpInventory.IsShieldPassActive, "pass expires");
  }
  static void TestLoginCalendarScreen(){
    ECCLoginRewardsUI.Open();
    var root = UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasLoginRewards");
    string texts = Texts(root);
    Check(FindAll<Image>(root).Count(i=>i.gameObject.name.StartsWith("Day"))==10, "10 calendar days");
    Check(texts.Contains(ECCPersianText.Fix("آماده دریافت")) && texts.Contains(ECCPersianText.Fix("آینده")), "states shown");
    var claim = FindAll<Button>(root,"ButtonClaim")[0]; claim.onClick.Invoke(); claim.onClick.Invoke();
    Check(ECCWallet.Balance==ECCEconomyConfig.Instance.GetLoginReward(1) && Texts(root).Contains(ECCPersianText.Fix("دریافت شده")), "claimed once from the calendar");
  }
  static ECCMenuHub MakeMenu(){
    var gc = new GameObject("GameController").AddComponent<ECCGameController>();
    var hub = ECCMenuHub.Create(gc);
    var menu = new GameObject("CanvasMenu"); typeof(ECCMenuHub).GetField("menuCanvas",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub, menu);
    return hub;
  }
  static void ShowMenu(ECCMenuHub hub){ typeof(ECCMenuHub).GetMethod("OnMainMenuShown",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(hub,null); }
  static void TestDailyPopupPriority(){
    // Install day: no challenge popup, the calendar opens
    var hub = MakeMenu(); ShowMenu(hub); Coroutine.Run(2);
    Check(!ECCChallengePopup.IsOpen && ECCLoginRewardsUI.IsOpen, "install day: calendar, no challenge popup");
    Wipe(); NextDayFresh();
    hub = MakeMenu(); ShowMenu(hub); Coroutine.Run(2);
    Check(ECCChallengePopup.IsOpen && !ECCLoginRewardsUI.IsOpen, "challenge popup first, calendar waits");
    Coroutine.Run(5); Check(!ECCLoginRewardsUI.IsOpen, "never both at once");
    FindButton("ButtonDecline").onClick.Invoke(); Coroutine.Run(2);
    Check(!ECCChallengePopup.IsOpen && ECCLoginRewardsUI.IsOpen, "calendar after the challenge popup is dismissed");
    FindAll<Button>(UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasLoginRewards"),"ButtonClose")[0].onClick.Invoke();
    ShowMenu(hub); Coroutine.Run(3);
    Check(!ECCChallengePopup.IsOpen && !ECCLoginRewardsUI.IsOpen && ECCLoginRewards.CanClaimToday, "dismissed calendar not forced again; reward still claimable");
    ECCLoginRewardsUI.Open(); Check(ECCLoginRewardsUI.IsOpen, "calendar reachable from the menu button");
  }
  static void NextDayFresh(){ ECCDaily.Today.ToString(); ECCSave.Data.installDay="2000-01-01"; }
}
