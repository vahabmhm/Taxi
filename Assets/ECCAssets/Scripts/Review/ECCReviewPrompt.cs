using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The one-time review prompt. It can appear once per save, after several different NORMAL levels were won
    /// ( ECCEconomyConfig.reviewAfterWins, default 3; not the daily challenge, not skipped levels ), and only when the player
    /// taps «مرحله بعد» on a victory screen. It stays away when the daily recommendation or an interstitial uses that
    /// transition, or another popup is open; it then waits for a later Next Level tap.
    /// reviewPromptShown is saved when the popup is actually displayed, whatever the player chooses.
    /// ( Deleting the app data or reinstalling resets it: the save is local. )
    /// </summary>
    public static class ECCReviewPrompt
    {
        /// <summary>
        /// Counts a normal level win ( each level once )
        /// </summary>
        public static void RecordNormalWin(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;

            ECCSaveData data = ECCSave.Data;

            if (data.reviewPromptShown || data.reviewWins.Contains(levelId)) return;

            data.reviewWins.Add(levelId);
            ECCSave.Save();
        }

        public static bool IsEligible
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                return data.reviewPromptShown == false && data.reviewWins.Count >= Mathf.Max(1, ECCEconomyConfig.Instance.reviewAfterWins);
            }
        }

        /// <summary>
        /// Shows the prompt on a Next Level transition if everything allows it. Returns false if nothing was shown
        /// </summary>
        public static bool TryShow(Action onClosed)
        {
            if (IsEligible == false || ECCAds.IsInterstitialScheduled || ECCAds.IsBusy || ECCModalTracker.AnyOpen) return false;

            ECCReviewPromptUI.Show(BazaarReviewManager.Instance, onClosed);

            if (ECCReviewPromptUI.IsOpen == false) return false;

            ECCSave.Data.reviewPromptShown = true;
            ECCSave.Save();

            return true;
        }
    }

    /// <summary>
    /// The small review popup. Its text follows the store selected in BazaarReviewManager
    /// </summary>
    public class ECCReviewPromptUI : MonoBehaviour
    {
        static ECCReviewPromptUI open;

        Action onClosed;
        bool closing = false;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show(BazaarReviewManager manager, Action onClosed)
        {
            if (open || manager == null) return;

            bool myket = manager.store == ECCReviewStore.Myket;

            RectTransform panel = ECCUIKit.CreatePopup("CanvasReviewPrompt", 88, new Vector2(600, 460));

            ECCReviewPromptUI ui = panel.gameObject.AddComponent<ECCReviewPromptUI>();
            ui.onClosed = onClosed;
            open = ui;

            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            ECCRuntimeUI.CreateText("Title", panel, myket ? "نظرت رو در مایکت ثبت کن" : "نظرت رو در کافه‌بازار ثبت کن", 32, topCenter, new Vector2(0, -55), new Vector2(480, 50), ECCRuntimeUI.AccentColor);

            ECCRuntimeUI.CreateStarRow(panel, topCenter, new Vector2(0, -130), 5, 5, 34);

            ECCRuntimeUI.CreateText("Message", panel, "از بازی لذت می‌بری؟ با ثبت نظرت به ما کمک کن!", 24, topCenter, new Vector2(0, -195), new Vector2(560, 40), Color.white);

            Button close = ECCUIKit.CreateCloseButton(panel);
            close.onClick.AddListener(ui.Close);

            Button review = ECCRuntimeUI.CreateButton("ButtonReview", panel, myket ? "ثبت نظر در مایکت" : "ثبت نظر در کافه‌بازار", 28, bottomCenter, new Vector2(0, 140), new Vector2(400, 80), ECCRuntimeUI.GoodColor);
            review.onClick.AddListener(delegate
            {
                if (ui.closing) return;

                manager.OpenReview();

                ui.Close();
            });
            ECCPulse.Set(review, true, 0.04f);

            Button later = ECCRuntimeUI.CreateButton("ButtonLater", panel, "بعداً", 26, bottomCenter, new Vector2(0, 50), new Vector2(220, 64), ECCRuntimeUI.ButtonColor);
            later.onClick.AddListener(ui.Close);
        }

        void Close()
        {
            if (closing) return;

            closing = true;

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
