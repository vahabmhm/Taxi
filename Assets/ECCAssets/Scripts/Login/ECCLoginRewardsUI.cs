using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// «جایزه ورود روزانه», on the shared popup card ( gold ribbon and gift badge ): a 5 x 2 calendar of the 10 days
    /// ( right to left, day 1 top right ). Each day is a tile with a colored band «روز N», its reward ( coins, or the shield
    /// pass on day 10 ) and a state chip: «آینده», «آماده دریافت» or «دریافت شده» ( with a green check ). Today glows and
    /// pulses while its reward waits. Under it: the shield pass info and the big claim button.
    /// </summary>
    public class ECCLoginRewardsUI : MonoBehaviour
    {
        const int Columns = 5;
        const float CellHeight = 158;
        const float Gap = 12;

        static readonly Color Gold = ECCCarTheme.Hex(0xF4B400);
        static readonly Color FutureBand = ECCCarTheme.Hex(0x7D8BA6);

        static ECCLoginRewardsUI open;

        RectTransform grid;
        Button claimButton;
        Text infoText;
        float cellWidth;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            if (open) return;

            ECCResultCard card = ECCPopupKit.Create("CanvasLoginRewards", 74, "جایزه ورود روزانه", Gold, "Icons/gift", "هر روز سر بزن؛ جایزه‌ها روز به روز بیشتر می‌شن!", true, delegate { if (open) open.Close(); }, 660, 96);

            open = card.Panel.gameObject.AddComponent<ECCLoginRewardsUI>();
            open.cellWidth = (card.ContentWidth - (Columns - 1) * Gap) / Columns;
            open.grid = card.CustomRow(2 * CellHeight + Gap);

            open.infoText = card.InfoBox("Icons/shield", ECCPowerUpManager.GetColor(ECCPowerUpType.Shield), string.Empty, 62);

            open.claimButton = card.WideButton("ButtonClaim", string.Empty, ECCCarTheme.CandyGreen, "Icons/gift", 92, 30);
            open.claimButton.onClick.AddListener(open.Claim);

            open.Refresh();

            card.Finish(false);
        }

        void Refresh()
        {
            for (int index = grid.childCount - 1; index >= 0; index--) Destroy(grid.GetChild(index).gameObject);

            int today = ECCLoginRewards.CurrentDay;
            bool claimable = ECCLoginRewards.CanClaimToday;

            for (int day = 1; day <= ECCLoginRewards.CycleLength; day++) CreateDay(day, today, claimable);

            claimButton.interactable = claimable;
            claimButton.targetGraphic.color = claimable ? ECCCarTheme.CandyGreen : ECCCarTheme.CandyGray;
            ECCRuntimeUI.SetText(ECCRuntimeUI.GetLabel(claimButton), claimable ? "دریافت جایزه‌ی روز " + ECCUIKit.PersianDigits(today) : "جایزه‌ی امروز رو گرفتی؛ فردا برگرد!");
            ECCPulse.Set(claimButton, claimable, 0.04f);

            if (ECCPowerUpInventory.IsShieldPassActive)
            {
                long hours = (ECCPowerUpInventory.ShieldPassSecondsLeft + 3599) / 3600;

                ECCRuntimeUI.SetText(infoText, "سپر رایگان فعاله: در هر دور یک سپر رایگان، " + ECCUIKit.PersianDigits((int)hours) + " ساعت دیگه تموم می‌شه");
            }
            else
            {
                ECCRuntimeUI.SetText(infoText, "جایزه‌ی ویژه‌ی روز دهم: سپر رایگان " + ECCUIKit.PersianDigits(Mathf.RoundToInt(ECCEconomyConfig.Instance.loginDay10PassHours)) + " ساعته (در هر دور یک سپر رایگان)");
            }
        }

        void CreateDay(int day, int today, bool claimable)
        {
            int row = (day - 1) / Columns;
            int column = (day - 1) % Columns;

            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            // Right to left: day 1 at the top right
            float x = (Columns - 1) * 0.5f * (cellWidth + Gap) - column * (cellWidth + Gap);
            float y = -row * (CellHeight + Gap);

            bool special = ECCLoginRewards.IsSpecialDay(day);
            bool claimed = ECCLoginRewards.IsDayClaimed(day);
            bool isToday = day == today;
            bool ready = isToday && claimable;

            Color band = claimed ? ECCCarTheme.CandyGray : (ready ? ECCCarTheme.CandyGreen : (special ? Gold : (isToday ? ECCCarTheme.CandyOrange : FutureBand)));

            // Today's glow behind its tile
            if (ready)
            {
                Image glow = ECCRuntimeUI.CreateImage("Glow" + day, grid, topCenter, new Vector2(x, y + 22), new Vector2(cellWidth * 2.1f, CellHeight * 1.6f), new Color(ECCCarTheme.CandyGreen.r, ECCCarTheme.CandyGreen.g, ECCCarTheme.CandyGreen.b, 0.45f));
                glow.rectTransform.pivot = topCenter;
                glow.sprite = ECCCarTheme.CardSprite("glow");
                glow.raycastTarget = false;
            }

            Image cell = ECCPopupKit.Tile("Day" + day, grid, topCenter, new Vector2(x, y), new Vector2(cellWidth, CellHeight), Color.clear);
            cell.raycastTarget = false;

            Transform face = cell.transform.Find("Face");
            if (face && special && claimed == false) face.GetComponent<Image>().color = ECCCarTheme.Hex(0xFFF1C4);
            if (face && ready) face.GetComponent<Image>().color = ECCCarTheme.Hex(0xEAF9EF);

            if (ready) ECCPulse.Set(cell, true, 0.04f);

            // The band with the day's number
            Image header = ECCRuntimeUI.CreateImage("Band", cell.transform, topCenter, new Vector2(0, -5), new Vector2(cellWidth - 10, 34), band);
            header.rectTransform.pivot = topCenter;
            ECCCarTheme.Round(header);
            header.pixelsPerUnitMultiplier = 2.6f;
            header.raycastTarget = false;

            Text number = ECCRuntimeUI.CreateText("Number", header.transform, "روز " + ECCUIKit.PersianDigits(day), 19, center, new Vector2(0, 1), new Vector2(cellWidth - 10, 32), Color.white);
            number.font = ECCCarTheme.TitleFont;
            ECCCarTheme.InkText(number, Color.white);

            if (special)
            {
                Image shield = ECCUIKit.CreateCircle("Shield", cell.transform, topCenter, new Vector2(0, -46), 58, ECCPowerUpManager.GetColor(ECCPowerUpType.Shield));
                shield.rectTransform.pivot = topCenter;
                shield.raycastTarget = false;
                ECCCarTheme.CreateIcon("Icon", shield.transform, "Icons/shield", center, Vector2.zero, 36, Color.white).rectTransform.pivot = center;

                Text reward = ECCRuntimeUI.CreateText("Reward", cell.transform, "سپر " + ECCUIKit.PersianDigits(Mathf.RoundToInt(ECCEconomyConfig.Instance.loginDay10PassHours)) + " ساعته", 15, topCenter, new Vector2(0, -108), new Vector2(cellWidth - 6, 22), ECCCarTheme.InkAmber);
                reward.font = ECCCarTheme.TitleFont;
                reward.resizeTextForBestFit = true;
                reward.resizeTextMinSize = 10;
                reward.resizeTextMaxSize = 15;
                ECCCarTheme.InkText(reward, ECCCarTheme.InkAmber);
            }
            else
            {
                Image coin = ECCUIKit.CreateCoinIcon("Coin", cell.transform, topCenter, new Vector2(0, -46), 50);
                coin.rectTransform.pivot = topCenter;

                Text reward = ECCUIKit.CreateNumberText("Reward", cell.transform, ECCWallet.Format(ECCLoginRewards.GetCoinReward(day)), 22, topCenter, new Vector2(0, -108), new Vector2(cellWidth - 6, 26), ECCCarTheme.InkAmber);
                ECCCarTheme.InkText(reward, ECCCarTheme.InkAmber);
            }

            string state;
            Color stateColor;

            if (claimed) { state = "دریافت شده"; stateColor = ECCCarTheme.CandyGray; }
            else if (ready) { state = "آماده دریافت"; stateColor = ECCCarTheme.CandyGreen; }
            else if (day > today) { state = "آینده"; stateColor = FutureBand; }
            else { state = "گذشته"; stateColor = ECCCarTheme.Hex(0xB8C0CE); }

            ECCPopupKit.Chip(cell.transform, "State", state, stateColor, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(cellWidth - 14, 24), 14);

            // A claimed day: a big green check over the reward
            if (claimed)
            {
                Image check = ECCUIKit.CreateCircle("Claimed", cell.transform, topCenter, new Vector2(0, -48), 56, ECCCarTheme.CandyGreen);
                check.rectTransform.pivot = topCenter;
                check.raycastTarget = false;
                ECCCarTheme.CreateIcon("Icon", check.transform, "Icons/check", center, Vector2.zero, 34, Color.white).rectTransform.pivot = center;
            }
        }

        void Claim()
        {
            if (ECCLoginRewards.CanClaimToday == false) return;

            int day = ECCLoginRewards.CurrentDay;

            // Disabled first, so a double tap can't claim twice before the refresh
            claimButton.interactable = false;

            int paid = ECCLoginRewards.Claim();

            if (paid > 0) ECCRewardFx.ShowCoins(paid);
            else if (paid == 0 && ECCLoginRewards.IsSpecialDay(day)) ECCToast.Show("سپر رایگان فعال شد!", ECCRuntimeUI.GoodColor, "در هر دور یک سپر رایگان داری");

            ECCMenuHub.RefreshIndicators();

            Refresh();
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
