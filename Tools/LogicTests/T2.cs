using System; using System.Linq; using System.Collections.Generic; using UnityEngine; using EndlessCarChase;
public static partial class T {
  static void TestMissionCatalog(){
    var all = ECCMissionCatalog.Instance.missions;
    Check(all.Length==22, "22 taxi missions");
    Check(all.Count(m=>m.difficulty==ECCMissionDifficulty.Easy)==8 && all.Count(m=>m.difficulty==ECCMissionDifficulty.Medium)==8 && all.Count(m=>m.difficulty==ECCMissionDifficulty.Hard)==6, "8 easy 8 medium 6 hard");
    Check(all.Select(m=>m.id).Distinct().Count()==all.Length, "unique ids");
    Check(all.All(ECCMissions.IsAvailable), "all achievable");
    Check(all.All(m=>m.reward>0 && m.Text.Contains(m.target.ToString())), "text has target, reward set");
  }
  static void TestMissionSelection(){
    var counts = new HashSet<int>();
    for (int d=1; d<=60; d++){
      string day = new DateTime(2026,1,1).AddDays(d).ToString("yyyy-MM-dd");
      var a = ECCMissions.Generate(day); var b = ECCMissions.Generate(day);
      var defs = a.Select(s=>ECCMissionCatalog.Instance.Get(s.id)).ToList();
      if (!(a.Count>=4 && a.Count<=5)) { Check(false,"count 4-5 on "+day); return; }
      if (a.Select(s=>s.id).Distinct().Count()!=a.Count) { Check(false,"distinct "+day); return; }
      if (!a.Select(s=>s.id).SequenceEqual(b.Select(s=>s.id))) { Check(false,"stable "+day); return; }
      if (defs.Select(m=>m.difficulty).Distinct().Count()!=3) { Check(false,"mixed difficulty "+day); return; }
      if (defs.Select(m=>m.type).Distinct().Count()!=defs.Count) { Check(false,"distinct types "+day); return; }
      counts.Add(a.Count);
    }
    Check(counts.Contains(4) && counts.Contains(5), "both 4 and 5 happen over 60 days");
    Check(!ECCMissions.Generate("2026-03-01").Select(s=>s.id).SequenceEqual(ECCMissions.Generate("2026-03-02").Select(s=>s.id)), "different days differ");
  }
  static ECCDailyMissionState FindType(ECCMissionType t){ return ECCMissions.Today.FirstOrDefault(s=>ECCMissions.GetDefinition(s).type==t); }
  static void TestMissionProgressAndClaim(){
    PlayerPrefs.SetInt("Money", 0);
    var today = ECCMissions.Today; var ids = today.Select(s=>s.id).ToList();
    Restart(); Check(ECCMissions.Today.Select(s=>s.id).SequenceEqual(ids), "same missions after restart");
    var s0 = ECCMissions.Today[0]; var def = ECCMissions.GetDefinition(s0);
    int changed=0; Action h=()=>changed++; ECCMissions.Changed+=h;
    if (def.KeepsBest) { ECCMissions.Report(def.type, def.target-1); ECCMissions.Report(def.type, 1); Check(s0.progress==def.target-1, "keep best does not add"); }
    else { ECCMissions.Report(def.type, def.target-1); Check(s0.progress==def.target-1, "progress adds"); }
    Check(!ECCMissions.CanClaim(s0) && ECCMissions.Claim(s0)==0, "not completed cannot claim");
    ECCMissions.Report(def.type, def.target + 5);
    Check(s0.progress==def.target, "progress capped at target");
    Check(ECCMissions.ClaimableCount>=1, "claimable count");
    Restart(); s0 = ECCMissions.Today[0];
    Check(s0.progress==def.target, "progress persisted");
    int paid = ECCMissions.Claim(s0);
    Check(paid==def.reward && ECCWallet.Balance==def.reward, "claim pays reward");
    Check(ECCMissions.Claim(s0)==0 && ECCWallet.Balance==def.reward, "second claim blocked");
    s0.claimed=false; Check(ECCMissions.Claim(s0)==0 && ECCWallet.Balance==def.reward, "transaction id blocks double pay even if flag lost");
    Check(changed>=2, "change events");
    ECCMissions.Changed-=h;
    // New day
    ECCSave.Data.lastSeenDay = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
    var next = ECCMissions.Today;
    Check(next.All(s=>s.progress==0 && !s.claimed) && ECCSave.Data.dailyMissionDay==ECCSave.Data.lastSeenDay, "new day resets missions");
  }
  static void TestLegacyMissionMigration(){
    PlayerPrefs.SetInt("Money", 100);
    var d = ECCSave.Data; d.missionDay = DateTime.Now.ToString("yyyy-MM-dd");
    d.missions.Add(new ECCMissionState{type=0,target=10,progress=10,reward=150});
    d.missions.Add(new ECCMissionState{type=1,target=10,progress=3,reward=250});
    d.missions.Add(new ECCMissionState{type=2,target=10,progress=10,reward=400,claimed=true});
    ECCSave.Save(); Restart();
    var t = ECCMissions.Today;
    Check(ECCWallet.Balance==250 && ECCSave.Data.missions.Count==0, "legacy unclaimed completed mission paid once ("+ECCWallet.Balance+")");
    Restart(); var t2 = ECCMissions.Today; Check(ECCWallet.Balance==250, "legacy not paid twice");
  }
}
public static partial class T {
  static void TestMissionsScreen(){
    PlayerPrefs.SetInt("Money", 0);
    var s0 = ECCMissions.Today[0]; var def = ECCMissions.GetDefinition(s0);
    s0.progress = def.target; ECCSave.Save();
    ECCDailyMissionsUI.Open();
    var popup = (ECCDailyMissionsUI)typeof(ECCDailyMissionsUI).GetField("open",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
    Check(popup!=null && ECCModalTracker.AnyOpen, "missions popup open and registered as modal");
    var root = popup.transform.root;
    var claims = FindAll<UnityEngine.UI.Button>(root, "Claim");
    Check(claims.Count==ECCMissions.Today.Count, "one claim button per mission ("+claims.Count+")");
    Check(Texts(root).Contains(ECCPersianText.Fix("آماده دریافت")) && Texts(root).Contains(ECCPersianText.Fix("تکمیل نشده")), "ready and not-completed labels");
    var ready = claims.First(b=>b.interactable);
    ready.onClick.Invoke(); ready.onClick.Invoke();
    Check(ECCWallet.Balance==def.reward, "claim through UI pays once");
    Check(Texts(root).Contains(ECCPersianText.Fix("دریافت شده")), "claimed label after claim");
    ECCDailyMissionsUI.Open();
    Check(FindAll<ECCDailyMissionsUI>(root).Count==1, "no second missions popup");
  }
}
public static partial class T {
  static void TestModalClosesCleanly(){
    ECCDailyMissionsUI.Open();
    Check(ECCDailyMissionsUI.IsOpen && ECCModalTracker.AnyOpen, "open");
    var popup = (ECCDailyMissionsUI)typeof(ECCDailyMissionsUI).GetField("open",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
    FindAll<UnityEngine.UI.Button>(popup.transform.root, "ButtonClose")[0].onClick.Invoke();
    Check(!ECCDailyMissionsUI.IsOpen && !ECCModalTracker.AnyOpen, "closed and modal registry empty");
  }
}
