using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The «مأموریت‌های امروز» screen, on the shared popup card ( purple ribbon and target badge ): how many of today's
    /// missions are done, then one tile per mission, right to left: an icon in the difficulty's color, the difficulty
    /// chip, the mission text, its progress bar, the coin reward and the claim button
    /// ( «تکمیل نشده» / «آماده دریافت» / «دریافت شده» ).
    /// </summary>
    public class ECCDailyMissionsUI : MonoBehaviour
    {
        const float Width = 660;
        const float RowHeight = 116;
        const float RowGap = 14;

        static readonly Color Purple = ECCCarTheme.Hex(0x8E5CF7);

        static ECCDailyMissionsUI open;

        RectTransform rows;
        Text summary;
        Image summaryFill;
        float rowWidth;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            if (open) return;

            int count = ECCMissions.Today.Count;

            ECCResultCard card = ECCPopupKit.Create("CanvasDailyMissions", 70, "مأموریت‌های امروز", Purple, "Icons/target", "هر روز مأموریت‌های تازه؛ جایزه‌ها به سکه‌هات اضافه می‌شه", true, delegate { if (open) open.Close(); }, Width);

            open = card.Panel.gameObject.AddComponent<ECCDailyMissionsUI>();
            open.rowWidth = card.ContentWidth;

            // The day's progress: a caption and a bar, refreshed with the rows
            RectTransform progressRow = card.CustomRow(40);
            open.summary = ECCRuntimeUI.CreateText("Summary", progressRow, string.Empty, 20, new Vector2(0.5f, 1), new Vector2(0, -2), new Vector2(card.ContentWidth, 28), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(open.summary, ECCCarTheme.CardSubInk);
            open.summaryFill = ECCPopupKit.Bar(progressRow, new Vector2(1, 0), new Vector2(0, 6), new Vector2(card.ContentWidth, 10), 0, Purple);

            open.rows = card.CustomRow(count * (RowHeight + RowGap) - RowGap);

            open.Refresh();

            card.Finish(false);
        }

        void Refresh()
        {
            for (int index = rows.childCount - 1; index >= 0; index--) Destroy(rows.GetChild(index).gameObject);

            List<ECCDailyMissionState> missions = ECCMissions.Today;

            int done = 0;

            for (int index = 0; index < missions.Count; index++)
            {
                ECCMissionDefinition mission = ECCMissions.GetDefinition(missions[index]);

                if (mission != null && (missions[index].claimed || missions[index].progress >= mission.target)) done++;

                CreateRow(missions[index], -index * (RowHeight + RowGap));
            }

            ECCRuntimeUI.SetText(summary, ECCUIKit.PersianDigits(done) + " از " + ECCUIKit.PersianDigits(missions.Count) + " مأموریت انجام شده");

            float fraction = missions.Count > 0 ? done / (float)missions.Count : 0;
            RectTransform track = (RectTransform)summaryFill.transform.parent;
            summaryFill.rectTransform.sizeDelta = new Vector2(track.sizeDelta.x * fraction, summaryFill.rectTransform.sizeDelta.y);
            summaryFill.gameObject.SetActive(fraction > 0.001f);
        }

        static Color DifficultyColor(ECCMissionDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ECCMissionDifficulty.Easy: return ECCCarTheme.CandyGreen;
                case ECCMissionDifficulty.Medium: return ECCCarTheme.CandyOrange;
                default: return ECCCarTheme.CandyRed;
            }
        }

        static string DifficultyName(ECCMissionDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ECCMissionDifficulty.Easy: return "آسان";
                case ECCMissionDifficulty.Medium: return "متوسط";
                default: return "سخت";
            }
        }

        static string MissionIcon(ECCMissionType type)
        {
            switch (type)
            {
                case ECCMissionType.DestroyCops:
                case ECCMissionType.DestroyCopsInOneRun: return "Icons/fire";
                case ECCMissionType.CollectCash:
                case ECCMissionType.EarnInOneRun: return "dollar";
                case ECCMissionType.NearMisses: return "Icons/bolt";
                case ECCMissionType.SurviveSeconds: return "Icons/clock";
                case ECCMissionType.ReachWantedLevel: return "Icons/target";
                case ECCMissionType.CompleteLevels: return "Icons/levels";
                case ECCMissionType.ThreeStarLevels: return "Icons/star";
                case ECCMissionType.UsePowerUps: return "Icons/shield";
                default: return "Icons/play";
            }
        }

        void CreateRow(ECCDailyMissionState state, float y)
        {
            ECCMissionDefinition mission = ECCMissions.GetDefinition(state);

            if (mission == null) return;

            Vector2 right = new Vector2(1, 0.5f);
            Vector2 left = new Vector2(0, 0.5f);

            bool claimable = ECCMissions.CanClaim(state);
            Color color = state.claimed ? ECCCarTheme.CandyGray : DifficultyColor(mission.difficulty);

            Image tile = ECCPopupKit.Tile("Mission_" + state.id, rows, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(rowWidth, RowHeight), color);
            Transform row = tile.transform;

            string icon = MissionIcon(mission.type);
            ECCPopupKit.IconDisc(row, right, new Vector2(-62, 6), 72, color, icon, icon == "dollar" ? ECCCoinBalanceButton.DollarSprite : null);

            ECCPopupKit.Chip(row, "Difficulty", DifficultyName(mission.difficulty), color, new Vector2(1, 0), new Vector2(-30, 8), new Vector2(64, 24), 15);

            ECCPopupKit.TileText(row, "Description", mission.Text, 22, right, new Vector2(-112, 22), new Vector2(290, 46), ECCCarTheme.CardInk, true);

            int progress = Mathf.Min(state.progress, mission.target);

            ECCPopupKit.Bar(row, right, new Vector2(-112, -28), new Vector2(200, 14), progress / (float)Mathf.Max(1, mission.target), color);

            Text progressText = ECCUIKit.CreateNumberText("Progress", row, ECCWallet.Format(progress) + " / " + ECCWallet.Format(mission.target), 16, right, new Vector2(-320, -28), new Vector2(100, 24), ECCCarTheme.CardSubInk);
            progressText.alignment = TextAnchor.MiddleRight;
            progressText.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.InkText(progressText, ECCCarTheme.CardSubInk);

            ECCPopupKit.CoinPill(row, mission.reward, left, new Vector2(16, 24), 128);

            string label = state.claimed ? "دریافت شده" : (claimable ? "آماده دریافت" : "تکمیل نشده");

            Button claim = ECCPopupKit.ClaimButton(row, "Claim", label, claimable, state.claimed, left, new Vector2(16, -26), new Vector2(128, 48));

            if (claimable)
            {
                claim.onClick.AddListener(delegate
                {
                    // Disable first, so a second tap can't claim again before the screen refreshes
                    claim.interactable = false;

                    int paid = ECCMissions.Claim(state);

                    if (paid > 0) ECCRewardFx.ShowCoins(paid);

                    ECCMenuHub.RefreshIndicators();

                    Refresh();
                });
            }
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
