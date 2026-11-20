using System; using System.Linq; using System.Reflection; using System.Collections.Generic; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Levels;
// AGENTS.md task 13: the weekly star league
public static partial class T {
  // A far-future day of the league week ( ECCDaily never goes back, so tests only move forward )
  static DateTime LeagueDay(DayOfWeek weekday, int week){
    var d = new DateTime(2999,1,1); while (d.DayOfWeek != DayOfWeek.Saturday) d = d.AddDays(1);
    return d.AddDays(week*7 + ECCWeeklyLeague.DayOfLeagueWeek(NextWeekday(d, weekday)));
  }
  static DateTime NextWeekday(DateTime from, DayOfWeek w){ while (from.DayOfWeek != w) from = from.AddDays(1); return from; }
  static void UnlockLeague(){ PassLevels(ECCWeeklyLeague.UnlockLevel-1); ECCWeeklyLeague.CheckUnlock(); }
  static void SetLeagueDay(DateTime day){ ECCSave.Data.lastSeenDay = day.ToString("yyyy-MM-dd"); ECCSave.Save(); }
  static List<ECCLeagueEntry> RawCompetitors(){ var d = ECCSave.Data.league; return ECCWeeklyLeague.Generate(d.weekId, d.salt, d.firstWeek, d.startDay, ECCWeeklyLeague.TodayIndex, ECCLeagueConfig.Instance); }

  static void TestLeagueWeekBoundary(){
    Check(ECCWeeklyLeague.DayOfLeagueWeek(NextWeekday(new DateTime(2026,1,1), DayOfWeek.Saturday))==0 && ECCWeeklyLeague.DayOfLeagueWeek(NextWeekday(new DateTime(2026,1,1), DayOfWeek.Friday))==6, "league week: Saturday is day 0, Friday day 6");
    var fri = NextWeekday(new DateTime(2026,1,1), DayOfWeek.Friday);
    Check(ECCWeeklyLeague.WeekOf(fri.ToString("yyyy-MM-dd")) == fri.AddDays(-6).ToString("yyyy-MM-dd") && ECCWeeklyLeague.WeekOf(fri.AddDays(1).ToString("yyyy-MM-dd")) == fri.AddDays(1).ToString("yyyy-MM-dd"), "the week changes exactly at Friday 24:00 ( Saturday 00:00 )");
  }

  static void TestLeagueFirstInstall(){
    var config = ECCLeagueConfig.Instance;
    SetLeagueDay(LeagueDay(DayOfWeek.Wednesday, 0)); // installed on a Wednesday
    UnlockLeague();
    ECCWeeklyLeague.Ensure();
    var data = ECCSave.Data.league;
    var comps = RawCompetitors();
    Check(data.firstWeek && data.startDay==4 && ECCWeeklyLeague.PlayerStars==0, "first league week ( unlocked mid-week ): player at 0, competitors start growing on the unlock day");
    Check(comps.Count==config.leagueSize-1 && comps.Take(config.leaderCount).All(c=>c.stars>=config.firstWeekLeaderMin && c.stars<=config.firstWeekLeaderMax), "leading group 50-80 ahead, no retroactive gains for the elapsed days");
    Check(comps.Skip(config.leaderCount).All(c=>c.stars>=config.othersStartMin && c.stars<=config.othersStartMax), "the others start lower ( 0-40 )");
    Check(comps.Select(c=>c.name).Distinct().Count()==comps.Count && !comps.Any(c=>c.name.Contains("آنلاین")), "distinct invented nicknames");
    int rank = ECCWeeklyLeague.Rank; Check(rank > config.leaderCount && rank <= config.leagueSize, "rank below the leading group at the start");
    // The next day: once per day growth, deterministic after a restart
    var before = comps.Select(c=>c.stars).ToArray();
    SetLeagueDay(LeagueDay(DayOfWeek.Thursday, 0));
    var thu = RawCompetitors().Select(c=>c.stars).ToArray();
    Check(Enumerable.Range(0, before.Length).All(i=>thu[i]-before[i]>=0 && thu[i]-before[i]<=config.dailyGainMax), "a new day: each competitor gains 0-8");
    Check(Enumerable.Range(0, config.leaderCount).All(i=>thu[i]-before[i]>=config.leaderDailyGainMin), "the leading group gains 6-8");
    Restart();
    Check(RawCompetitors().Select(c=>c.stars).SequenceEqual(thu) && ECCSave.Data.league.weekId==data.weekId, "same table after a restart, nothing re-randomized");
    Check(RawCompetitors().Select(c=>c.stars).SequenceEqual(thu), "asking again the same day doesn't grow anyone");
  }

