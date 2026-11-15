using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The messages shown during a run ( danger, crashes, near-miss bonuses... ). A wrecked police car has no message,
    /// only its bonus dollars flying to the balance ( ECCComboSystem ). They slide down in the same white
    /// card as the level hints ( ECCHintBanner ) at the top of the screen, away from the player's health bar.
    /// After a bad event a second card tells the player what to do about it, and the power-up that helps pulses with a
    /// glowing halo on the right ( ECCPowerUpBar.Highlight ). The same advice isn't repeated for a while.
    /// </summary>
    public static class ECCRunMessages
    {
        // The same advice is given again only after this many seconds
        public const float AdviceCooldown = 25;

        // How long the power-up buttons pulse after an advice
        public const float HighlightTime = 6;

        static readonly Color DangerColor = new Color(0.93f, 0.24f, 0.2f, 1);
        static readonly Color AdviceColor = new Color(0.2f, 0.66f, 0.3f, 1);

        static readonly Dictionary<string, float> lastAdvice = new Dictionary<string, float>();

        /// <summary>
        /// The player's car dropped into the red: warn at once, then advise repairing it
        /// </summary>
        public static void CarInDanger()
        {
            ECCHintBanner.Show("Icons/heart", DangerColor, "هشدار! ماشینت داره نابود میشه", "مراقب ضربه‌ها باش", 1.8f, ECCHintBanner.Kind.Urgent);

            Advise("repair", "تعمیرش کن!", "دکمه‌ی «تعمیر فوری» سمت راست رو بزن، یا با «سپر» از ضربه‌ها در امان بمون",
                ECCPowerUpType.Repair, ECCPowerUpType.Shield);
        }

        public static void Crash()
        {
            ECCHintBanner.Show("Icons/fire", DangerColor, "تصادف!", "ماشین‌های جاده بهت آسیب می‌زنن", 1.4f);

            AdviseShield();
        }

        public static void FlatTire()
        {
            ECCHintBanner.Show("Icons/target", DangerColor, "پنچر شدی!", "از روی میخ‌ها رد نشو", 1.4f);

            AdviseShield();
        }

        public static void Roadblock()
        {
            ECCHintBanner.Show("Icons/lock", DangerColor, "راه‌بندان!", "پلیس جاده رو بسته؛ از کنارش رد شو", 1.8f);

            AdviseShield();
        }

        public static void HelicopterSpikes()
        {
            ECCHintBanner.Show("Icons/target", DangerColor, "هلیکوپتر میخ انداخت!", "از روی میخ‌ها رد نشو", 1.4f);

            AdviseShield();
        }

        public static void NearMiss(int bonus)
        {
            ECCHintBanner.Show("Icons/bolt", ECCCarTheme.CandyOrange, "از بیخ گوش!", "+" + ECCUIKit.PersianDigits(bonus) + " سکه جایزه", 1.1f, ECCHintBanner.Kind.Minor);
        }

        static void AdviseShield()
        {
            Advise("shield", "با «سپر» آسیب نبین!", "دکمه‌ی «سپر» سمت راست چند ثانیه ماشینت رو از هر ضربه‌ای در امان نگه می‌داره", ECCPowerUpType.Shield);
        }

        /// <summary>
        /// Queues an advice card ( once per AdviceCooldown ); when it appears, the helpful power-up buttons pulse
        /// </summary>
        static void Advise(string key, string title, string text, params ECCPowerUpType[] powerUps)
        {
            // Advice about buttons the player doesn't have would only confuse
            bool anyButton = false;

            foreach (ECCPowerUpType type in powerUps) anyButton |= ECCPowerUpBar.HasButton(type);

            if (anyButton == false) return;

            float last;

            if (lastAdvice.TryGetValue(key, out last) && Time.unscaledTime - last < AdviceCooldown) return;

            lastAdvice[key] = Time.unscaledTime;

            ECCHintBanner.Show("Icons/upgrade", AdviceColor, title, text, 3.2f, ECCHintBanner.Kind.Normal, delegate
            {
                ECCPowerUpBar.Highlight(HighlightTime, powerUps);
            });
        }

        /// <summary>
        /// Forgets the advice already given ( a new run )
        /// </summary>
        public static void ResetRun()
        {
            lastAdvice.Clear();
        }
    }
}
