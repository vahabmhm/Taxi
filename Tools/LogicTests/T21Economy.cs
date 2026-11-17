using System; using System.Linq; using UnityEngine; using EndlessCarChase; using EndlessCarChase.Levels;
// The coin economy audit ( AGENTS.md task 1 ): the model is an estimate built from the real config values
public static partial class T {
  static void TestEconomyModel(){
    var config = ECCEconomyConfig.Instance;
    Console.WriteLine("ECONOMY\n" + ECCEconomyModel.Report());
    foreach (int chapter in new[]{0, 9, 24}) {
      var days = ECCEconomyModel.Profiles(chapter).Select(ECCEconomyModel.Simulate).ToArray();
      var casual = days[0]; var regular = days[1]; var oneAd = days[2]; var active = days[3];
      string at = " (chapter " + (chapter+1) + ")";
      Check(casual.Savings > 0 && casual.Income >= 2 * ECCEconomyModel.AveragePowerUpPrice(config), "casual player without videos still gains coins and can buy power-ups" + at);
      Check(regular.Savings > regular.Income * 0.3f, "regular player without videos keeps a third of the income for cars and upgrades" + at);
      float gift = config.GetRewardedCoinGift(chapter);
      float share = gift / regular.Income;
      Check(share >= 0.07f && share <= 0.2f, "one gift video is a noticeable top-up, 7-20% of a regular day's income (" + Mathf.RoundToInt(share*100) + "%)" + at);
      Check(gift >= 0.1f * regular.Consumables || gift >= ECCEconomyModel.AveragePowerUpPrice(config), "one gift video pays for at least one average power-up" + at);
      Check(active.AdIncome < active.Income * 0.35f, "two videos are a top-up, not the main income (" + Mathf.RoundToInt(active.AdIncome/active.Income*100) + "%)" + at);
      Check(oneAd.Income > regular.Income && oneAd.Income < regular.Income * 1.25f, "one video adds less than a quarter" + at);
    }
    // Meaningful prices
    Check(config.skipLevelPrice >= 2 * ECCEconomyModel.LevelReward(0), "level skip costs at least two early level rewards");
    Check(config.reviveCoinPrice > ECCEconomyModel.AveragePowerUpPrice(config), "a coin revive costs more than a power-up");
    // No easy farming loop: replaying the first level earns less per minute than playing new levels or the shift
    Check(ECCEconomyModel.ReplayFarmingPerMinute() < ECCEconomyModel.ProgressPerMinute(0), "replaying the easiest level pays less than progressing");
    Check(ECCEconomyModel.ReplayFarmingPerMinute() < ECCEconomyModel.ProgressPerMinute(0) * 0.7f, "replay farming pays clearly less than playing new levels");
    // The gift grows with the chapter but stays capped and is what the popup offers
    Check(config.GetRewardedCoinGift(0) == config.rewardedCoinGift && config.GetRewardedCoinGift(29) <= config.rewardedCoinGiftMax && config.GetRewardedCoinGift(10) > config.GetRewardedCoinGift(0), "gift: base in chapter 1, grows, capped");
    Check(ECCCoinGiftPopup.CurrentOffer == config.GetRewardedCoinGift(0), "new player's offer is the chapter 1 gift");
    var levels = ECCLevelCatalog.Instance.AllLevels; foreach (var l in levels.Take(25)) ECCLevelProgress.RecordResult(l, true);
    foreach (var l in levels.Take(25)) ECCSave.SetStars(l.id, 1);
    Check(ECCLevelCatalog.Instance.FurthestChapter == 2 && ECCCoinGiftPopup.CurrentOffer == config.GetRewardedCoinGift(2), "offer follows the furthest chapter");
  }
}
