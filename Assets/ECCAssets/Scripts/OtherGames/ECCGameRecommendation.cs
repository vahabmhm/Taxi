using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The daily other-game recommendation: at most one popup per calendar day, one random game from the verified
    /// «سایر بازی‌ها» catalog ( ECCGameCatalog ). Only shown on the way out of a normal level victory screen, after the daily
    /// challenge popup of the day was dealt with, never at the first menu entry and never over another popup.
    /// The chosen game is saved for the day; the "shown" day is saved when the popup is actually on screen.
    /// With no verified game in the catalog it's silently skipped.
    /// </summary>
    public static class ECCGameRecommendation
    {
        /// <summary>
        /// True if the popup may be shown now
        /// </summary>
        public static bool IsDueToday
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                if (ECCDaily.IsToday(data.recommendShownDay)) return false;

                // The daily challenge popup has priority: wait until it was shown ( or can't be shown ) today
                if (ECCDailyChallenge.ShouldAutoShowPopup) return false;

                return TodayGame != null;
            }
        }

        /// <summary>
        /// Today's game, picked once per day and kept, or null when the catalog has no eligible game
        /// </summary>
        public static ECCGameEntry TodayGame
        {
            get
            {
                ECCGameCatalog catalog = ECCGameCatalog.Instance;
                List<ECCGameEntry> eligible = catalog.EligibleGames;

                if (eligible.Count == 0) return null;

                ECCSaveData data = ECCSave.Data;
                string today = ECCDaily.Today;

                if (data.recommendDay == today)
                {
                    ECCGameEntry kept = catalog.Find(data.recommendPackage);

                    if (kept != null) return kept;
                }

                // Avoid the game of the previous recommendation when there's a choice
                List<ECCGameEntry> choices = new List<ECCGameEntry>(eligible);

                if (choices.Count >= 2) choices.RemoveAll(game => game.packageName == data.recommendPackage);

                ECCGameEntry picked = choices[UnityEngine.Random.Range(0, choices.Count)];

                data.recommendDay = today;
                data.recommendPackage = picked.packageName;
                ECCSave.Save();

                return picked;
            }
        }

        /// <summary>
        /// Shows today's recommendation if it's due. onClosed runs when the popup closes. Returns false if nothing was shown
        /// </summary>
        public static bool TryShow(Action onClosed)
        {
            if (IsDueToday == false || ECCModalTracker.AnyOpen || ECCAds.IsBusy) return false;

            ECCGameEntry game = TodayGame;

            if (game == null) return false;

            ECCGameRecommendationUI.Show(game, onClosed);

            if (ECCGameRecommendationUI.IsOpen == false) return false;

            ECCSave.Data.recommendShownDay = ECCDaily.Today;
            ECCSave.Save();

            return true;
        }
    }

    /// <summary>
    /// «پیشنهاد بازی امروز»: the game's icon, name, optional description, «در کافه بازار نصب کنید» and a close (X) button
    /// </summary>
    public class ECCGameRecommendationUI : MonoBehaviour
    {
        static ECCGameRecommendationUI open;

        Action onClosed;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(ECCGameEntry game, Action onClosed)
        {
            if (open || game == null) return;

            bool hasDescription = string.IsNullOrEmpty(game.description) == false;

            RectTransform panel = ECCUIKit.CreatePopup("CanvasGameRecommendation", 88, new Vector2(600, hasDescription ? 640 : 580));

            open = panel.gameObject.AddComponent<ECCGameRecommendationUI>();
            open.onClosed = onClosed;

            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            ECCRuntimeUI.CreateText("Title", panel, "پیشنهاد بازی امروز", 36, topCenter, new Vector2(0, -50), new Vector2(440, 54), ECCRuntimeUI.AccentColor);

            Button close = ECCUIKit.CreateCloseButton(panel);
            close.onClick.AddListener(open.Close);

            ECCOtherGamesUI.CreateGameIcon(panel, game, topCenter, new Vector2(0, -100), 200);

            Text name = ECCRuntimeUI.CreateText("Name", panel, game.title, 32, topCenter, new Vector2(0, -335), new Vector2(540, 50), Color.white);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 20;
            name.resizeTextMaxSize = 32;

            if (hasDescription)
            {
                Text description = ECCRuntimeUI.CreateText("Description", panel, game.description, 22, topCenter, new Vector2(0, -400), new Vector2(540, 70), new Color(1, 1, 1, 0.75f));
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
            }

            string package = game.packageName;

            Button install = ECCRuntimeUI.CreateButton("ButtonInstall", panel, "در کافه بازار نصب کنید", 30, bottomCenter, new Vector2(0, 65), new Vector2(420, 84), ECCRuntimeUI.GoodColor);
            install.onClick.AddListener(delegate
            {
                ECCStoreLinks.OpenBazaarListing(package);

                open.Close();
            });
            ECCPulse.Set(install, true, 0.04f);
        }

        void Close()
        {
            Action callback = onClosed;

            onClosed = null;

            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);

            if (callback != null) callback();
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
