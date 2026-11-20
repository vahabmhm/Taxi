using System; using System.Linq; using System.Collections.Generic; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Levels;
// AGENTS.md task 13 update: the league opens at level 8, and the name pool
public static partial class T {
  static Button LatestButton(string name){ var root = UnityEngine.Object.FindAllRoots().Last(r=>FindAll<Button>(r,name).Count>0); return FindAll<Button>(root,name)[0]; }
  static void TestLeagueLockedBeforeLevel8(){
    SetLeagueDay(LeagueDay(DayOfWeek.Sunday, 20));
    PassLevels(ECCWeeklyLeague.UnlockLevel-2); // level 7 not completed yet
    Check(!ECCWeeklyLeague.IsUnlocked && ECCWeeklyLeague.Rank==0 && string.IsNullOrEmpty(ECCSave.Data.league.weekId), "before level 8: locked, no rank, no week or competitors");
    Check(ECCWeeklyLeague.RecordWin("c1-l1", 3, "pre")==null && ECCSave.Data.league.stars==0, "no stars counted while locked");
    var hub = MakeMenu(); ShowMenu(hub);
    var button = LatestButton("ButtonLeague");
    string texts = Texts(button.transform);
    var lockIcons = FindAll<Image>(button.transform,"Lock"); var rankTexts = FindAll<Text>(button.transform,"Rank");
    Check(lockIcons.Count==1 && rankTexts.Count==0 && !texts.Contains(ECCPersianText.Fix("رتبه")), "menu button shows the lock, no rank or numbers");
    button.onClick.Invoke();
    var locked = UnityEngine.Object.FindAllRoots().LastOrDefault(r=>r.gameObject.name=="CanvasLeagueLocked");
    Check(locked!=null && !ECCWeeklyLeagueUI.IsOpen && Texts(locked).Contains(ECCPersianText.Fix("وقتی به مرحله ۸ برسی، وارد لیگ هفتگی می‌شی و می‌تونی رتبه‌ات رو ببینی.")) && Texts(locked).Contains(ECCPersianText.Fix("باشه")), "tap: the locked info popup with «باشه», never the league screen");
    button.onClick.Invoke();
    Check(UnityEngine.Object.FindAllRoots().Count(r=>r.gameObject.name=="CanvasLeagueLocked" && r.gameObject.activeSelf)==1, "no stacking on an open popup");
    FindAll<Button>(locked,"ButtonOk")[0].onClick.Invoke();
    Check(!ECCModalTracker.AnyOpen, "«باشه» closes it");
    ECCLeagueConfig.Instance.leagueUnlockLevel = 12; Check(ECCWeeklyLeague.LockedText.Contains("۱۲"), "the level in the text comes from leagueUnlockLevel"); ECCLeagueConfig.Instance.leagueUnlockLevel = 8;
    // A locked win screen has no league section
    var level = ECCLevelCatalog.Instance.AllLevels[0];
    ECCGameController gc; var mode = MakeLevelRun(out gc, level); mode.ShowResultScreen(true);
    var root = UnityEngine.Object.FindAllRoots().Last(r=>r.gameObject.name=="CanvasLevelResult");
    Check(!Texts(root).Contains(ECCPersianText.Fix("لیگ هفتگی")), "locked: no league section on the win screen");
    FindButton("ButtonMenu").onClick.Invoke();
  }
  static void TestLeagueUnlockAtLevel8(){
    var day = LeagueDay(DayOfWeek.Tuesday, 21); SetLeagueDay(day);
    var levels = ECCLevelCatalog.Instance.AllLevels;
    for (int i=0;i<6;i++) ECCSave.SetStars(levels[i].id, 3);
    ECCGameController gc; var mode = MakeLevelRun(out gc, levels[6]); mode.ShowResultScreen(true); // wins level 7 -> level 8 reached
    var data = ECCSave.Data.league;
    var root = UnityEngine.Object.FindAllRoots().Last(r=>r.gameObject.name=="CanvasLevelResult");
    Check(data.unlocked && data.unlockDay==day.ToString("yyyy-MM-dd") && data.firstWeekId==ECCWeeklyLeague.WeekOf(data.unlockDay) && data.startDay==ECCWeeklyLeague.TodayIndex, "winning level 7 unlocks once; the first week starts on the unlock day");
    Check(data.stars==0, "no retroactive stars: the unlocking win and earlier wins don't count");
    Check(Texts(root).Contains(ECCPersianText.Fix("لیگ هفتگی برات باز شد! حالا رتبه‌ات رو ببین.")) && FindButton("ButtonNext")!=null, "unlock message on that win screen, result actions untouched");
    Check(!Texts(root).Contains(ECCPersianText.Fix("ستاره به لیگ هفتگی")) && data.unlockMessageShown, "message shown once instead of the league section");
    FindButton("ButtonMenu").onClick.Invoke();
    Restart(); Check(ECCWeeklyLeague.IsUnlocked && !ECCWeeklyLeague.CheckUnlock() && ECCSave.Data.league.unlockDay==day.ToString("yyyy-MM-dd"), "stays unlocked after a restart, recorded once");
    var hub = MakeMenu(); ShowMenu(hub);
    var button = LatestButton("ButtonLeague");
    Check(button.GetComponent<ECCPulse>()!=null && button.GetComponent<ECCPulse>().enabled && Texts(button.transform).Contains(ECCPersianText.Fix("رتبه")), "unlocked: rank on the button and a one-time pulse");
    button.onClick.Invoke(); Check(ECCWeeklyLeagueUI.IsOpen && ECCWeeklyLeague.WasOpened, "opens the league screen");
    ShowMenu(hub); Check(!button.GetComponent<ECCPulse>().enabled, "pulse stops after the first open");
    var w = ECCWeeklyLeague.RecordWin(levels[7].id, 3, "after"); Check(w!=null && w.added==3, "stars count after the unlock");
  }
  static void TestLeagueUnlockMigration(){
    PassLevels(40); // an existing player already past level 8
    Check(ECCWeeklyLeague.IsUnlocked && ECCSave.Data.league.stars==0, "update past level 8: unlocked on the next launch, from 0");
    var hub = MakeMenu(); ShowMenu(hub);
    Check(ECCSave.Data.league.unlockMessageShown, "the unlock message is shown at the menu ( toast, not a modal )");
    // Clock rollback can't unlock early: the unlock only follows the level progress
    Wipe(); ECCSave.Data.lastSeenDay = "2999-12-31"; Check(!ECCWeeklyLeague.IsUnlocked, "clock changes never unlock the league");
  }
  static void TestLeagueNamePool(){
    Check(ECCWeeklyLeague.BoyNames.Length>=40 && ECCWeeklyLeague.GirlNames.Length>=40 && ECCWeeklyLeague.Nicknames.Length>=30, "at least 40 boy, 40 girl names and 30 nicknames");
    var all = ECCWeeklyLeague.BoyNames.Concat(ECCWeeklyLeague.GirlNames).ToList();
    Check(all.All(n=>!n.Contains(" ")) && all.Distinct().Count()==all.Count && !ECCWeeklyLeague.BoyNames.Intersect(ECCWeeklyLeague.GirlNames).Any(), "first names only ( no surnames ), no duplicates");
    var config = ECCLeagueConfig.Instance; int boys=0, girls=0, nicks=0, total=0; bool unique=true, stable=true;
    Func<string,string> baseName = n => { foreach (var s in new[]{" ","_",".",}) { int i=n.IndexOf(s); if (i>0 && !ECCWeeklyLeague.Nicknames.Contains(n)) return n.Substring(0,i); } return n; };
    for (int w=0; w<60; w++){
      string week = new DateTime(2031,1,4).AddDays(7*w).ToString("yyyy-MM-dd");
      var t = ECCWeeklyLeague.Generate(week, "s"+w, false, 0, 3, config);
      if (t.Select(e=>e.name).Distinct().Count()!=t.Count) unique=false;
      if (!ECCWeeklyLeague.Generate(week, "s"+w, false, 0, 3, config).Select(e=>e.name).SequenceEqual(t.Select(e=>e.name))) stable=false;
      foreach (var e in t){ total++; string b=baseName(e.name); if (ECCWeeklyLeague.BoyNames.Contains(b)) boys++; else if (ECCWeeklyLeague.GirlNames.Contains(b)) girls++; else if (ECCWeeklyLeague.Nicknames.Contains(e.name)) nicks++; }
    }
    Console.WriteLine("LEAGUE names boys "+(100*boys/total)+"% girls "+(100*girls/total)+"% nicknames "+(100*nicks/total)+"%");
    Check(unique, "no duplicate name in a week's table");
    Check(stable, "the same week gives the same names ( restart-safe )");
    Check(boys+girls+nicks==total && Math.Abs(boys-girls) < total*0.08 && nicks > total*0.2 && nicks < total*0.4, "boys, girls and nicknames mixed about 35/35/30");
  }
}
