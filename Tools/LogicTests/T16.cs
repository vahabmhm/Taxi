using System; using System.Linq; using UnityEngine; using EndlessCarChase;
// Coin gift popup ( the + on the balance button ): closes after a rewarded video and never locks the game
public static partial class T {
  static void TestCoinGiftPopupClosesAfterVideo(){
    var fake = FakeAds.Install(); int before = ECCWallet.Balance;
    ECCCoinGiftPopup.Open(); Check(ECCCoinGiftPopup.IsOpen, "coin gift popup opens");
    FindButton("ButtonWatch").onClick.Invoke(); Coroutine.Run(2.3f);
    Check(!ECCCoinGiftPopup.IsOpen && !ECCAds.IsBusy, "rewarded video watched: popup closes, input restored");
    Check(ECCWallet.Balance == before + ECCEconomyConfig.Instance.rewardedCoinGift, "offered coins paid once");
    fake.result = ECCAdResult.Skipped; before = ECCWallet.Balance;
    ECCCoinGiftPopup.Open(); FindButton("ButtonWatch").onClick.Invoke(); Coroutine.Run(2.3f);
    Check(ECCCoinGiftPopup.IsOpen && FindButton("ButtonWatch").interactable && ECCWallet.Balance == before, "skipped video: no coins, popup stays usable");
    FindButton("ButtonClose").onClick.Invoke(); Coroutine.Run(0.5f);
    Check(!ECCCoinGiftPopup.IsOpen, "close button works after a skipped video");
  }
}
