using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The ride request phone of the shift ( left middle of the screen, Persian, right to left ): up to three waiting
    /// passengers, the accepted one first, then the nearest. Every request shows the kind of passenger, the fare to
    /// expect, how far the passenger is and how long the ride is. Tapping a request accepts it ( one at a time ): the
    /// rivals leave that passenger alone, they wait longer, the guide arrow goes to them and the fare has a bonus.
    /// Tapping it again lets it go
    /// </summary>
    public class TaxiRideRequestsUI : MonoBehaviour
    {
        public const int MaxRows = 3;

        const float Width = 256;
        const float RowHeight = 60;
        const float HeaderHeight = 36;

        // The top of the phone, from the top left of the safe area: under the scoreboard of five taxis
        static readonly Vector2 Position = new Vector2(10, -430);
        const float RefreshSeconds = 0.35f;

        static readonly Color PhoneColor = new Color(0.06f, 0.05f, 0.14f, 0.86f);
        static readonly Color RowColor = new Color(1, 1, 1, 0.08f);
        static readonly Color AcceptedColor = new Color(0.2f, 0.75f, 0.35f, 0.45f);
        static readonly Color ChipColor = new Color(0.2f, 0.7f, 0.35f, 1);
        static readonly Color ChipDoneColor = new Color(1, 0.8f, 0.15f, 1);
        static readonly Color ChipOffColor = new Color(0.4f, 0.4f, 0.45f, 1);

        class Row
        {
            public Image back;
            public Image dot;
            public Text title;
            public Text info;
            public Image chip;
            public Text chipText;
            public TaxiPassenger passenger;
        }

        TaxiSession session;
        RectTransform phone;
        int rowCount = MaxRows;
        Image phoneBack;
        readonly List<Row> rows = new List<Row>();
        float refresh;

        public static TaxiRideRequestsUI Create(RectTransform parent, TaxiSession session)
        {
            // Short screens ( 4:3 tablets ) have room for one request above the ride card and the hints
            int count = RowsFor(Screen.width, Screen.height);

            RectTransform phone = ECCRuntimeUI.CreateRect("RideRequests", parent, new Vector2(0, 1), Position, new Vector2(Width, HeaderHeight + count * RowHeight + 8));

            TaxiRideRequestsUI ui = phone.gameObject.AddComponent<TaxiRideRequestsUI>();
            ui.rowCount = count;
            ui.session = session;
            ui.phone = phone;
            ui.Build();
            ui.Refresh();

            return ui;
        }

        void Build()
        {
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            phoneBack = ECCRuntimeUI.CreateImage("Phone", phone, center, Vector2.zero, phone.sizeDelta, PhoneColor);
            phoneBack.raycastTarget = false;
            ECCCarTheme.Round(phoneBack);

            // The phone's speaker slot
            Image slot = ECCRuntimeUI.CreateImage("Speaker", phone, top, new Vector2(0, -6), new Vector2(46, 5), new Color(1, 1, 1, 0.25f));
            slot.raycastTarget = false;

            Text header = ECCRuntimeUI.CreateText("Header", phone, "درخواست‌های سفر", 19, top, new Vector2(0, -22), new Vector2(Width - 20, 26), TaxiColors.PlayerYellow);
            header.raycastTarget = false;

            for (int index = 0; index < rowCount; index++) rows.Add(BuildRow(index));
        }

        /// <summary>
        /// How many requests fit: the canvas is 720 wide in portrait, so its height follows the screen's shape
        /// </summary>
        public static int RowsFor(int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= screenWidth) return MaxRows;

            float height = 720f * screenHeight / screenWidth;

            return height >= 1180 ? MaxRows : (height >= 1060 ? 2 : 1);
        }

        Row BuildRow(int index)
        {
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Row row = new Row();

            row.back = ECCRuntimeUI.CreateImage("Request" + index, phone, top, new Vector2(0, -HeaderHeight - RowHeight * 0.5f - index * RowHeight), new Vector2(Width - 16, RowHeight - 6), RowColor);
            ECCCarTheme.Round(row.back);

            Button button = row.back.gameObject.AddComponent<Button>();
            button.targetGraphic = row.back;

            int slot = index;
            button.onClick.AddListener(delegate { OnTap(slot); });

            Transform holder = row.back.transform;

            // Right to left: the passenger's color on the right, the accept chip on the left
            row.dot = ECCUIKit.CreateCircle("Kind", holder, new Vector2(1, 0.5f), new Vector2(-15, 11), 16, Color.white);
            row.dot.raycastTarget = false;

            row.title = ECCRuntimeUI.CreateText("Title", holder, string.Empty, 18, center, new Vector2(-10, 11), new Vector2(196, 26), Color.white);
            row.title.alignment = TextAnchor.MiddleRight;

            row.info = ECCRuntimeUI.CreateText("Info", holder, string.Empty, 14, center, new Vector2(30, -13), new Vector2(150, 22), new Color(1, 1, 1, 0.8f));
            row.info.alignment = TextAnchor.MiddleRight;

            row.chip = ECCRuntimeUI.CreateImage("Chip", holder, center, new Vector2(-84, -13), new Vector2(68, 22), ChipColor);
            row.chip.raycastTarget = false;
            ECCCarTheme.Round(row.chip);

            row.chipText = ECCRuntimeUI.CreateText("Text", row.chip.transform, "قبول", 14, center, Vector2.zero, new Vector2(68, 22), Color.white);

            return row;
        }

        void Update()
        {
            refresh -= Time.unscaledDeltaTime;

            if (refresh > 0) return;

            Refresh();
        }

        /// <summary>
        /// The requests to show: the accepted passenger first, then the nearest waiting ones
        /// </summary>
        public static List<TaxiPassenger> Requests(TaxiSession session, Vector3 position, int count)
        {
            List<TaxiPassenger> list = new List<TaxiPassenger>();

            if (session == null) return list;

            foreach (TaxiPassenger passenger in TaxiPassenger.All)
            {
                if (passenger && passenger.IsWaiting) list.Add(passenger);
            }

            list.Sort((a, b) =>
            {
                if (a.accepted != b.accepted) return a.accepted ? -1 : 1;

                return TaxiCity.StreetDistance(position, a.pickup.zone).CompareTo(TaxiCity.StreetDistance(position, b.pickup.zone));
            });

            if (list.Count > count) list.RemoveRange(count, list.Count - count);

            return list;
        }

        /// <summary>
        /// The fare the passenger should pay for a good ride ( a follow ride's length isn't known: a typical one )
        /// </summary>
        public static int ExpectedFare(TaxiSession session, TaxiPassenger passenger)
        {
            float distance = passenger.kind == TaxiPassengerKind.Follow ? 320 : TaxiCity.StreetDistance(passenger.pickup.zone, passenger.dropoff.zone);

            int fare = TaxiPassenger.Fare(passenger.kind, distance, 4, 1, 1, false);

            float multiplier = session ? session.FareFor(session.Player, passenger) : 1;

            // Not accepted yet: show what it pays once accepted
            if (passenger.accepted == false) multiplier *= TaxiSession.AcceptedFareBonus;

            return Mathf.Max(1, Mathf.RoundToInt(fare * multiplier));
        }

        void Refresh()
        {
            refresh = RefreshSeconds;

            TaxiDriver driver = session ? session.Player : null;

            if (driver == null || driver.car == null)
            {
                phone.gameObject.SetActive(false);
                return;
            }

            List<TaxiPassenger> requests = Requests(session, driver.car.transform.position, rowCount);

            phone.gameObject.SetActive(requests.Count > 0);

            for (int index = 0; index < rows.Count; index++)
            {
                Row row = rows[index];

                TaxiPassenger passenger = index < requests.Count ? requests[index] : null;

                row.passenger = passenger;
                row.back.gameObject.SetActive(passenger != null);

                if (passenger == null) continue;

                bool fits = session.CanAccept(passenger);

                row.dot.color = passenger.KindColor;
                row.back.color = passenger.accepted ? AcceptedColor : RowColor;

                ECCRuntimeUI.SetText(row.title, TaxiPassenger.GetName(passenger.kind) + "  ·  " + ECCUIKit.PersianDigits(ExpectedFare(session, passenger)) + " سکه");

                string ride = passenger.kind == TaxiPassengerKind.Follow ? "سفر: تعقیب" : "سفر " + ECCUIKit.PersianDigits(Mathf.RoundToInt(TaxiCity.StreetDistance(passenger.pickup.zone, passenger.dropoff.zone))) + " م";

                // A passenger going the same way as the one on board: the shared ride matters more than the ride's length
                if (TaxiSharedRide.FitsRiding(driver, passenger)) ride = "هم‌مسیر، اشتراکی";

                ECCRuntimeUI.SetText(row.info, "تا مسافر " + ECCUIKit.PersianDigits(Mathf.RoundToInt(TaxiCity.StreetDistance(driver.car.transform.position, passenger.pickup.zone))) + " م · " + ride);

                if (passenger.accepted)
                {
                    row.chip.color = ChipDoneColor;
                    ECCRuntimeUI.SetText(row.chipText, "قبول شد");
                    row.chipText.color = new Color(0.25f, 0.15f, 0);
                }
                else
                {
                    row.chip.color = fits ? ChipColor : ChipOffColor;
                    ECCRuntimeUI.SetText(row.chipText, fits ? "قبول" : "جا نمی‌شه");
                    row.chipText.color = Color.white;
                }
            }
        }

        void OnTap(int index)
        {
            if (index < 0 || index >= rows.Count || session == null) return;

            TaxiPassenger passenger = rows[index].passenger;

            if (passenger == null || passenger.IsWaiting == false) return;

            if (passenger.accepted == false && session.CanAccept(passenger) == false)
            {
                ECCToast.Show("جا نمی‌شن!", ECCRuntimeUI.BadColor, "برای این مسافرها ماشین سه‌نفره لازمه", 1.4f);
                return;
            }

            bool wasAccepted = passenger.accepted;

            if (session.ToggleAccept(passenger) == false) return;

            ECCHaptics.Vibrate(ECCHaptics.Kind.Light);

            if (wasAccepted) ECCToast.Show("سفر لغو شد", Color.white, "می‌تونی یه سفر دیگه قبول کنی", 1.2f);
            else ECCToast.Show("سفر قبول شد!", ECCRuntimeUI.GoodColor, "رقیب‌ها سراغش نمی‌رن؛ کرایه " + ECCUIKit.PersianDigits(Mathf.RoundToInt((TaxiSession.AcceptedFareBonus - 1) * 100)) + "٪ بیشتره", 1.6f);

            Refresh();
        }
    }
}
