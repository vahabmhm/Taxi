using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase;
// In-run messages in the top banner, advice after bad events and the pulsing power-up buttons
public static partial class T {
  static RectTransform PowerUpRoot(string type){ foreach (var t in UnityEngine.Object.FindAllRoots()) { var r = FindAll<RectTransform>(t, "PowerUp"+type); if (r.Count>0) return r[0]; } return null; }
  static bool Highlighted(RectTransform root){ var pulse = root.GetComponent<ECCPulse>(); var halo = root.kids.FirstOrDefault(k=>k.gameObject.name=="Halo"); return pulse!=null && pulse.enabled && halo!=null && halo.gameObject.activeSelf; }
  static void TestRunMessagesAndAdvice(){
    ECCGameController gc; ECCPowerUpManager manager; var bar = MakeChaseWithBar(out gc, out manager);
    ECCHintBanner.Clear(); Coroutine.Run(0.1f);
    var repair = PowerUpRoot("Repair"); var shield = PowerUpRoot("Shield");
    Check(repair!=null && shield!=null && !Highlighted(repair), "power-up buttons built, not highlighted");
    ECCRunMessages.NearMiss(5); Coroutine.Run(0.2f);
    Check(ECCHintBanner.Current=="از بیخ گوش!", "near-miss bonus: shown in the top banner");
    ECCRunMessages.CarInDanger(); Coroutine.Run(0.6f);
    Check(ECCHintBanner.Current=="هشدار! ماشینت داره نابود میشه", "danger is urgent: replaces the bonus within half a second");
    Check(!Highlighted(repair), "no pulse before the advice appears");
    Coroutine.Run(2.6f);
    Check(ECCHintBanner.Current=="تعمیرش کن!" && Highlighted(repair) && Highlighted(shield), "advice follows; repair and shield buttons pulse");
    var update = typeof(ECCPowerUpBar).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Instance);
    Coroutine.Run(4); ECCRunMessages.CarInDanger(); Coroutine.Run(2.5f); update.Invoke(bar,null);
    Check(ECCHintBanner.Current!="تعمیرش کن!", "same advice not repeated within the cooldown");
    Coroutine.Run(1); update.Invoke(bar,null);
    Check(!Highlighted(repair) && !Highlighted(shield), "pulse stops after a few seconds");
    ECCHintBanner.Clear();
    ECCHintBanner.Show("Icons/target", Color.white, "هدف", "متن"); ECCRunMessages.NearMiss(5); ECCRunMessages.NearMiss(5); Coroutine.Run(0.2f);
    Check(ECCHintBanner.Current=="هدف", "frequent bonus never interrupts or queues behind a normal hint");
    ECCHintBanner.Clear(); ECCRunMessages.Crash(); Coroutine.Run(2.1f);
    Check(ECCHintBanner.Current=="با «سپر» آسیب نبین!" && Highlighted(shield), "crash: shield advice with a pulsing shield button");
    // ( repair on a full car isn't applied, so the stub doesn't need to build effects )
    ECCPowerUpBar.Highlight(5, ECCPowerUpType.Repair); var face = repair.kids.First(k=>k.gameObject.name=="Face").gameObject.GetComponent<Button>(); face.onClick.Invoke();
    Check(!Highlighted(repair), "tapping the highlighted button stops its pulse");
    gc.GameOverNow(); Check(ECCHintBanner.Current==null, "game over clears the banner");
  }
}
