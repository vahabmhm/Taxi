using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The taxi HUD ( built in code, Persian, right to left ):
    /// - the big brake button ( bottom right ) and the horn next to it
    /// - the guide arrow around the screen center, towards the next passenger or the destination
    /// - the ride card: the passengers on board, the distance to the destination, the hurry timer, the rating so far,
    ///   and the bar that fills while a passenger gets in
    /// - the scoreboard of the taxis ( top left ) and the police panel ( top right: wanted stars, the fine, cooling )
    /// - messages for fares, violations, the police, the VIP and stolen passengers
    /// </summary>
    public class TaxiHUD : MonoBehaviour
    {
        public static TaxiHUD Instance { get; private set; }

        static readonly Color PanelColor = new Color(0.06f, 0.05f, 0.14f, 0.78f);
        static readonly Color BrakeColor = new Color(0.9f, 0.18f, 0.16f);
        static readonly Color StarOn = new Color(1, 0.8f, 0.15f);
        static readonly Color StarOff = new Color(1, 1, 1, 0.18f);

        ECCGameController gameController;
        TaxiSession session;
        Canvas canvas;
        RectTransform safe;

        Image brakeFace;
        RectTransform brakeRoot;
        Image hornFace;

        RectTransform arrowBadge;
        RectTransform arrowChevron;
        Image arrowRing;
        Text arrowText;

        RectTransform rideCard;
        Text rideTitle;
        Text rideDetail;
        Image boardBack;
        Image boardFill;
        Text hintText;

        RectTransform board;
        readonly List<Text> boardRows = new List<Text>();
        readonly List<Image> boardDots = new List<Image>();

        RectTransform policePanel;
        readonly List<Image> stars = new List<Image>();
        Text fineText;
        Image catchFill;

        public static TaxiHUD Create(ECCGameController controller, TaxiSession session)
        {
            if (Instance) return Instance;

            GameObject holder = new GameObject("TaxiHUD");
            TaxiHUD hud = holder.AddComponent<TaxiHUD>();
            hud.gameController = controller;
            hud.session = session;
            hud.Build();

            Instance = hud;

            return hud;
        }

        void OnEnable()
        {
            TaxiSession.Delivered += OnDelivered;
            TaxiSession.PickedUp += OnPickedUp;
            TaxiSession.VipAppeared += OnVip;
            TaxiPolice.ViolationCommitted += OnViolation;
            TaxiPolice.Caught += OnCaught;
            TaxiPolice.Escaped += OnEscaped;
            TaxiPolice.StarsChanged += OnStarsChanged;
            TaxiSession.DriverRankUp += OnRankUp;
            TaxiSession.AcceptedGaveUp += OnAcceptedGaveUp;
        }

        void OnDisable()
        {
            TaxiSession.Delivered -= OnDelivered;
            TaxiSession.PickedUp -= OnPickedUp;
            TaxiSession.VipAppeared -= OnVip;
            TaxiPolice.ViolationCommitted -= OnViolation;
            TaxiPolice.Caught -= OnCaught;
            TaxiPolice.Escaped -= OnEscaped;
            TaxiPolice.StarsChanged -= OnStarsChanged;
            TaxiSession.DriverRankUp -= OnRankUp;
            TaxiSession.AcceptedGaveUp -= OnAcceptedGaveUp;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (canvas) Destroy(canvas.gameObject);
        }

        public void Hide()
        {
            if (canvas) canvas.gameObject.SetActive(false);
        }

        static string Digits(int value)
        {
            return ECCUIKit.PersianDigits(value);
        }

        // ----- Building -----

        void Build()
        {
            safe = ECCUIKit.CreateSafeCanvas("CanvasTaxiHUD", 45, out canvas);

            BuildBrake();
            BuildArrow();
            BuildRideCard();
            BuildBoard();
            BuildPolice();

            // The ride request phone ( the shift )
            if (session.rules.rideRequests) TaxiRideRequestsUI.Create(safe, session);
        }

        void BuildBrake()
        {
            Vector2 bottomRight = new Vector2(1, 0);
            Vector2 center = new Vector2(0.5f, 0.5f);

            const float size = 190;

            brakeRoot = ECCRuntimeUI.CreateRect("Brake", safe, bottomRight, new Vector2(-140, 150), new Vector2(size, size));

            ECCUIKit.CreateCircle("Shade", brakeRoot, center, new Vector2(0, -8), size, new Color(0.45f, 0.05f, 0.05f, 1));
            brakeFace = ECCUIKit.CreateCircle("Face", brakeRoot, center, Vector2.zero, size, BrakeColor);

            Image ring = ECCRuntimeUI.CreateImage("Ring", brakeRoot, center, Vector2.zero, new Vector2(size - 18, size - 18), new Color(1, 1, 1, 0.85f));
            ring.sprite = ECCCarTheme.Icon("Icons/ring");
            ring.raycastTarget = false;

            Text label = ECCRuntimeUI.CreateText("Label", brakeRoot, "ترمز", 46, center, new Vector2(0, 4), new Vector2(size, 60), Color.white);
            label.raycastTarget = false;

            TaxiHoldButton hold = brakeFace.gameObject.AddComponent<TaxiHoldButton>();
            hold.onChanged = pressed =>
            {
                if (TaxiDriver.Instance) TaxiDriver.Instance.brakeButton = pressed;
            };

            // The horn
            const float hornSize = 104;

            RectTransform horn = ECCRuntimeUI.CreateRect("Horn", safe, bottomRight, new Vector2(-330, 105), new Vector2(hornSize, hornSize));

            ECCUIKit.CreateCircle("Shade", horn, center, new Vector2(0, -6), hornSize, new Color(0.45f, 0.35f, 0.02f, 1));
            hornFace = ECCUIKit.CreateCircle("Face", horn, center, Vector2.zero, hornSize, TaxiColors.PlayerYellow);

            Text hornLabel = ECCRuntimeUI.CreateText("Label", horn, "بوق", 28, center, new Vector2(0, 2), new Vector2(hornSize, 40), new Color(0.2f, 0.12f, 0));
            hornLabel.raycastTarget = false;

            TaxiHoldButton hornButton = hornFace.gameObject.AddComponent<TaxiHoldButton>();
            hornButton.onChanged = pressed =>
            {
                if (pressed && TaxiDriver.Instance) TaxiDriver.Instance.Honk();
            };
        }

        void BuildArrow()
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            arrowBadge = ECCRuntimeUI.CreateRect("GuideArrow", canvas.transform, center, Vector2.zero, new Vector2(96, 96));

            Image disc = ECCUIKit.CreateCircle("Disc", arrowBadge, center, Vector2.zero, 90, new Color(0.05f, 0.05f, 0.12f, 0.7f));
            disc.raycastTarget = false;

            arrowRing = ECCRuntimeUI.CreateImage("Ring", arrowBadge, center, Vector2.zero, new Vector2(96, 96), Color.white);
            arrowRing.sprite = ECCCarTheme.Icon("Icons/ring");
            arrowRing.raycastTarget = false;

            Image chevron = ECCRuntimeUI.CreateImage("Chevron", arrowBadge, center, Vector2.zero, new Vector2(52, 52), Color.white);
            chevron.sprite = ECCCarTheme.Icon("Icons/chevron");
            chevron.raycastTarget = false;
            arrowChevron = chevron.rectTransform;

            arrowText = ECCRuntimeUI.CreateText("Distance", arrowBadge, string.Empty, 20, center, new Vector2(0, -66), new Vector2(170, 28), Color.white);
            arrowText.raycastTarget = false;
        }

        void BuildRideCard()
        {
            Vector2 bottom = new Vector2(0.5f, 0);
            Vector2 center = new Vector2(0.5f, 0.5f);

            rideCard = ECCRuntimeUI.CreateRect("RideCard", safe, bottom, new Vector2(-60, 315), new Vector2(460, 104));

            Image back = ECCRuntimeUI.CreateImage("Back", rideCard, center, Vector2.zero, new Vector2(460, 104), PanelColor);
            back.raycastTarget = false;
            ECCCarTheme.Round(back);

            rideTitle = ECCRuntimeUI.CreateText("Title", rideCard, string.Empty, 26, center, new Vector2(0, 24), new Vector2(440, 36), Color.white);
            rideDetail = ECCRuntimeUI.CreateText("Detail", rideCard, string.Empty, 21, center, new Vector2(0, -12), new Vector2(440, 30), new Color(1, 1, 1, 0.85f));

            boardBack = ECCRuntimeUI.CreateImage("BoardBar", rideCard, center, new Vector2(0, -40), new Vector2(380, 10), new Color(1, 1, 1, 0.15f));
            boardFill = ECCRuntimeUI.CreateImage("Fill", boardBack.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(380, 10), ECCRuntimeUI.GoodColor);
            boardFill.rectTransform.pivot = new Vector2(0, 0.5f);

            hintText = ECCRuntimeUI.CreateText("Hint", safe, string.Empty, 24, bottom, new Vector2(-60, 390), new Vector2(560, 34), Color.white);
            hintText.raycastTarget = false;

            Shadow shadow = hintText.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(0, -2);
        }

        void BuildBoard()
        {
            Vector2 topLeft = new Vector2(0, 1);

            board = ECCRuntimeUI.CreateRect("Scoreboard", safe, topLeft, new Vector2(130, -190), new Vector2(230, 40));

            ECCRuntimeUI.CreateText("Title", board, "مسافرها", 20, new Vector2(0.5f, 1), new Vector2(0, 0), new Vector2(230, 28), new Color(1, 1, 1, 0.8f));
        }

        void EnsureRows(int count)
        {
            Vector2 top = new Vector2(0.5f, 1);

            while (boardRows.Count < count)
            {
                int row = boardRows.Count;

                Image back = ECCRuntimeUI.CreateImage("Row" + row, board, top, new Vector2(0, -36 - row * 38), new Vector2(230, 34), PanelColor);
                back.raycastTarget = false;
                ECCCarTheme.Round(back);

                Image dot = ECCUIKit.CreateCircle("Dot", back.transform, new Vector2(1, 0.5f), new Vector2(-20, 0), 20, Color.white);
                boardDots.Add(dot);

                Text text = ECCRuntimeUI.CreateText("Text", back.transform, string.Empty, 20, new Vector2(0.5f, 0.5f), new Vector2(-12, 0), new Vector2(190, 30), Color.white);
                boardRows.Add(text);
            }
        }

        void BuildPolice()
        {
            Vector2 topRight = new Vector2(1, 1);
            Vector2 top = new Vector2(0.5f, 1);

            policePanel = ECCRuntimeUI.CreateRect("Police", safe, topRight, new Vector2(-130, -235), new Vector2(230, 96));

            Image back = ECCRuntimeUI.CreateImage("Back", policePanel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 96), new Color(0.25f, 0.03f, 0.05f, 0.8f));
            back.raycastTarget = false;
            ECCCarTheme.Round(back);

            for (int index = 0; index < 5; index++)
            {
                Image star = ECCCarTheme.CreateIcon("Star" + index, policePanel, "Icons/star", top, new Vector2((index - 2) * 38, -24), 32, StarOff);
                stars.Add(star);
            }

            fineText = ECCRuntimeUI.CreateText("Fine", policePanel, string.Empty, 18, top, new Vector2(0, -58), new Vector2(224, 28), Color.white);

            Image catchBack = ECCRuntimeUI.CreateImage("CatchBar", policePanel, top, new Vector2(0, -84), new Vector2(200, 8), new Color(1, 1, 1, 0.15f));
            catchFill = ECCRuntimeUI.CreateImage("Fill", catchBack.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(200, 8), new Color(0.3f, 0.6f, 1));
            catchFill.rectTransform.pivot = new Vector2(0, 0.5f);
        }

        // ----- Updating -----

        void Update()
        {
            if (gameController == null || session == null) return;

            bool playing = gameController.gameStarted && gameController.isGameOver == false;

            if (canvas) canvas.gameObject.SetActive(playing);

            if (playing == false) return;

            TaxiDriver driver = TaxiDriver.Instance;

            UpdateBrake(driver);
            UpdateArrow(driver);
            UpdateRide(driver);
            UpdateBoard();
            UpdatePolice();
        }

        void UpdateBrake(TaxiDriver driver)
        {
            bool braking = driver && driver.IsBraking;

            brakeRoot.localScale = Vector3.one * (braking ? 0.92f : 1);
            brakeFace.color = braking ? Color.Lerp(BrakeColor, Color.white, 0.25f) : BrakeColor;

            // The brake button pulses when the taxi is near a passenger's ring
            bool near = driver && driver.IsBraking == false && NearRing(driver);

            if (near) brakeRoot.localScale = Vector3.one * (1 + Mathf.Abs(Mathf.Sin(Time.time * 7)) * 0.08f);

            hornFace.color = driver && driver.CanHonk ? TaxiColors.PlayerYellow : new Color(0.6f, 0.5f, 0.2f);
        }

        static bool NearRing(TaxiDriver driver)
        {
            Vector3 position = driver.car.transform.position;

            if (driver.FreeSeats > 0)
            {
                foreach (TaxiPassenger passenger in TaxiPassenger.All)
                {
                    if (passenger && passenger.IsWaiting && passenger.Seats <= driver.FreeSeats && ECCRuntimeVisuals.FlatDistance(position, passenger.pickup.zone) < 14) return true;
                }
            }

            foreach (TaxiPassenger passenger in driver.riding)
            {
                if (passenger && passenger.dropoffHidden == false && ECCRuntimeVisuals.FlatDistance(position, passenger.dropoff.zone) < 14) return true;
            }

            return false;
        }

        void UpdateArrow(TaxiDriver driver)
        {
            Vector3 target;
            Color color;
            bool destination;

            Camera view = Camera.main;

            if (driver == null || view == null || driver.GetGuide(out target, out color, out destination) == false)
            {
                arrowBadge.gameObject.SetActive(false);
                return;
            }

            float distance = ECCRuntimeVisuals.FlatDistance(driver.car.transform.position, target);

            // Close by the ring itself is easy to see
            if (distance < 12)
            {
                arrowBadge.gameObject.SetActive(false);
                return;
            }

            arrowBadge.gameObject.SetActive(true);

            Vector3 from = view.WorldToScreenPoint(driver.car.transform.position);
            Vector3 to = view.WorldToScreenPoint(target);

            Vector2 direction = new Vector2(to.x - from.x, to.y - from.y);

            if (to.z < 0) direction = -direction;

            if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;

            direction.Normalize();

            float radius = 210 + Mathf.Sin(Time.time * 6) * 8;

            arrowBadge.anchoredPosition = direction * radius;
            arrowChevron.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90);

            arrowRing.color = color;

            bool following = destination && driver.riding.Exists(passenger => passenger && passenger.dropoffHidden);

            ECCRuntimeUI.SetText(arrowText, (following ? "ماشین " : (destination ? "مقصد " : "مسافر ")) + Digits(Mathf.RoundToInt(distance)) + " متر");
        }

        void UpdateRide(TaxiDriver driver)
        {
            string hint = string.Empty;

            if (driver == null)
            {
                rideCard.gameObject.SetActive(false);
                ECCRuntimeUI.SetText(hintText, hint);
                return;
            }

            // The bar fills while the taxi stands in a ring
            float fill = 0;

            TaxiPassenger inReach = driver.FreeSeats > 0 ? driver.WaitingPassengerInReach() : null;

            if (inReach && inReach.Seats <= driver.FreeSeats) fill = Mathf.Clamp01(driver.StoppedTime / TaxiCab.PickupHold);

            foreach (TaxiPassenger passenger in driver.riding)
            {
                if (passenger && passenger.dropoffHidden == false && ECCRuntimeVisuals.FlatDistance(driver.car.transform.position, passenger.dropoff.zone) <= driver.ZoneReach) fill = Mathf.Max(fill, Mathf.Clamp01(driver.StoppedTime / TaxiCab.DropHold));
            }

            if (driver.IsBoarding) fill = 1;

            boardBack.gameObject.SetActive(fill > 0);
            boardFill.rectTransform.sizeDelta = new Vector2(380 * fill, 10);

            if (driver.riding.Count == 0)
            {
                rideCard.gameObject.SetActive(driver.IsBoarding || fill > 0);

                ECCRuntimeUI.SetText(rideTitle, driver.IsBoarding ? "مسافر داره سوار می‌شه..." : "وایسا تا مسافر سوار بشه");
                ECCRuntimeUI.SetText(rideDetail, string.Empty);

                if (inReach && inReach.Seats > driver.FreeSeats) hint = "این مسافرها جا نمی‌شن؛ ماشین بزرگ‌تر لازمه";
                else if (inReach && driver.IsBraking == false && driver.car.CurrentSpeed > TaxiCab.StopSpeed) hint = "ترمز کن و توی دایره وایسا";
                else if (inReach == null && driver.IsBraking && driver.car.CurrentSpeed < TaxiCab.StopSpeed && NearRing(driver)) hint = "داخل دایره‌ی رنگی وایسا";

                ECCRuntimeUI.SetText(hintText, hint);

                return;
            }

            rideCard.gameObject.SetActive(true);

            TaxiPassenger first = driver.riding[0];

            if (first == null) return;

            string title = TaxiPassenger.GetName(first.kind);

            if (driver.riding.Count > 1) title += " و " + Digits(driver.riding.Count - 1) + " مسافر دیگه";

            int rating = TaxiPassenger.Rate(first.kind, first.bumps, first.IsLate, first.pickupQuality, 2);

            title += "  ·  " + Digits(rating) + " ستاره";

            ECCRuntimeUI.SetText(rideTitle, title);

            string detail = "مقصد: " + Digits(Mathf.RoundToInt(ECCRuntimeVisuals.FlatDistance(driver.car.transform.position, first.dropoff.zone))) + " متر";

            TaxiFollowRide follow = first.dropoffHidden ? TaxiFollowRide.For(first) : null;

            if (follow) detail = "فاصله تا ماشین: " + Digits(Mathf.RoundToInt(follow.Distance)) + " متر";

            if (first.rideLimit > 0) detail += first.IsLate ? "  ·  دیر شد!" : "  ·  وقت: " + Digits(Mathf.CeilToInt(first.RideTimeLeft)) + " ثانیه";

            if (driver.FreeSeats > 0) detail += "  ·  جای خالی: " + Digits(driver.FreeSeats);

            ECCRuntimeUI.SetText(rideDetail, detail);

            if (follow && follow.IsLeading)
            {
                float gap = follow.Distance;

                if (follow.tracker.IsTooClose(gap)) hint = "زیادی نزدیکی! یه کم فاصله بگیر";
                else if (follow.tracker.FarTime > 0) hint = "داری گمش می‌کنی! " + Digits(Mathf.CeilToInt(follow.tracker.LoseTimeLeft)) + " ثانیه";
                else if (follow.tracker.IsTooFar(gap)) hint = "داره دور می‌شه؛ تندتر برو";
                else hint = "خوبه؛ همین فاصله رو نگه دار";
            }
            else if (first.kind == TaxiPassengerKind.Grumpy) hint = "مسافر بدخلقه؛ آروم رانندگی کن";
            else if (first.IsLate == false && first.rideLimit > 0 && first.RideTimeLeft < 10) hint = "عجله کن، مسافر دیرش شده!";

            ECCRuntimeUI.SetText(hintText, hint);
        }

        readonly List<TaxiCab> sorted = new List<TaxiCab>();

        void UpdateBoard()
        {
            sorted.Clear();

            if (session.Player) sorted.Add(session.Player);

            foreach (TaxiRival rival in session.Rivals)
            {
                if (rival) sorted.Add(rival);
            }

            board.gameObject.SetActive(sorted.Count > 1);

            if (sorted.Count <= 1) return;

            // The player first on a tie
            sorted.Sort((a, b) => b.delivered != a.delivered ? b.delivered.CompareTo(a.delivered) : (a.IsPlayer ? -1 : (b.IsPlayer ? 1 : 0)));

            EnsureRows(sorted.Count);

            for (int index = 0; index < boardRows.Count; index++)
            {
                bool used = index < sorted.Count;

                boardRows[index].transform.parent.gameObject.SetActive(used);

                if (used == false) continue;

                TaxiCab cab = sorted[index];

                boardDots[index].color = cab.color;

                ECCRuntimeUI.SetText(boardRows[index], cab.displayName + ": " + Digits(cab.delivered));

                boardRows[index].color = cab.IsPlayer ? TaxiColors.PlayerYellow : Color.white;
            }
        }

        void UpdatePolice()
        {
            TaxiPolice police = TaxiPolice.Instance;

            bool show = police && (police.Stars > 0 || police.PendingFine > 0);

            policePanel.gameObject.SetActive(show);

            if (show == false) return;

            int lit = police.Stars;

            // Cooling: the stars blink
            bool blink = police.IsCooling && Mathf.Repeat(Time.time * 3, 1) < 0.5f;

            for (int index = 0; index < stars.Count; index++)
            {
                stars[index].color = index < lit && blink == false ? StarOn : StarOff;
            }

            string text = "جریمه: " + Digits(police.PendingFine);

            if (police.IsCooling) text += "  ·  داری گم می‌شی";
            else if (police.ChasingCops > 0) text += "  ·  پلیس دنبالته!";

            ECCRuntimeUI.SetText(fineText, text);

            catchFill.rectTransform.sizeDelta = new Vector2(200 * police.CatchProgress, 8);
            catchFill.color = Color.Lerp(new Color(0.3f, 0.6f, 1), new Color(1, 0.2f, 0.2f), Mathf.PingPong(Time.time * 4, 1));
        }

        // ----- Messages -----

        void OnPickedUp(TaxiCab cab, TaxiPassenger passenger)
        {
            if (cab.IsPlayer)
            {
                string quality = passenger.pickupQuality == 2 ? "ترمز عالی!" : (passenger.pickupQuality == 1 ? "خوب بود" : "سوار شد");

                ECCToast.Show(quality, passenger.pickupQuality == 2 ? StarOn : Color.white, TaxiPassenger.GetName(passenger.kind) + " سوار شد؛ دنبال فلش برو", 1.4f);
            }
            else if (passenger.kind == TaxiPassengerKind.Vip)
            {
                ECCHintBanner.Show("Icons/crown", ECCRuntimeUI.BadColor, cab.displayName + " مسافر VIP رو برد!", "دفعه‌ی بعد زودتر برس", 2.4f, ECCHintBanner.Kind.Urgent);
            }
        }

        void OnDelivered(TaxiCab cab, TaxiPassenger passenger, int fare, int rating, int dropQuality)
        {
            if (cab.IsPlayer == false) return;

            string detail = "امتیاز مسافر: " + Digits(rating) + " از ۵";

            if (dropQuality == 2) detail += "  ·  توقف عالی!";
            if (passenger.accepted) detail += "  ·  پاداش سفر قبول‌شده";
            if (passenger.kind == TaxiPassengerKind.Follow) detail += passenger.followLost ? "  ·  ماشین گم شد" : "  ·  تعقیب موفق!";
            if (session.StreakMultiplier > 1) detail += "  ·  کمبو ×" + Digits(session.StreakMultiplier);

            ECCToast.Show("+" + Digits(fare) + " سکه", rating >= 3 ? ECCRuntimeUI.GoodColor : ECCRuntimeUI.BadColor, detail, 1.8f);

            ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);
        }

        void OnRankUp(int rank)
        {
            TaxiDriverRank.Rank reached = TaxiDriverRank.Ranks[Mathf.Clamp(rank, 0, TaxiDriverRank.Ranks.Length - 1)];

            ECCHintBanner.Show("Icons/crown", TaxiColors.Gold, "رتبه‌ی جدید: " + reached.title, reached.perk, 3, ECCHintBanner.Kind.Urgent);

            ECCRewardFx.Confetti();
        }

        void OnAcceptedGaveUp(TaxiPassenger passenger)
        {
            ECCHintBanner.Show("Icons/clock", ECCRuntimeUI.BadColor, "مسافرت منتظر نموند!", "سفری که قبول کردی رو زودتر برسون", 2, ECCHintBanner.Kind.Minor);
        }

        void OnVip(TaxiPassenger vip)
        {
            ECCHintBanner.Show("Icons/crown", TaxiColors.Vip, "مسافر VIP منتظره!", "چهار برابر کرایه می‌ده؛ تاکسی‌های رقیب هم دنبالشن", 2.6f, ECCHintBanner.Kind.Urgent);
        }

        void OnViolation(TaxiViolation violation, int fine, int stars)
        {
            ECCToast.Show(TaxiPolice.GetName(violation) + "!", ECCRuntimeUI.BadColor, "جریمه: " + Digits(fine) + " سکه", 1.6f);
        }

        void OnStarsChanged(int stars)
        {
            if (stars <= 0) return;

            TaxiPolice police = TaxiPolice.Instance;

            if (police == null || police.IsCooling) return;

            string detail;

            switch (stars)
            {
                case 1: detail = "یک ماشین پلیس دنبالته؛ ازش دور شو یا وایسا جریمه‌ت رو بده"; break;
                case 2: detail = "دو ماشین پلیس دنبالتن"; break;
                case 3: detail = "سه ماشین پلیس و میخ روی جاده!"; break;
                case 4: detail = "پلیس‌های بیشتر و راه‌بند!"; break;
                default: detail = "همه‌ی پلیس‌ها و هلیکوپتر دنبالتن!"; break;
            }

            ECCHintBanner.Show("Icons/star", ECCRuntimeUI.BadColor, "تحت تعقیب: " + Digits(stars) + " ستاره", detail, 2.2f, ECCHintBanner.Kind.Urgent);
        }

        void OnCaught(int paid, int stars)
        {
            ECCHintBanner.Show("Icons/lock", ECCRuntimeUI.BadColor, "پلیس گرفتت!", Digits(paid) + " سکه جریمه پرداخت شد", 2.4f, ECCHintBanner.Kind.Urgent);
        }

        void OnEscaped()
        {
            ECCHintBanner.Show("Icons/check", ECCRuntimeUI.GoodColor, "از دست پلیس در رفتی!", "جریمه‌ها بخشیده شد", 2, ECCHintBanner.Kind.Urgent);
        }
    }

    /// <summary>
    /// A button that reports while it is held down ( the brake ), not only clicks
    /// </summary>
    public class TaxiHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public System.Action<bool> onChanged;

        bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            Set(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Set(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Set(false);
        }

        void OnDisable()
        {
            Set(false);
        }

        void Set(bool value)
        {
            if (pressed == value) return;

            pressed = value;

            if (onChanged != null) onChanged(value);
        }
    }
}