  static void TestLeagueGrowthPace(){
    var config = ECCLeagueConfig.Instance;
    // Normal weeks: average daily growth of the leading group and of the field, over many generated weeks
    double leaderGain=0, fieldGain=0; int leaderN=0, fieldN=0; bool capOk = true;
    for (int w=0; w<40; w++){
      string week = new DateTime(2030,1,5).AddDays(7*w).ToString("yyyy-MM-dd");
      var start = ECCWeeklyLeague.Generate(week, "salt", false, 0, 0, config);
      var end = ECCWeeklyLeague.Generate(week, "salt", false, 0, 6, config);
      for (int i=0;i<start.Count;i++){ double g=(end[i].stars-start[i].stars)/6.0; if (i<config.leaderCount){leaderGain+=g;leaderN++;} fieldGain+=g; fieldN++; if (end[i].stars>config.weeklyCap) capOk=false; }
      if (!start.Take(config.leaderCount).All(c=>c.stars>=config.leaderAheadMin && c.stars<=config.leaderAheadMax)) capOk=false;
    }
    leaderGain/=leaderN; fieldGain/=fieldN;
    Console.WriteLine("LEAGUE leaders gain "+leaderGain.ToString("0.0")+"/day, field "+fieldGain.ToString("0.0")+"/day");
    Check(leaderGain>=6 && leaderGain<=8, "leading group grows about 7 stars a day (never more than 8)");
    Check(capOk, "Saturday start: leaders 50-80, never above the weekly cap");
    Check(9 > config.dailyGainMax, "three 3-star wins a day ( 9 ) outpace every competitor's daily gain");
  }

  static void TestLeagueStarRules(){
    SetLeagueDay(LeagueDay(DayOfWeek.Sunday, 1));
    UnlockLeague();
    var a = ECCWeeklyLeague.RecordWin("c1-l1", 3, "win-a");
    Check(a.added==3 && ECCWeeklyLeague.PlayerStars==3 && !a.improvement, "a 3-star win adds exactly 3");
    Check(ECCWeeklyLeague.RecordWin("c1-l1", 3, "win-a")==null && ECCWeeklyLeague.PlayerStars==3, "the same win id never counts twice");
    var again = ECCWeeklyLeague.RecordWin("c1-l1", 3, "win-b");
    Check(again.added==0 && ECCWeeklyLeague.PlayerStars==3, "replaying the same level with the same stars adds nothing this week");
    ECCWeeklyLeague.RecordWin("c1-l2", 2, "win-c");
    var better = ECCWeeklyLeague.RecordWin("c1-l2", 3, "win-d");
    Check(better.added==1 && better.improvement && ECCWeeklyLeague.PlayerStars==6, "a replay adds only the improvement");
    Check(ECCWeeklyLeague.StarsText(a).Contains("ستاره به لیگ هفتگی") && ECCWeeklyLeague.StarsText(better).Contains("بهبود رکورد این هفته") && ECCWeeklyLeague.StarsText(again).Contains("اضافه نشد"), "win-screen star texts");
    Restart(); Check(ECCWeeklyLeague.PlayerStars==6 && ECCWeeklyLeague.BestThisWeek("c1-l2")==3, "weekly stars and level bests survive a restart");
    Check(ECCWeeklyLeague.RecordWin("c1-l2", 3, "win-d")==null, "a counted win stays counted after a restart");
  }

