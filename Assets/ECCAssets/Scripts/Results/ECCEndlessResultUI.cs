using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The end of an endless chase, on the shared result card ( ECCResultCard ): a crown badge ( with light rays and
    /// confetti for a new record ), the wanted level reached as stars, the money earned ( already added to the wallet ),
    /// the record ( or how much was missing to beat it ), a pulsing «دوباره» and «منو».
    /// The continue offer ( video or coins ) already happened when the car was wrecked
    /// </summary>
    public class ECCEndlessResultUI : MonoBehaviour
    {
        static ECCEndlessResultUI open;

        ECCGameController gameController;
        bool leaving = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(ECCGameController gameController, int score, int highScore, bool newRecord, int wantedLevel)
        {
            if (open) return;

            ECCResultCard card = ECCResultCard.Create("CanvasEndlessResult", 80, "شیفت بی‌نهایت", ECCCarTheme.CandyOrange);

            ECCEndlessResultUI ui = card.Panel.gameObject.AddComponent<ECCEndlessResultUI>();
            ui.gameController = gameController;
            open = ui;

            card.Badge("Icons/crown", newRecord ? ECCCarTheme.Hex(0xFFB020) : ECCCarTheme.Hex(0x8E5CF7), newRecord);
            card.Title(newRecord ? "رکورد جدید!" : "پایان شیفت!", newRecord ? ECCCarTheme.CandyGreenShade : ECCCarTheme.CandyRed, 46);

            // Taxi: the passengers' average rating, the passengers and the fines of the shift
            TaxiSession session = TaxiSession.Instance;
            TaxiPolice police = TaxiPolice.Instance;

            int delivered = session ? session.PlayerDeliveries : 0;
            int rating = session ? Mathf.RoundToInt(session.AverageRating) : 0;

            card.Stars(5, rating, 46);
            card.Line("مسافر: " + ECCUIKit.PersianDigits(delivered) + " · توقف عالی: " + ECCUIKit.PersianDigits(session ? session.PerfectStops : 0), 22, 32);

            // The driver's rank, kept across the shifts
            if (TaxiDriverRank.Rides > 0) card.Line("رتبه: " + TaxiDriverRank.CurrentRank.title + " · امتیاز راننده " + TaxiDriverRank.FormatRating(TaxiDriverRank.Rating), 20, 30, ECCCarTheme.CandyOrangeShade);

            if (police && police.TotalFined > 0) card.Line("جریمه‌ی پلیس: " + ECCUIKit.PersianDigits(police.TotalFined) + " سکه", 20, 30, ECCCarTheme.CandyRed);

            card.MoneyPill("درآمد این شیفت", score);

            if (newRecord) card.InfoBox("Icons/trophy", ECCCarTheme.Hex(0xFFB020), "رکورد تازه‌ی تو: " + ECCUIKit.PersianDigits(highScore) + " سکه", 66);
            else card.InfoBox("Icons/trophy", ECCCarTheme.CandyBlue, "رکورد: " + ECCUIKit.PersianDigits(highScore) + " سکه · " + ECCUIKit.PersianDigits(Mathf.Max(0, highScore - score + 1)) + " سکه تا رکورد جدید", 66);

            card.Space(4);

            Button retry = card.WideButton("ButtonRetry", "دوباره", ECCCarTheme.CandyOrange, "Icons/play", 92, 34);
            retry.onClick.AddListener(delegate { ui.Leave(true); });
            ECCPulse.Set(retry, true, 0.04f);

            card.ButtonRow(72, new ECCResultCard.ButtonSpec("ButtonMenu", "منو", ECCCarTheme.CandyGray, "Icons/home", delegate { ui.Leave(false); }));

            card.Finish();

            if (newRecord) ECCRewardFx.Confetti();

            if (score > 0) ECCRewardFx.ShowCoins(score);
        }

        void Leave(bool retry)
        {
            if (leaving) return;

            leaving = true;

            ECCGameController controller = gameController;

            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);

            if (controller) controller.ReloadScene(retry);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
