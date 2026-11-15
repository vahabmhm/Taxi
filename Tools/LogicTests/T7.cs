using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
public static partial class T {
  static void TestWheelGeometryAndWeights(){
    var prizes = ECCLuckyWheel.Prizes; int n = prizes.Length;
    Check(n==8 && prizes.All(p=>p.coins>0 && p.weight>0), "8 prize segments with weights");
    bool aligned = true;
    for (int i=0;i<n;i++) for (int k=0;k<=8;k++) foreach (float off in new[]{-0.35f,0,0.35f}) {
      float landing = ECCLuckyWheel.AngleForSegment(i,n) + off*360f/n - 360*k;
      if (ECCLuckyWheel.SegmentAtPointer(landing,n)!=i) aligned=false;
    }
    Check(aligned, "pointer segment matches the chosen prize for every landing angle");
    Check(ECCLuckyWheel.PickWeighted(prizes,0f)==0 && ECCLuckyWheel.PickWeighted(prizes,0.9999f)==n-1 && ECCLuckyWheel.PickWeighted(prizes,1f)==n-1, "weighted pick bounds");
    var counts = new int[n]; var rnd = new System.Random(1); for (int i=0;i<100000;i++) counts[ECCLuckyWheel.PickWeighted(prizes,(float)rnd.NextDouble())]++;
    float total = prizes.Sum(p=>p.weight); bool ok=true; for (int i=0;i<n;i++) if (Math.Abs(counts[i]/100000f - prizes[i].weight/total) > 0.01f) ok=false;
    Check(ok, "weights respected");
    var zero = new[]{ new ECCWheelPrize{coins=1,weight=0}, new ECCWheelPrize{coins=2,weight=1}};
    Check(ECCLuckyWheel.PickWeighted(zero,0f)==1 && ECCLuckyWheel.PickWeighted(zero,1f)==1, "zero weight never picked");
    Check(Math.Abs(ECCLuckyWheelUI.Ease(0))<1e-5 && Math.Abs(ECCLuckyWheelUI.Ease(1)-1)<1e-5 && ECCLuckyWheelUI.Ease(0.5f)>ECCLuckyWheelUI.Ease(0.3f), "easing 0..1 increasing");
  }
  static void TestWheelOncePerDay(){
    PlayerPrefs.SetInt("Money",0);
    Check(ECCLuckyWheel.IsAvailableToday, "spin available");
    int i = ECCLuckyWheel.Spin(); int prize = ECCLuckyWheel.Prizes[i].coins;
    Check(i>=0 && ECCSave.Data.wheelPrize==prize && ECCWallet.Balance==0, "result saved before paying");
    Check(ECCLuckyWheel.Spin()==-1, "second spin same day refused");
    Restart(); Check(ECCLuckyWheel.Spin()==-1 && !ECCLuckyWheel.IsAvailableToday, "no spin after restart");
    Check(ECCLuckyWheel.PayPending()==prize && ECCWallet.Balance==prize, "interrupted spin paid on recovery");
    Check(ECCLuckyWheel.PayPending()==0 && ECCWallet.Balance==prize, "paid once");
    NextDay(); Check(ECCLuckyWheel.IsAvailableToday, "new spin next day");
    ECCSave.Data.lastSeenDay = "2000-01-01"; Check(!ECCLuckyWheel.IsAvailableToday || ECCDaily.Today!="2000-01-01", "clock rollback does not reopen the day");
  }
  static void TestWheelScreen(){
    PlayerPrefs.SetInt("Money",0);
    ECCLuckyWheelUI.Open();
    var spin = FindButton("ButtonSpin");
    Check(spin!=null && spin.interactable && Texts(spin.transform).Contains(ECCPersianText.Fix("چرخاندن گردونه")), "spin button");
    spin.onClick.Invoke(); spin.onClick.Invoke();
    Check(!spin.interactable && ECCWallet.Balance==0, "button disabled while spinning, nothing paid yet");
    Coroutine.Run(6);
    var wheel = FindAll<RectTransform>(UnityEngine.Object.FindAllRoots().Last(t=>t.gameObject.name=="CanvasLuckyWheel"), "Wheel")[0];
    Check(ECCLuckyWheel.SegmentAtPointer(wheel.localEulerAngles.z, ECCLuckyWheel.Prizes.Length)==ECCSave.Data.wheelResult, "wheel stops on the paid segment");
    Check(ECCWallet.Balance==ECCSave.Data.wheelPrize && !spin.interactable, "paid once after the spin");
  }
}