  static void TestLeagueReset(){
    var config = ECCLeagueConfig.Instance;
    SetLeagueDay(LeagueDay(DayOfWeek.Friday, 2)); ECCWeeklyLeague.Ensure();
    UnlockLeague();
    ECCWeeklyLeague.RecordWin("c1-l1", 3, "w1");
    string fridayWeek = ECCWeeklyLeague.WeekId;
    var saturday = LeagueDay(DayOfWeek.Saturday, 3);
    SetLeagueDay(saturday);
    ECCWeeklyLeague.Ensure();
    var data = ECCSave.Data.league;
    Check(data.weekId==saturday.ToString("yyyy-MM-dd") && data.weekId!=fridayWeek && ECCWeeklyLeague.PlayerStars==0 && !data.firstWeek && data.levelIds.Count==0, "Saturday: new week, player stars 0, level bests cleared");
    var start = RawCompetitors();
    Check(start.Take(config.leaderCount).All(c=>c.stars>=config.leaderAheadMin && c.stars<=config.leaderAheadMax), "first Saturday open: the leading group is 50-80 ahead");
    Check(ECCWeeklyLeague.RecordWin("c1-l1", 3, "w2").added==3, "levels can count again after the reset");
    SetLeagueDay(saturday.AddDays(1));
    var sunday = RawCompetitors();
    Check(Enumerable.Range(0,start.Count).All(i=>sunday[i].stars-start[i].stars>=0 && sunday[i].stars-start[i].stars<=8), "then each competitor grows 0-8 a day");
    // Closed for several weeks: lands on the current week once
    var later = saturday.AddDays(23); SetLeagueDay(later);
    Check(ECCWeeklyLeague.Ensure() && ECCWeeklyLeague.WeekId==ECCWeeklyLeague.WeekOf(later.ToString("yyyy-MM-dd")) && ECCWeeklyLeague.PlayerStars==0, "app closed across weeks: the current week, one reset");
    ECCWeeklyLeague.RecordWin("c2-l1", 2, "w3");
    Check(!ECCWeeklyLeague.Ensure() && ECCWeeklyLeague.PlayerStars==2, "no second reset in the same week");
    // Clock rollback: the protected day never goes back, so the week isn't reopened or reset again
    var oldNow = ECCSave.Data.lastSeenDay; ECCSave.Data.lastSeenDay = oldNow; // the device clock is far behind ( real now )
    Check(ECCDaily.Today==oldNow && ECCWeeklyLeague.WeekId==ECCWeeklyLeague.WeekOf(later.ToString("yyyy-MM-dd")) && ECCWeeklyLeague.PlayerStars==2, "clock rollback: no reset, no extra stars");
  }

  static void TestLeagueRankChanges(){
    var w = new ECCLeagueWin{ rank=8, previousRank=12 };
    Check(ECCWeeklyLeague.RankChangeText(w).Contains(ECCUIKit.PersianDigits(4)+" پله بهتر شد ↑"), "rank up text");
    w = new ECCLeagueWin{ rank=14, previousRank=12 };
    Check(ECCWeeklyLeague.RankChangeText(w).Contains(ECCUIKit.PersianDigits(2)+" پله پایین‌تر رفت ↓"), "rank down text");
    w = new ECCLeagueWin{ rank=12, previousRank=12 };
    Check(ECCWeeklyLeague.RankChangeText(w).Contains("تغییری نکرد"), "rank unchanged text");
    // Ties: the player is above competitors with the same stars
    SetLeagueDay(LeagueDay(DayOfWeek.Saturday, 6)); ECCWeeklyLeague.Ensure();
    UnlockLeague();
    var comps = RawCompetitors(); int target = comps.Max(c=>c.stars);
    ECCSave.Data.league.stars = target;
    Check(ECCWeeklyLeague.Rank==1 && ECCWeeklyLeague.Table()[0].isPlayer, "ties: the player ranks above");
    Check(ECCWeeklyLeague.TimeLeftText().StartsWith("پایان هفته:"), "time left text");
  }

