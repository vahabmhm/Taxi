using System; using UnityEngine; using EndlessCarChase;
public static partial class T {
  static void TestWallet(){
    PlayerPrefs.SetInt("Money", 500); Restart();
    Check(ECCWallet.Balance==500, "legacy balance kept");
    int events=0; int last=-1; Action<int> h = v=>{events++; last=v;}; ECCWallet.BalanceChanged += h;
    Check(ECCWallet.TryReward("r1",100) && ECCWallet.Balance==600, "reward paid");
    Check(!ECCWallet.TryReward("r1",100) && ECCWallet.Balance==600, "duplicate reward ignored");
    Restart();
    Check(!ECCWallet.TryReward("r1",100) && ECCWallet.Balance==600, "duplicate reward ignored after restart");
    Check(!ECCWallet.TrySpend(601) && ECCWallet.Balance==600, "insufficient balance refused");
    Check(ECCWallet.TrySpend(600) && ECCWallet.Balance==0, "spend all");
    Check(!ECCWallet.TrySpend(-5) && ECCWallet.Balance==0, "negative spend refused");
    ECCSave.Money = -50; Check(ECCWallet.Balance==0, "non negative");
    Check(events==2 && last==0, "balance events raised "+events);
    ECCWallet.BalanceChanged -= h;
    PlayerPrefs.SetInt("Money", int.MaxValue-5); Check(ECCWallet.TryReward("big",100) && ECCWallet.Balance==int.MaxValue, "overflow clamped");
    Check(ECCWallet.Format(1234567)=="1,234,567", "thousands separator");
  }
  static void TestDaily(){
    string today = ECCDaily.Today;
    Check(ECCSave.Data.installDay==today && ECCDaily.IsInstallDay, "fresh install day");
    ECCSave.Data.lastSeenDay = "2999-01-01"; Check(ECCDaily.Today=="2999-01-01", "clock rollback keeps latest day");
    Check(ECCDaily.DaysBetween("2026-02-27","2026-03-01")==2, "days between");
    Wipe(); PlayerPrefs.SetInt("Money", 10); Restart();
    Check(ECCDaily.IsInstallDay==false, "update of existing install is not the install day");
  }
}
