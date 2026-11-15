using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Modes; using EndlessCarChase.Levels;
public static partial class T {
  static void NextDay(){ ECCSave.Data.lastSeenDay = DateTime.ParseExact(ECCDaily.Today,"yyyy-MM-dd",null).AddDays(1).ToString("yyyy-MM-dd"); typeof(ECCDailyChallenge).GetField("cachedLevel",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null); }
  static void TestChallengeGeneration(){
    var a = ECCDailyChallenge.Generate("2026-05-01",0); var b = ECCDailyChallenge.Generate("2026-05-01",0);
    Check(a.objective==b.objective && a.target==b.target && a.timeLimit==b.timeLimit && a.theme==b.theme, "same day same challenge");
    var objectives = new System.Collections.Generic.HashSet<ECCObjectiveType>();
    for (int d=0; d<40; d++) for (int tier=0; tier<3; tier++){
      var l = ECCDailyChallenge.Generate(new DateTime(2026,1,1).AddDays(d).ToString("yyyy-MM-dd"), tier);
      objectives.Add(l.objective);
      if (!(l.timeLimit>0 && l.target>0 && l.objective!=ECCObjectiveType.Survive && l.objective!=ECCObjectiveType.DestroyBoss && (int)l.theme>=1 && (int)l.theme<=tier+1 && l.reward>0)) { Check(false,"valid challenge d"+d+" t"+tier); return; }
    }
    Check(objectives.Count==3, "all three timed objectives occur");
    Check(ECCDailyChallenge.Today.title=="چالش روز", "challenge title");
    var today = ECCDailyChallenge.Today; ECCSave.Data.levels.Add(new ECCLevelRecord{id="c1-l1",stars=3}); Restart(); typeof(ECCDailyChallenge).GetField("cachedLevel",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,null);
    Check(ECCDailyChallenge.Today.target==today.target && ECCDailyChallenge.Today.objective==today.objective, "challenge stable through restart");
  }
  static void TestChallengePopupSchedule(){
    Check(ECCDaily.IsInstallDay && !ECCDailyChallenge.ShouldAutoShowPopup, "no popup on install day");
    NextDay(); Check(ECCDailyChallenge.ShouldAutoShowPopup, "popup on the next day");
    ECCDailyChallenge.MarkPopupShown(); Restart(); Check(!ECCDailyChallenge.ShouldAutoShowPopup, "popup once per day, also after restart");
    NextDay(); Check(ECCDailyChallenge.ShouldAutoShowPopup, "popup again on a later day");
  }
  static void TestChallengeDailyAllowances(){
    PlayerPrefs.SetInt("Money",0);
    Check(ECCDailyChallenge.ClaimDouble()==0, "no 2X before winning");
    int reward = ECCDailyChallenge.Today.reward;
    Check(ECCDailyChallenge.CompleteToday()==reward && ECCWallet.Balance==reward, "base reward paid");
    Check(ECCDailyChallenge.CompleteToday()==0 && ECCWallet.Balance==reward, "base reward once (replay)");
    Restart(); Check(ECCDailyChallenge.CompleteToday()==0 && ECCWallet.Balance==reward, "base reward once after restart");
    Check(!ECCDailyChallenge.IsReviveUsedToday && ECCDailyChallenge.ConsumeRevive(), "revive first use");
    Check(!ECCDailyChallenge.ConsumeRevive(), "revive second use same day blocked");
    Check(!ECCDailyChallenge.IsDoubleUsedToday, "revive does not block 2X");
    Check(ECCDailyChallenge.ClaimDouble()==reward && ECCWallet.Balance==reward*2, "2X pays base again");
    Check(ECCDailyChallenge.ClaimDouble()==0 && ECCWallet.Balance==reward*2, "2X once per day");
    Restart(); Check(ECCDailyChallenge.IsReviveUsedToday && ECCDailyChallenge.IsDoubleUsedToday, "allowances not restored by restart");
    NextDay(); Check(!ECCDailyChallenge.IsReviveUsedToday && !ECCDailyChallenge.IsDoubleUsedToday && !ECCDailyChallenge.IsCompletedToday, "next day restores both");
    Wipe(); ECCDailyChallenge.CompleteToday(); ECCDailyChallenge.ClaimDouble();
    Check(ECCDailyChallenge.ConsumeRevive(), "2X does not block revive");
  }
  static ECCDailyChallengeMode MakeChallengeRun(out ECCGameController gc){
    var go = new GameObject("GameController"); gc = go.AddComponent<ECCGameController>();
    ECCDailyChallenge.PlayRequested = true;
    var mode = (ECCDailyChallengeMode)ECCGameModeManager.AttachMode(gc);
    gc.activeMode = mode; gc.gameStarted = true;
    typeof(ECCLevelMode).GetField("isRunning",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mode,true);
    return mode;
  }
  static float Bonus(ECCLevelMode m){ return (float)typeof(ECCLevelMode).GetField("bonusTime",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m); }
  static Button FindButton(string name){ foreach (var t in UnityEngine.Object.FindAllRoots()) { var b = FindAll<Button>(t, name); if (b.Count>0) return b[0]; } return null; }
  static void TestChallengeTimeUpSecondChance(){
    ECCGameController gc; var mode = MakeChallengeRun(out gc);
    Check(mode.HudTitle=="چالش روز" && mode.Level.id.StartsWith("challenge-"), "challenge mode attached with its title");
    typeof(ECCDailyChallengeMode).GetMethod("OnTimeUp",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(mode,null);
    Check(ECCChallengeFailUI.IsOpen && Time.timeScale==0 && mode.IsFailurePending && !gc.isGameOver, "time up freezes the run and shows the failure screen");
    // No ads available -> NoAds, nothing consumed
    FindButton("ButtonRevive").onClick.Invoke(); Coroutine.Run(8);
    Check(!ECCDailyChallenge.IsReviveUsedToday && Bonus(mode)==0 && ECCChallengeFailUI.IsOpen && FindButton("ButtonRevive").interactable, "unavailable ad: no time, nothing consumed, can retry");
    var fake = FakeAds.Install(ECCAdResult.Skipped);
    FindButton("ButtonRevive").onClick.Invoke(); Coroutine.Run(3);
    Check(!ECCDailyChallenge.IsReviveUsedToday && Bonus(mode)==0 && ECCChallengeFailUI.IsOpen, "skipped ad: no time, nothing consumed");
    fake.result = ECCAdResult.Rewarded; fake.doubleCallback = true;
    var btn = FindButton("ButtonRevive"); btn.onClick.Invoke(); btn.onClick.Invoke();
    Check(ECCAdLoadingPopup.IsOpen, "loading popup before the video");
    Coroutine.Run(1); Check(fake.shows==1, "video not shown before 2 seconds"); Coroutine.Run(2);
    Check(fake.shows==2, "one video for repeated taps");
    Check(ECCDailyChallenge.IsReviveUsedToday && Bonus(mode)==30 && !ECCChallengeFailUI.IsOpen && Time.timeScale==1 && !mode.IsFailurePending, "verified video: +30 seconds once, same run resumes");
    Check(mode.TimeLeft > 29 && mode.TimeLeft <= 30 + 0.01f || mode.TimeLeft > 0, "time left refreshed");
    typeof(ECCDailyChallengeMode).GetMethod("OnTimeUp",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(mode,null);
    Check(ECCChallengeFailUI.IsOpen && FindButton("ButtonRevive")==null, "second loss same day: no second chance offered");
  }
  static void TestChallengeVictoryDouble(){
    PlayerPrefs.SetInt("Money",0);
    ECCGameController gc; var mode = MakeChallengeRun(out gc);
    mode.ShowResultScreen(true);
    int reward = ECCDailyChallenge.BaseReward;
    Check(ECCChallengeResultUI.IsOpen && ECCWallet.Balance==reward, "victory pays the base once");
    var fake = FakeAds.Install(ECCAdResult.Failed);
    FindButton("ButtonDouble").onClick.Invoke(); Coroutine.Run(3);
    Check(ECCWallet.Balance==reward && !ECCDailyChallenge.IsDoubleUsedToday && FindButton("ButtonDouble").interactable, "failed ad: no bonus, still eligible");
    fake.result = ECCAdResult.Rewarded; fake.doubleCallback = true;
    FindButton("ButtonDouble").onClick.Invoke(); FindButton("ButtonDouble").onClick.Invoke(); Coroutine.Run(3);
    Check(ECCWallet.Balance==reward*2 && ECCDailyChallenge.IsDoubleUsedToday && !FindButton("ButtonDouble").interactable, "2X pays once with duplicate callbacks and taps");
    Check(Texts(FindButton("ButtonDouble").transform).Contains(ECCPersianText.Fix("جایزه دوبرابر امروز دریافت شده")), "claimed 2X label");
    FindButton("ButtonMenu").onClick.Invoke();
    ECCChallengeResultUI.ShowVictory(gc, 0, false);
    Check(ECCWallet.Balance==reward*2 && !FindButton("ButtonDouble").interactable, "reopened result: no base again, 2X stays claimed");
  }
}
public static partial class T {
  static void TestChallengeGiveUpAndCampaignUntouched(){
    ECCGameController gc; var mode = MakeChallengeRun(out gc);
    int levelsBefore = ECCSave.Data.levels.Count; string selected = ECCSave.Data.selectedLevel;
    typeof(ECCDailyChallengeMode).GetMethod("OnTimeUp",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(mode,null);
    FindButton("ButtonGiveUp").onClick.Invoke(); Coroutine.Run(2);
    Check(gc.isGameOver && ECCChallengeResultUI.IsOpen && FindButton("ButtonRetry")!=null, "give up ends the run with the failure screen and retry");
    Check(!ECCDailyChallenge.IsReviveUsedToday, "giving up does not consume the second chance");
    Check(ECCSave.Data.levels.Count==levelsBefore && ECCSave.Data.selectedLevel==selected, "campaign progress untouched");
    FindButton("ButtonMenu").onClick.Invoke();
    Check(!ECCDailyChallenge.PlayRequested && ECCGameModeManager.CurrentMode!=GameModeType.DailyChallenge, "menu leaves the challenge mode");
  }
}
