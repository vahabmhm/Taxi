using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Modes;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// The HUD of the levels mode: the level name, a round goal gauge ( police wrecked, money, distance, survive time, boss health ),
    /// a round countdown gauge for levels with a time limit, and on escape levels a big guide arrow towards the destination
    /// with a warning when the player drives away from it
    /// </summary>
    public class ECCLevelHUD : MonoBehaviour
    {
        ECCGameController gameController;
        ECCLevelMode levelMode;

        Canvas canvas;
        bool isHidden = false;

        // A round gauge: a dark disc, a ring that fills with the progress, an icon, a big value and a small line under it
        class Gauge
        {
            public RectTransform root;
            public Image fill;
            public Image icon;
            public Text value;
            public Text subValue;
            public float punch;
        }

        Gauge goalGauge;
        Gauge timerGauge;

        // Two chips under the goal gauge, like «منفجر شده ۷» and «مانده ۹»
        Text doneChip;
        Text leftChip;

        // The last values shown, to react when they change
        int lastCops = 0;
        int lastCash = 0;
        int lastCheckpoints = 0;
        int lastSecond = -1;

        // The guide arrow of escape levels
        RectTransform arrowBadge;
        RectTransform arrowChevron;
        RectTransform arrowChevronBack;
        Image arrowRing;
        Image arrowDisc;
        Text arrowDistance;

        // The "you are driving away" warning
        RectTransform warningBanner;
        Text warningText;
        float lastDistance = -1;
        float distanceRate = 0;

        // The destination the guide points at; heists and checkpoints move it
        Object guidedPoint;
        float awayTime = 0;
        float closerTime = 0;
        bool isWarning = false;

        static readonly Color GuideColor = new Color(0.2f, 1, 0.4f, 1);
        static readonly Color CopColor = new Color(1, 0.45f, 0.1f, 1);
        static readonly Color CashColor = new Color(0.35f, 0.95f, 0.45f, 1);
        static readonly Color WarnColor = new Color(1, 0.8f, 0.1f, 1);

        const float GaugeSize = 150;
        const float ArrowRadius = 190;

        public static ECCLevelHUD Create(ECCGameController gameController, ECCLevelMode levelMode)
        {
            Canvas canvas = ECCRuntimeUI.CreateCanvas("CanvasLevelHUD", 50);

            ECCLevelHUD hud = canvas.gameObject.AddComponent<ECCLevelHUD>();
            hud.gameController = gameController;
            hud.levelMode = levelMode;
            hud.canvas = canvas;
            hud.Build();

            return hud;
        }

        void Build()
        {
            Vector2 topCenter = new Vector2(0.5f, 1);

            ECCRuntimeUI.CreateText("Title", canvas.transform, levelMode.HudTitle, 22, topCenter, new Vector2(0, -100), new Vector2(600, 30), new Color(1, 1, 1, 0.85f));

            bool hasTimer = levelMode.TimeLeft < float.PositiveInfinity;

            // Right to left: the goal on the right, the clock on the left
            float goalX = hasTimer ? 95 : 0;

            goalGauge = CreateGauge("GoalGauge", new Vector2(goalX, -205), GoalIcon(), GoalColor());

            if (hasTimer) timerGauge = CreateGauge("TimerGauge", new Vector2(-95, -205), ECCCarTheme.Icon("Icons/clock"), GuideColor);

            if (levelMode.Level.objective == ECCObjectiveType.DestroyCops || levelMode.Level.objective == ECCObjectiveType.CollectCash || levelMode.Level.objective == ECCObjectiveType.Checkpoints)
            {
                doneChip = CreateChip("DoneChip", goalGauge.root, new Vector2(52, -GaugeSize * 0.5f - 22), GoalColor());
                leftChip = CreateChip("LeftChip", goalGauge.root, new Vector2(-52, -GaugeSize * 0.5f - 22), Color.white);
            }

            if (timerGauge != null) ECCRuntimeUI.CreateText("TimerCaption", timerGauge.root, "زمان باقی‌مانده", 18, new Vector2(0.5f, 0.5f), new Vector2(0, -GaugeSize * 0.5f - 22), new Vector2(200, 26), Color.white);

            if (levelMode.escapePoint) BuildGuide();
        }

        Sprite GoalIcon()
        {
            switch (levelMode.Level.objective)
            {
                case ECCObjectiveType.DestroyCops: return ECCCarTheme.Icon("Icons/fire");
                case ECCObjectiveType.CollectCash: return ECCCoinBalanceButton.DollarSprite;
                case ECCObjectiveType.Escape: return ECCCarTheme.Icon("Icons/target");
                case ECCObjectiveType.DestroyBoss: return ECCCarTheme.Icon("Icons/target");
                case ECCObjectiveType.Heist: return ECCCarTheme.Icon("Icons/gift");
                case ECCObjectiveType.NoDamage: return ECCCarTheme.Icon("Icons/shield");
                case ECCObjectiveType.Checkpoints: return ECCCarTheme.Icon("Icons/bolt");
                default: return ECCCarTheme.Icon("Icons/heart");
            }
        }

        Color GoalColor()
        {
            switch (levelMode.Level.objective)
            {
                case ECCObjectiveType.DestroyCops: return CopColor;
                case ECCObjectiveType.CollectCash: return CashColor;
                case ECCObjectiveType.Escape: return GuideColor;
                case ECCObjectiveType.DestroyBoss: return ECCRuntimeUI.BadColor;
                case ECCObjectiveType.Heist: return ECCEscapePoint.LootColor;
                case ECCObjectiveType.NoDamage: return GuideColor;
                case ECCObjectiveType.Checkpoints: return ECCEscapePoint.CheckpointColor;
                default: return new Color(1, 0.35f, 0.45f, 1);
            }
        }

        Gauge CreateGauge(string name, Vector2 position, Sprite iconSprite, Color color)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            Gauge gauge = new Gauge();

            gauge.root = ECCRuntimeUI.CreateRect(name, canvas.transform, new Vector2(0.5f, 1), position, new Vector2(GaugeSize, GaugeSize));
            gauge.root.pivot = center;

            Image disc = ECCUIKit.CreateCircle("Disc", gauge.root, center, Vector2.zero, GaugeSize, new Color(0.06f, 0.05f, 0.14f, 0.78f));
            disc.raycastTarget = false;

            Image track = ECCRuntimeUI.CreateImage("Track", gauge.root, center, Vector2.zero, new Vector2(GaugeSize, GaugeSize), new Color(1, 1, 1, 0.14f));
            track.sprite = ECCCarTheme.Icon("Icons/ring");
            track.raycastTarget = false;

            gauge.fill = ECCRuntimeUI.CreateImage("Fill", gauge.root, center, Vector2.zero, new Vector2(GaugeSize, GaugeSize), color);
            gauge.fill.sprite = ECCCarTheme.Icon("Icons/ring");
            gauge.fill.type = Image.Type.Filled;
            gauge.fill.fillMethod = Image.FillMethod.Radial360;
            gauge.fill.fillOrigin = (int)Image.Origin360.Top;
            gauge.fill.fillClockwise = true;
            gauge.fill.fillAmount = 0;
            gauge.fill.raycastTarget = false;

            gauge.icon = ECCRuntimeUI.CreateImage("Icon", gauge.root, center, new Vector2(0, 40), new Vector2(34, 34), color);
            gauge.icon.sprite = iconSprite;
            gauge.icon.preserveAspect = true;
            gauge.icon.raycastTarget = false;

            // Numbers are set directly ( no Persian reordering ), in the bold title font
            gauge.value = ECCRuntimeUI.CreateText("Value", gauge.root, string.Empty, 44, center, new Vector2(0, -2), new Vector2(GaugeSize, 56), Color.white);
            gauge.value.font = ECCCarTheme.TitleFont;

            gauge.subValue = ECCRuntimeUI.CreateText("SubValue", gauge.root, string.Empty, 17, center, new Vector2(0, -38), new Vector2(GaugeSize, 24), new Color(1, 1, 1, 0.75f));

            return gauge;
        }

        Text CreateChip(string name, Transform parent, Vector2 position, Color textColor)
        {
            Image chip = ECCCarTheme.CreatePanel(name, parent, new Vector2(0.5f, 0.5f), position, new Vector2(100, 34), new Color(0.06f, 0.05f, 0.14f, 0.8f));

            return ECCRuntimeUI.CreateText("Label", chip.transform, string.Empty, 18, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 30), textColor);
        }

        /// <summary>
        /// A round badge that circles the middle of the screen ( where the car is ) and points at the destination,
        /// with the distance written under it
        /// </summary>
        void BuildGuide()
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            arrowBadge = ECCRuntimeUI.CreateRect("GuideArrow", canvas.transform, center, Vector2.zero, new Vector2(110, 110));

            arrowDisc = ECCUIKit.CreateCircle("Disc", arrowBadge, center, Vector2.zero, 104, new Color(0.04f, 0.2f, 0.08f, 0.75f));
            arrowDisc.raycastTarget = false;

            arrowRing = ECCRuntimeUI.CreateImage("Ring", arrowBadge, center, Vector2.zero, new Vector2(110, 110), GuideColor);
            arrowRing.sprite = ECCCarTheme.Icon("Icons/ring");
            arrowRing.raycastTarget = false;

            // Two chevrons, the back one fainter, so it reads as "go this way"
            Image back = ECCRuntimeUI.CreateImage("ChevronBack", arrowBadge, center, Vector2.zero, new Vector2(58, 58), new Color(1, 1, 1, 0.45f));
            back.sprite = ECCCarTheme.Icon("Icons/chevron");
            back.preserveAspect = true;
            back.raycastTarget = false;
            arrowChevronBack = back.rectTransform;

            Image front = ECCRuntimeUI.CreateImage("Chevron", arrowBadge, center, Vector2.zero, new Vector2(62, 62), Color.white);
            front.sprite = ECCCarTheme.Icon("Icons/chevron");
            front.preserveAspect = true;
            front.raycastTarget = false;
            arrowChevron = front.rectTransform;

            arrowDistance = ECCRuntimeUI.CreateText("Distance", arrowBadge, string.Empty, 22, center, new Vector2(0, -74), new Vector2(160, 30), Color.white);

            // The warning, low on the screen so it doesn't cover the road ahead
            Image banner = ECCCarTheme.CreatePanel("AwayWarning", canvas.transform, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(560, 92), new Color(0.75f, 0.1f, 0.08f, 0.92f));
            warningBanner = banner.rectTransform;

            warningText = ECCRuntimeUI.CreateText("Text", warningBanner, "داری از مقصد دور می‌شی!", 30, new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(540, 40), Color.white);
            ECCRuntimeUI.CreateText("Hint", warningBanner, "برگرد و دنبال فلش سبز راهنما برو", 20, new Vector2(0.5f, 0.5f), new Vector2(0, -22), new Vector2(540, 30), WarnColor);

            warningBanner.gameObject.SetActive(false);
        }

        void Update()
        {
            bool visible = isHidden == false && (gameController.gameCanvas == null || gameController.gameCanvas.gameObject.activeInHierarchy);

            if (canvas.enabled != visible) canvas.enabled = visible;

            if (visible == false) return;

            UpdateGoalGauge();

            if (timerGauge != null) UpdateTimerGauge();

            if (arrowBadge) UpdateGuide();

            AnimatePunch(goalGauge);
            if (timerGauge != null) AnimatePunch(timerGauge);
        }

        void UpdateGoalGauge()
        {
            ECCLevelData level = levelMode.Level;

            goalGauge.fill.fillAmount = Mathf.MoveTowards(goalGauge.fill.fillAmount, levelMode.Progress, Time.deltaTime * 1.5f);

            switch (level.objective)
            {
                case ECCObjectiveType.DestroyCops:
                    {
                        int destroyed = Mathf.Min(levelMode.CopsDestroyed, level.target);

                        SetNumber(goalGauge.value, destroyed);
                        ECCRuntimeUI.SetText(goalGauge.subValue, "از " + ECCUIKit.PersianDigits(level.target));
                        ECCRuntimeUI.SetText(doneChip, "منفجر " + ECCUIKit.PersianDigits(destroyed));
                        ECCRuntimeUI.SetText(leftChip, "مانده " + ECCUIKit.PersianDigits(level.target - destroyed));

                        if (levelMode.CopsDestroyed > lastCops)
                        {
                            goalGauge.punch = 1;
                            FloatingText("+" + ECCUIKit.PersianDigits(levelMode.CopsDestroyed - lastCops), CopColor);

                            int left = level.target - destroyed;

                            if (left > 0 && left <= 3) ECCHintBanner.Show("Icons/target", CopColor, left == 1 ? "فقط یکی دیگه!" : "فقط " + ECCUIKit.PersianDigits(left) + " تای دیگه!", "تا پایان مرحله چیزی نمونده", 1.4f, ECCHintBanner.Kind.Urgent);
                        }

                        lastCops = levelMode.CopsDestroyed;
                    }
                    break;

                case ECCObjectiveType.CollectCash:
                    {
                        int cash = Mathf.Min(levelMode.CashCollected, level.target);

                        SetNumber(goalGauge.value, cash);
                        ECCRuntimeUI.SetText(goalGauge.subValue, "از " + ECCUIKit.PersianDigits(level.target) + " دلار");
                        ECCRuntimeUI.SetText(doneChip, "جمع " + ECCUIKit.PersianDigits(cash));
                        ECCRuntimeUI.SetText(leftChip, "مانده " + ECCUIKit.PersianDigits(level.target - cash));

                        if (levelMode.CashCollected > lastCash)
                        {
                            goalGauge.punch = 1;
                            FloatingText("+" + ECCUIKit.PersianDigits(levelMode.CashCollected - lastCash) + "$", CashColor);
                        }

                        lastCash = levelMode.CashCollected;
                    }
                    break;

                case ECCObjectiveType.Escape:
                    SetNumber(goalGauge.value, Mathf.RoundToInt(levelMode.DistanceToEscape));
                    ECCRuntimeUI.SetText(goalGauge.subValue, "متر تا مقصد");
                    break;

                case ECCObjectiveType.NoDamage:
                    SetNumber(goalGauge.value, Mathf.RoundToInt(levelMode.DistanceToEscape));
                    ECCRuntimeUI.SetText(goalGauge.subValue, "متر تا مقصد");
                    break;

                case ECCObjectiveType.Heist:
                    SetNumber(goalGauge.value, Mathf.RoundToInt(levelMode.DistanceToEscape));
                    ECCRuntimeUI.SetText(goalGauge.subValue, levelMode.HasLoot ? "متر تا فرار" : "متر تا کیف پول");
                    goalGauge.icon.color = levelMode.HasLoot ? GuideColor : ECCEscapePoint.LootColor;
                    break;

                case ECCObjectiveType.Checkpoints:
                    {
                        int passed = Mathf.Min(levelMode.CheckpointsPassed, level.target);

                        SetNumber(goalGauge.value, passed);
                        ECCRuntimeUI.SetText(goalGauge.subValue, "از " + ECCUIKit.PersianDigits(level.target) + " چک‌پوینت");
                        ECCRuntimeUI.SetText(doneChip, "رد شده " + ECCUIKit.PersianDigits(passed));
                        ECCRuntimeUI.SetText(leftChip, "مانده " + ECCUIKit.PersianDigits(level.target - passed));

                        if (passed > lastCheckpoints) goalGauge.punch = 1;

                        lastCheckpoints = passed;
                    }
                    break;

                case ECCObjectiveType.DestroyBoss:
                    if (levelMode.boss)
                    {
                        int percent = Mathf.CeilToInt(Mathf.Max(0, levelMode.boss.health) / Mathf.Max(1, levelMode.boss.healthMax) * 100);

                        goalGauge.value.text = ECCUIKit.PersianDigits(percent) + "٪";
                        ECCRuntimeUI.SetText(goalGauge.subValue, "سلامت اتوبوس");
                    }
                    else
                    {
                        goalGauge.value.text = "...";
                        ECCRuntimeUI.SetText(goalGauge.subValue, "در راه است");
                    }
                    break;

                default:
                    goalGauge.value.text = ClockText(Mathf.Max(0, level.target - levelMode.Elapsed));
                    ECCRuntimeUI.SetText(goalGauge.subValue, "زنده بمان");
                    break;
            }
        }

        void UpdateTimerGauge()
        {
            float timeLeft = levelMode.TimeLeft;
            float fraction = Mathf.Clamp01(timeLeft / Mathf.Max(1, levelMode.TimeLimitTotal));

            timerGauge.fill.fillAmount = fraction;
            timerGauge.value.text = ClockText(timeLeft);

            Color color = fraction > 0.5f ? GuideColor : (fraction > 0.25f ? WarnColor : ECCRuntimeUI.BadColor);

            // The last ten seconds: red, ticking with a punch every second
            int second = Mathf.CeilToInt(timeLeft);

            if (timeLeft <= 10)
            {
                color = ECCRuntimeUI.BadColor;

                if (second != lastSecond && second > 0) timerGauge.punch = 1;

                timerGauge.value.color = Color.Lerp(Color.white, ECCRuntimeUI.BadColor, 0.5f + Mathf.Sin(Time.time * 12) * 0.5f);
            }
            else
            {
                timerGauge.value.color = Color.white;
            }

            lastSecond = second;

            timerGauge.fill.color = color;
            timerGauge.icon.color = color;
        }

        void AnimatePunch(Gauge gauge)
        {
            gauge.punch = Mathf.MoveTowards(gauge.punch, 0, Time.deltaTime * 4);

            gauge.root.localScale = Vector3.one * (1 + Mathf.Sin(gauge.punch * Mathf.PI) * 0.18f);
        }

        void UpdateGuide()
        {
            if (gameController.playerObject == null || levelMode.escapePoint == null || Camera.main == null) return;

            // A new destination ( the heist's escape point, the next checkpoint ): start the away warning over
            if (guidedPoint != levelMode.escapePoint)
            {
                guidedPoint = levelMode.escapePoint;
                lastDistance = -1;
                distanceRate = 0;
            }

            Vector3 direction = levelMode.escapePoint.transform.position - gameController.playerObject.transform.position;

            // Direction relative to the camera, on the ground plane
            Transform cameraTransform = Camera.main.transform;

            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;

            Vector3 cameraRight = cameraTransform.right;
            cameraRight.y = 0;

            float angle = Mathf.Atan2(Vector3.Dot(direction, cameraRight.normalized), Vector3.Dot(direction, cameraForward.normalized));

            // The badge circles the car and bobs outwards, the chevrons point at the destination ( the icon points left )
            float radius = ArrowRadius + Mathf.Sin(Time.time * 6) * 10;

            arrowBadge.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;

            Vector3 rotation = Vector3.forward * (-90 - angle * Mathf.Rad2Deg);

            arrowChevron.localEulerAngles = rotation;
            arrowChevronBack.localEulerAngles = rotation;

            Vector2 pointing = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));

            arrowChevron.anchoredPosition = pointing * (6 + Mathf.Sin(Time.time * 8) * 5);
            arrowChevronBack.anchoredPosition = pointing * -12;

            float distance = levelMode.DistanceToEscape;

            ECCRuntimeUI.SetText(arrowDistance, ECCUIKit.PersianDigits(Mathf.RoundToInt(distance)) + " متر");

            UpdateAwayWarning(distance);

            Color color = isWarning ? Color.Lerp(ECCRuntimeUI.BadColor, WarnColor, 0.5f + Mathf.Sin(Time.time * 10) * 0.5f) : GuideColor;

            arrowRing.color = color;
            arrowDisc.color = isWarning ? new Color(0.3f, 0.04f, 0.03f, 0.8f) : new Color(0.04f, 0.2f, 0.08f, 0.75f);

            // Bigger and pulsing while the player drives the wrong way
            float scale = isWarning ? 1.25f + Mathf.Sin(Time.time * 10) * 0.08f : 1;

            arrowBadge.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// Warns when the distance to the destination keeps growing for a while ( driving away from it ),
        /// and clears once the player is getting closer again
        /// </summary>
        void UpdateAwayWarning(float distance)
        {
            if (Time.deltaTime <= 0) return;

            if (lastDistance < 0) lastDistance = distance;

            float rate = (distance - lastDistance) / Time.deltaTime;

            lastDistance = distance;

            distanceRate = Mathf.Lerp(distanceRate, rate, Time.deltaTime * 3);

            bool nearGoal = distance < levelMode.escapePoint.reachRadius * 4;

            if (distanceRate > 2.5f && nearGoal == false)
            {
                awayTime += Time.deltaTime;
                closerTime = 0;
            }
            else if (distanceRate < -1)
            {
                closerTime += Time.deltaTime;
                awayTime = 0;
            }

            if (isWarning == false && awayTime > 1.5f)
            {
                isWarning = true;

                warningBanner.gameObject.SetActive(true);
            }
            else if (isWarning && (closerTime > 0.6f || nearGoal))
            {
                isWarning = false;
                awayTime = 0;

                warningBanner.gameObject.SetActive(false);
            }

            if (isWarning)
            {
                warningBanner.localScale = Vector3.one * (1 + Mathf.Sin(Time.time * 8) * 0.04f);
                warningText.color = Color.Lerp(Color.white, WarnColor, 0.5f + Mathf.Sin(Time.time * 8) * 0.5f);
            }
        }

        /// <summary>
        /// A short "+1" that floats up from the goal gauge and fades out
        /// </summary>
        void FloatingText(string content, Color color)
        {
            Text text = ECCRuntimeUI.CreateText("Floating", canvas.transform, string.Empty, 40, new Vector2(0.5f, 1), goalGauge.root.anchoredPosition + new Vector2(0, -GaugeSize * 0.5f - 60), new Vector2(200, 50), color);
            text.font = ECCCarTheme.TitleFont;
            text.text = content;

            text.gameObject.AddComponent<ECCFloatUp>();
        }

        static void SetNumber(Text text, int value)
        {
            text.text = ECCUIKit.PersianDigits(value);
        }

        /// <summary>
        /// Seconds as a clock, like ۱:۰۵. Set directly, without Persian reordering
        /// </summary>
        static string ClockText(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0, seconds));

            return ECCUIKit.PersianDigits(total / 60) + ":" + ECCUIKit.PersianDigits(total % 60).PadLeft(2, '۰');
        }

        public void Hide()
        {
            isHidden = true;
        }
    }

    /// <summary>
    /// Floats a UI element up while fading it out, then removes it
    /// </summary>
    public class ECCFloatUp : MonoBehaviour
    {
        public float duration = 1;
        public float distance = 70;

        float time = 0;
        Vector2 start;
        Graphic graphic;

        void Start()
        {
            graphic = GetComponent<Graphic>();
            start = ((RectTransform)transform).anchoredPosition;
        }

        void Update()
        {
            time += Time.deltaTime;

            float progress = time / duration;

            ((RectTransform)transform).anchoredPosition = start + Vector2.up * distance * progress;
            transform.localScale = Vector3.one * (progress < 0.15f ? Mathf.Lerp(1.6f, 1, progress / 0.15f) : 1);

            if (graphic)
            {
                Color color = graphic.color;
                color.a = progress < 0.6f ? 1 : Mathf.Lerp(1, 0, (progress - 0.6f) / 0.4f);
                graphic.color = color;
            }

            if (progress >= 1) Destroy(gameObject);
        }
    }
}
