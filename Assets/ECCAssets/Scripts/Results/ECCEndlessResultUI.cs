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

            ECCResultCard card = ECCResultCard.Create("CanvasEndlessResult", 80, "تعقیب بی‌نهایت", ECCCarTheme.CandyOrange);

            ECCEndlessResultUI ui = card.Panel.gameObject.AddComponent<ECCEndlessResultUI>();
            ui.gameController = gameController;
            open = ui;

            card.Badge("Icons/crown", newRecord ? ECCCarTheme.Hex(0xFFB020) : ECCCarTheme.Hex(0x8E5CF7), newRecord);
            card.Title(newRecord ? "رکورد جدید!" : "پایان تعقیب!", newRecord ? ECCCarTheme.CandyGreenShade : ECCCarTheme.CandyRed, 46);

            card.Stars(5, wantedLevel, 46);
            card.Line("سطح تعقیب: " + ECCUIKit.PersianDigits(wantedLevel) + " از ۵", 20, 30);

            card.MoneyPill("پول این دور", score);

            if (newRecord) card.InfoBox("Icons/trophy", ECCCarTheme.Hex(0xFFB020), "رکورد تازه‌ی تو: " + ECCUIKit.PersianDigits(highScore) + " دلار", 66);
            else card.InfoBox("Icons/trophy", ECCCarTheme.CandyBlue, "رکورد: " + ECCUIKit.PersianDigits(highScore) + " دلار · " + ECCUIKit.PersianDigits(Mathf.Max(0, highScore - score + 1)) + " دلار تا رکورد جدید", 66);

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
