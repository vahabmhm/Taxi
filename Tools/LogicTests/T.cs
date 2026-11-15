using System; using System.Reflection; using UnityEngine; using EndlessCarChase;
public static partial class T {
  static int fails=0, passes=0;
  public static void Check(bool c, string name){ if(c){passes++;Console.WriteLine("PASS "+name);} else {fails++;Console.WriteLine("FAIL "+name);} }
  // Simulates an app restart: static caches dropped, PlayerPrefs kept
  public static void Restart(){ typeof(ECCSave).GetField("data",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null); }
  public static void Wipe(){ PlayerPrefs.DeleteAll(); Restart();
    // Close every popup left by an earlier test
    var list = (System.Collections.IList)typeof(ECCModalTracker).GetField("open",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
    foreach (Component tracker in list) UnityEngine.Object.Destroy(tracker.gameObject);
    list.Clear();
    Coroutine.all.Clear(); Time.timeScale = 1; typeof(ECCGameCatalog).GetField("instance",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null); Resources.loads.Clear(); ECCCar.ActiveCars.Clear();
    ((System.Collections.IList)typeof(ECCAds).GetField("providers",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)).Clear();
    typeof(ECCAds).GetProperty("IsBusy").SetValue(null,false);
    EndlessCarChase.ECCDailyChallenge.PlayRequested = false;
    typeof(ECCDailyChallenge).GetField("cachedLevel",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
    foreach (var type in typeof(ECCWallet).Assembly.GetTypes()) { var f = type.GetField("open",BindingFlags.NonPublic|BindingFlags.Static); if (f!=null && !f.FieldType.IsValueType && f.FieldType!=typeof(System.Collections.IList) && !typeof(System.Collections.IList).IsAssignableFrom(f.FieldType)) f.SetValue(null,null); }
  }
  public static int Main(string[] a){
    foreach (var m in typeof(T).GetMethods(BindingFlags.Static|BindingFlags.NonPublic)) if (m.Name.StartsWith("Test")) { Wipe(); try { m.Invoke(null,null);} catch(Exception e){ fails++; Console.WriteLine("FAIL "+m.Name+" threw "+(e.InnerException??e)); } }
    Console.WriteLine("passed "+passes+" failed "+fails); return fails;
  }
}
public static partial class T {
  // Finds components in a built UI hierarchy ( stub transforms )
  public static System.Collections.Generic.List<C> FindAll<C>(Transform root, string name=null) where C:Component {
    var list = new System.Collections.Generic.List<C>();
    if (root==null) return list;
    var c = root.gameObject.GetComponent<C>(); if (c!=null && (name==null || root.gameObject.name==name) && root.gameObject.activeSelf) list.Add(c);
    foreach (Transform child in root.kids.ToArray()) list.AddRange(FindAll<C>(child, name));
    return list;
  }
  public static string Texts(Transform root){ var sb=new System.Text.StringBuilder(); foreach(var t in FindAll<UnityEngine.UI.Text>(root)) sb.Append(t.text).Append('|'); return sb.ToString(); }
}

public class FakeAds : IECCAdProvider, IECCInterstitialProvider {
  public bool ready = true; public bool failed; public bool interstitialReady; public int interstitials; public ECCAdResult result = ECCAdResult.Rewarded; public int shows; public bool doubleCallback;
  public bool HasRewardedFailed { get { return failed; } }
  public bool IsInterstitialReady { get { return interstitialReady; } }
  public void ShowInterstitial(System.Action<bool> done) { interstitials++; done(true); if (doubleCallback) done(true); }
  public string Name { get { return "Fake"; } }
  public bool IsRewardedReady { get { return ready; } }
  public void RequestRewarded() {}
  public void ShowRewarded(System.Action<ECCAdResult> done) { shows++; done(result); if (doubleCallback) done(result); }
  public static FakeAds Install(ECCAdResult result = ECCAdResult.Rewarded, bool ready = true) { var f = new FakeAds{result=result, ready=ready}; ECCAds.RegisterProvider(f); return f; }
}