  static void TestLeagueLevelIntegration(){
    typeof(ECCWeeklyLeague).GetProperty("PendingWin").SetValue(null, null);
    SetLeagueDay(LeagueDay(DayOfWeek.Monday, 8)); ECCWeeklyLeague.Ensure();
    UnlockLeague();
    ECCWeeklyLeague.MarkRankShown(ECCWeeklyLeague.Rank);
    var level = ECCLevelCatalog.Instance.AllLevels[0];
    ECCGameController gc; var mode = MakeLevelRun(out gc, level);
    mode.ShowResultScreen(false);
    Check(ECCWeeklyLeague.PlayerStars==0 && ECCWeeklyLeague.PendingWin==null, "a loss adds no league stars");
    FindButton("ButtonMenu").onClick.Invoke();
    mode = MakeLevelRun(out gc, level);
    mode.ShowResultScreen(true);
    int stars = ECCSave.GetStars(level.id);
    var root = UnityEngine.Object.FindAllRoots().Last(r=>r.gameObject.name=="CanvasLevelResult");
    string texts = Texts(root);
    Check(ECCWeeklyLeague.PlayerStars==stars && stars>0, "a normal win adds its stars");
    Check(texts.Contains(ECCPersianText.Fix("+" + ECCUIKit.PersianDigits(stars) + " ستاره به لیگ هفتگی")) && texts.Contains(ECCPersianText.Fix("رتبه " + ECCUIKit.PersianDigits(ECCWeeklyLeague.Rank))), "win screen shows the added stars and the rank");
    Check(ECCWeeklyLeague.PendingWin==null && ECCWeeklyLeague.LastShownRank==ECCWeeklyLeague.Rank, "the shown rank is remembered for the next change");
    mode.ShowResultScreen(true); // the same run reporting again
    Check(ECCWeeklyLeague.PlayerStars==stars, "duplicate win event of the same run adds nothing");
    FindButton("ButtonMenu").onClick.Invoke();
    // A skip after two losses never adds league stars
    var l2 = ECCLevelCatalog.Instance.AllLevels[ECCWeeklyLeague.UnlockLevel-1]; ECCLevelCatalog.Instance.SelectedLevel = l2;
    PlayerPrefs.SetInt("Money", ECCLevelProgress.SkipPrice+10);
    mode = MakeLevelRun(out gc, l2); mode.ShowResultScreen(false); FindButton("ButtonMenu").onClick.Invoke(); mode.ShowResultScreen(false);
    FindButton("ButtonSkip").onClick.Invoke();
    Check(ECCLevelProgress.IsSkipped(l2) && ECCWeeklyLeague.PlayerStars==stars, "a skipped level adds no league stars");
  }

  static void TestLeagueMenuAndScreen(){
    SetLeagueDay(LeagueDay(DayOfWeek.Tuesday, 9));
    UnlockLeague();
    var hub = MakeMenu(); ShowMenu(hub);
    var button = FindButton("ButtonLeague");
    int rank = ECCWeeklyLeague.Rank;
    Check(button!=null && Texts(button.transform).Contains(ECCPersianText.Fix("رتبه " + ECCUIKit.PersianDigits(rank))) && Texts(button.transform).Contains(ECCPersianText.Fix("لیگ هفتگی")), "menu button «لیگ هفتگی» with «رتبه N»");
    Check(ECCWeeklyLeague.LastShownRank==rank, "menu entry counts as the rank seen");
    var rect = (RectTransform)button.transform; var grid = new[]{"Buttonmissions","Buttonchallenge","Buttonwheel","Buttonothergames","Buttonlogin","Buttonachievements"}.Select(FindButton).Where(b=>b!=null).Select(b=>(RectTransform)b.transform).ToList();
    Check(grid.Count==6 && grid.All(g=>g.anchoredPosition.y - g.sizeDelta.y >= rect.anchoredPosition.y + 1), "league button sits below the feature grid, no overlap");
    ECCWeeklyLeague.RecordWin("c1-l3", 3, "menu-win");
    Check(Texts(button.transform).Contains(ECCPersianText.Fix("رتبه " + ECCUIKit.PersianDigits(ECCWeeklyLeague.Rank))), "rank on the button updates through the event");
    button.onClick.Invoke();
    Check(ECCWeeklyLeagueUI.IsOpen && !ECCChallengePopup.IsOpen && !ECCAds.IsBusy, "league screen opens, no other popup or ad");
    var root = UnityEngine.Object.FindAllRoots().Last(r=>r.gameObject.name=="CanvasWeeklyLeague");
    var rows = FindAll<RectTransform>(root).Where(t=>t.gameObject.name.StartsWith("LeagueRow_")||t.gameObject.name=="LeaguePlayer").ToList();
    string texts = Texts(root);
    Check(rows.Count==ECCLeagueConfig.Instance.leagueSize && FindAll<RectTransform>(root,"LeaguePlayer").Count==1 && texts.Contains(ECCPersianText.Fix("شما")), "the full table with the player's own row");
    Check(texts.Contains(ECCPersianText.Fix("لیگ هفتگی")) && texts.Contains(ECCPersianText.Fix("پایان هفته:")) && texts.Contains(ECCPersianText.Fix("جمعه شب ساعت ۲۴ امتیازها صفر می‌شن.")), "title, time left and explanation");
    Check(FindAll<ScrollRect>(root).Count==1, "scrollable list");
  }
}
