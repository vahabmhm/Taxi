using System; using System.Linq; using System.Reflection; using UnityEngine; using UnityEngine.UI; using EndlessCarChase; using EndlessCarChase.Modes; using EndlessCarChase.Levels;
public static partial class T {
  static ECCLevelMode MakeLevelRun(out ECCGameController gc, ECCLevelData level){
    var go = new GameObject("GameController"); gc = go.AddComponent<ECCGameController>();
    PlayerPrefs.SetInt("SelectedGameMode", (int)GameModeType.Levels);
    var mode = (ECCLevelMode)ECCGameModeManager.AttachMode(gc);
    gc.activeMode = mode; gc.gameStarted = true;
    if (level != null) typeof(ECCLevelMode).GetProperty("Level").SetValue(mode, level);
    typeof(ECCLevelMode).GetField("isRunning",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(mode,true);
    return mode;
  }
  static void TestLevelTimeContinue(){
    var level = new ECCLevelData{ title="t", objective=ECCObjectiveType.DestroyCops, target=5, timeLimit=60 }; level.id="custom";
    ECCGameController gc; var mode = MakeLevelRun(out gc, level);
    MethodInfo timeUp = typeof(ECCLevelMode).GetMethod("OnTimeUp",BindingFlags.NonPublic|BindingFlags.Instance);
    timeUp.Invoke(mode,null);
    Check(ECCContinueOfferUI.IsOpen && Time.timeScale==0 && !gc.isGameOver, "time up: run frozen, continue offer");
    var cta = FindButton("ButtonContinueVideo"); Check(cta.GetComponent<ECCPulse>()!=null && cta.GetComponent<ECCPulse>().enabled, "continue CTA pulses");
    var fake = FakeAds.Install(ECCAdResult.Skipped);
    cta.onClick.Invoke(); Coroutine.Run(3);
    Check(ECCContinueOfferUI.IsOpen && Bonus(mode)==0, "skipped video: no extra time, offer stays");
    fake.result = ECCAdResult.Rewarded; fake.doubleCallback = true;
    cta.onClick.Invoke(); cta.onClick.Invoke(); Coroutine.Run(3);
    Check(!ECCContinueOfferUI.IsOpen && Bonus(mode)==ECCEconomyConfig.Instance.levelContinueSeconds && Time.timeScale==1, "verified video: extra time once, same run");
    timeUp.Invoke(mode,null); Coroutine.Run(2);
    Check(!ECCContinueOfferUI.IsOpen && gc.isGameOver, "second time up in the run: no second offer, the run fails");
  }
  static void TestReviveOffer(){
    PlayerPrefs.SetInt("Money", 250);
    var go = new GameObject("GameController"); var gc = go.AddComponent<ECCGameController>(); gc.gameStarted = true;
    gc.activeMode = ECCGameModeManager.AttachMode(gc);
    var car = new GameObject("Player").AddComponent<ECCCar>(); car.healthMax = 10; car.health = 0; gc.playerObject = car; typeof(ECCCar).GetField("gameController",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null, gc);
    Check(ECCRevive.TryOffer(gc, car) && ECCContinueOfferUI.IsOpen && Time.timeScale==0, "wreck: revive offer, frozen");
    Check(!ECCRevive.TryOffer(gc, car), "no second offer while open");
    FindButton("ButtonContinuePay").onClick.Invoke();
    Check(!ECCContinueOfferUI.IsOpen && ECCWallet.Balance==50 && Time.timeScale==1, "paid revive: coins spent once, resumed"); Check(car.health==car.healthMax, "car repaired");
    Check(!ECCRevive.TryOffer(gc, car), "one revive per run");
  }
  static void TestLevelResultDouble(){
    PlayerPrefs.SetInt("Money", 0);
    var level = ECCLevelCatalog.Instance.AllLevels[0];
    ECCGameController gc; var mode = MakeLevelRun(out gc, level);
    mode.ShowResultScreen(true);
    int reward = level.reward;
    Check(ECCLevelResultUI.IsOpen && ECCWallet.Balance==reward && ECCSave.GetStars(level.id)>0, "victory: base reward once, stars saved");
    var dbl = FindButton("ButtonDouble"); Check(dbl.GetComponent<ECCPulse>().enabled, "2X button pulses");
    var fake = FakeAds.Install(ECCAdResult.Rewarded); fake.doubleCallback = true;
    dbl.onClick.Invoke(); dbl.onClick.Invoke(); Coroutine.Run(3);
    Check(ECCWallet.Balance==reward*2 && !dbl.interactable, "2X: bonus equal to base, paid once");
    FindButton("ButtonMenu").onClick.Invoke();
    for (int i=0;i<2;i++){ ECCLevelResultUI.Show(gc, level, true, 3, 25, 0); var b = FindButton("ButtonDouble"); b.onClick.Invoke(); Coroutine.Run(3); FindButton("ButtonMenu").onClick.Invoke(); }
    ECCLevelResultUI.Show(gc, level, true, 3, 25, 0);
    Check(!FindButton("ButtonDouble").interactable, "daily 2X limit reached after "+ECCEconomyConfig.Instance.levelDoubleDailyLimit);
  }
  static void TestSkipAfterTwoLosses(){
    var levels = ECCLevelCatalog.Instance.AllLevels; var l1 = levels[0]; var l2 = levels[1];
    PlayerPrefs.SetInt("Money", 1000);
    ECCGameController gc; var mode = MakeLevelRun(out gc, l1);
    mode.ShowResultScreen(false);
    Check(ECCLevelProgress.LossesInARow(l1)==1 && FindButton("ButtonSkip")==null, "one loss: no skip");
    FindButton("ButtonMenu").onClick.Invoke();
    mode.ShowResultScreen(false);
    Check(ECCLevelProgress.LossesInARow(l1)==2 && FindButton("ButtonSkip")!=null, "two losses in a row: skip offered");
    Check(FindButton("ButtonRetry").GetComponent<ECCPulse>().enabled, "retry CTA pulses on the loss screen");
    Check(!ECCLevelCatalog.Instance.IsUnlocked(l2), "next level locked before skipping");
    var skip = FindButton("ButtonSkip"); skip.onClick.Invoke(); skip.onClick.Invoke();
    Check(ECCWallet.Balance==1000-ECCLevelProgress.SkipPrice, "skip spends the price once");
    Check(ECCLevelProgress.IsSkipped(l1) && ECCSave.GetStars(l1.id)==0 && ECCLevelCatalog.Instance.IsUnlocked(l2) && ECCLevelCatalog.Instance.SelectedLevel==l2, "skip unlocks and selects the next level, no stars");
    Check(ECCLevelProgress.LossesInARow(l1)==0 && !ECCLevelResultUI.IsOpen, "counter reset after skip");
  }
  static void TestSkipWithVideo(){
    var levels = ECCLevelCatalog.Instance.AllLevels; var l1 = levels[0];
    PlayerPrefs.SetInt("Money", 10);
    ECCGameController gc; var mode = MakeLevelRun(out gc, l1);
    mode.ShowResultScreen(false); FindButton("ButtonMenu").onClick.Invoke(); mode.ShowResultScreen(false);
    var skip = FindButton("ButtonSkip"); Check(Texts(skip.transform).Contains(ECCPersianText.Fix("▶  تماشای ویدئو و رد کردن مرحله")), "not enough coins: video skip");
    var fake = FakeAds.Install(ECCAdResult.Skipped);
    skip.onClick.Invoke(); Coroutine.Run(3);
    Check(!ECCLevelProgress.IsSkipped(l1) && ECCWallet.Balance==10 && skip.interactable, "skipped video: not skipped");
    fake.result = ECCAdResult.Rewarded; skip.onClick.Invoke(); Coroutine.Run(3);
    Check(ECCLevelProgress.IsSkipped(l1) && ECCWallet.Balance==10, "verified video: skipped without coins");
  }
  static void TestLossCounterReset(){
    var levels = ECCLevelCatalog.Instance.AllLevels; var l1 = levels[0]; var l2 = levels[1];
    ECCLevelProgress.RecordResult(l1,false); ECCLevelProgress.RecordResult(l1,true);
    Check(ECCLevelProgress.LossesInARow(l1)==0, "win resets the counter");
    ECCLevelProgress.RecordResult(l1,false); ECCLevelProgress.RecordResult(l2,false); ECCLevelProgress.RecordResult(l1,false);
    Check(ECCLevelProgress.LossesInARow(l1)==1, "another level resets the counter");
  }
  static void TestEndlessResult(){
    PlayerPrefs.SetInt("SelectedGameMode", 0);
    var go = new GameObject("GameController"); var gc = go.AddComponent<ECCGameController>();
    gc.activeMode = ECCGameModeManager.AttachMode(gc); gc.score = 300; gc.highScore = 100;
    Check(gc.activeMode.ShowsOwnResultScreen, "endless mode has its own result screen");
    gc.activeMode.ShowResultScreen(false);
    Check(ECCEndlessResultUI.IsOpen && gc.highScore==300 && PlayerPrefs.GetInt(gc.HighScoreKey,0)==300, "result shown, new record saved");
  }
}
