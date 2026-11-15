using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The first-run tutorial: during the first chases of a new player, a card in the middle of the screen explains one
    /// thing at a time and waits until the player does it ( or a few seconds pass ). The X on the card skips the rest of it:
    /// steer, wreck the police, pick up dollars, pass close to cars, use the power-ups. The step reached is saved, so a run
    /// that ends early continues the tutorial in the next one. Players who had the game before this update never see it.
    /// </summary>
    public class ECCTutorial : MonoBehaviour
    {
        struct Step
        {
            public string icon;
            public string title;
            public string text;
            public float maxTime;
        }

        static readonly Step[] Steps =
        {
            new Step { icon = "SteeringIcon", title = "فرمان دست توئه!", text = "فرمان پایین صفحه رو به چپ و راست بکش تا ماشین بپیچه", maxTime = 6 },
            new Step { icon = "Icons/fire", title = "پلیس‌ها رو نابود کن", text = "پلیس‌ها رو به هم، به ساختمون‌ها یا به میخ‌ها بکوبون", maxTime = 7 },
            new Step { icon = "dollar", title = "دلارها رو جمع کن", text = "از روی دلارهای جاده رد شو؛ با پولش ماشین بخر و ارتقا بده", maxTime = 7 },
            new Step { icon = "Icons/bolt", title = "از بیخ گوش رد شو", text = "خیلی نزدیک از کنار پلیس یا ماشین‌ها رد شو تا سکه‌ی اضافه بگیری", maxTime = 7 },
            new Step { icon = "Icons/shield", title = "قدرت‌های ویژه", text = "وقتی گیر افتادی، از دکمه‌های قدرت ویژه‌ی پایین صفحه کمک بگیر", maxTime = 5 },
            new Step { icon = "Icons/star", title = "آفرین، آماده‌ای!", text = "حالا تا می‌تونی از دست پلیس فرار کن", maxTime = 2.5f },
        };

        const int SteerStep = 0, CopsStep = 1, CashStep = 2, NearMissStep = 3;

        // Turning this many degrees in total counts as having learned to steer
        const float SteerDegrees = 70;

        ECCGameController gameController;
        int step;
        float stepTime;
        float steered;
        float lastYaw;
        bool hasYaw;
        bool done;

        Canvas canvas;
        RectTransform card;
        Image icon;
        Text title;
        Text text;
        Text counter;
        RectTransform pointer;

        /// <summary>
        /// True while a new player hasn't finished the tutorial
        /// </summary>
        public static bool IsNeeded
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                if (data.tutorialStep < 0)
                {
                    // Decided once: players from before this update already know the game
                    data.tutorialStep = ECCDaily.IsLegacyInstall ? Steps.Length : 0;
                    ECCSave.Save();
                }

                return data.tutorialStep < Steps.Length;
            }
        }

        public static int StepCount
        {
            get { return Steps.Length; }
        }

        public static void StartIfNeeded(ECCGameController gameController)
        {
            if (gameController == null || IsNeeded == false) return;

            // Not in the daily challenge: it has its own clock and rules
            if (gameController.activeMode is Modes.ECCDailyChallengeMode) return;

            if (gameController.GetComponent<ECCTutorial>() == null) gameController.gameObject.AddComponent<ECCTutorial>();
        }

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            step = Mathf.Clamp(ECCSave.Data.tutorialStep, 0, Steps.Length - 1);

            ECCCar.CarDestroyed += OnCarDestroyed;
            ECCItem.Collected += OnItemCollected;
            ECCComboSystem.NearMiss += OnNearMiss;

            Build();
            ShowStep();
        }

        void OnDestroy()
        {
            ECCCar.CarDestroyed -= OnCarDestroyed;
            ECCItem.Collected -= OnItemCollected;
            ECCComboSystem.NearMiss -= OnNearMiss;

            if (canvas) Destroy(canvas.gameObject);
        }

        void Build()
        {
            canvas = ECCRuntimeUI.CreateCanvas("CanvasTutorial", 55);

            Vector2 center = new Vector2(0.5f, 0.5f);

            card = ECCCarTheme.CreatePanel("Card", canvas.transform, new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(620, 150), new Color(0.06f, 0.07f, 0.16f, 0.88f)).rectTransform;

            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = new Color(ECCCarTheme.Yellow.r, ECCCarTheme.Yellow.g, ECCCarTheme.Yellow.b, 0.8f);
            border.effectDistance = new Vector2(3, -3);

            Image iconDisc = ECCUIKit.CreateCircle("IconDisc", card, new Vector2(1, 0.5f), new Vector2(-70, 0), 96, ECCCarTheme.Yellow);
            iconDisc.rectTransform.pivot = center;
            iconDisc.raycastTarget = false;

            icon = ECCRuntimeUI.CreateImage("Icon", iconDisc.transform, center, Vector2.zero, new Vector2(60, 60), Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            title = ECCRuntimeUI.CreateText("Title", card, string.Empty, 32, new Vector2(1, 0.5f), new Vector2(-136, 30), new Vector2(440, 44), ECCCarTheme.Yellow);
            title.alignment = TextAnchor.MiddleRight;
            title.rectTransform.pivot = new Vector2(1, 0.5f);

            text = ECCRuntimeUI.CreateText("Text", card, string.Empty, 22, new Vector2(1, 0.5f), new Vector2(-136, -22), new Vector2(450, 64), Color.white);
            text.alignment = TextAnchor.MiddleRight;
            text.rectTransform.pivot = new Vector2(1, 0.5f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;

            counter = ECCRuntimeUI.CreateText("Counter", card, string.Empty, 18, new Vector2(0, 0), new Vector2(54, 22), new Vector2(90, 26), new Color(1, 1, 1, 0.6f));

            // The tutorial is optional: the X on the top left corner ( half outside the card ) closes all of it
            Button close = ECCUIKit.CreateCloseButton(card);
            RectTransform closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0, 1);
            closeRect.pivot = center;
            closeRect.anchoredPosition = new Vector2(6, -6);
            closeRect.sizeDelta = new Vector2(56, 56);
            close.onClick.AddListener(Skip);

            // A bouncing arrow over the steering wheel for the first step
            Image arrow = ECCRuntimeUI.CreateImage("Pointer", canvas.transform, center, Vector2.zero, new Vector2(90, 90), ECCCarTheme.Yellow);
            arrow.sprite = ECCCarTheme.Icon("Icons/chevron");
            arrow.preserveAspect = true;
            arrow.raycastTarget = false;
            arrow.rectTransform.localEulerAngles = new Vector3(0, 0, 90);
            pointer = arrow.rectTransform;
        }

        void ShowStep()
        {
            Step current = Steps[step];

            Sprite sprite = current.icon == "dollar" ? ECCCoinBalanceButton.DollarSprite : ECCCarTheme.Icon(current.icon);
            if (sprite == null) sprite = Resources.Load<Sprite>("ECCUI/" + current.icon);

            icon.sprite = sprite;
            icon.enabled = sprite != null;

            ECCRuntimeUI.SetText(title, current.title);
            ECCRuntimeUI.SetText(text, current.text);
            ECCRuntimeUI.SetText(counter, ECCUIKit.PersianDigits(step + 1) + " از " + ECCUIKit.PersianDigits(Steps.Length));

            ECCPunchScale.Play(card);

            stepTime = 0;
        }

        void Update()
        {
            if (done || gameController == null) return;

            bool visible = gameController.gameStarted && gameController.isGameOver == false && gameController.isPaused == false && (gameController.gameCanvas == null || gameController.gameCanvas.gameObject.activeInHierarchy);

            if (canvas.enabled != visible) canvas.enabled = visible;

            UpdatePointer();

            if (visible == false || gameController.startDelay > 0) return;

            stepTime += Time.deltaTime;

            if (step == SteerStep) TrackSteering();

            if (stepTime >= Steps[step].maxTime) NextStep();
        }

        void TrackSteering()
        {
            ECCCar player = gameController.playerObject;

            if (player == null) return;

            float yaw = player.transform.eulerAngles.y;

            if (hasYaw) steered += Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw));

            lastYaw = yaw;
            hasYaw = true;

            // Give the player a moment to read before moving on
            if (steered >= SteerDegrees && stepTime > 1.5f) NextStep();
        }

        void UpdatePointer()
        {
            Slider wheel = gameController.steeringWheel;

            bool show = canvas.enabled && step == SteerStep && wheel && wheel.gameObject.activeInHierarchy;

            if (pointer.gameObject.activeSelf != show) pointer.gameObject.SetActive(show);

            if (show == false) return;

            // Both canvases are screen space overlays, so the wheel's position is a screen position
            RectTransform wheelRect = (RectTransform)wheel.transform;
            Vector3 top = wheelRect.position + Vector3.up * wheelRect.rect.height * wheelRect.lossyScale.y * 0.5f;

            pointer.position = top + Vector3.up * (60 + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5)) * 30) * pointer.lossyScale.y;
        }

        void OnCarDestroyed(ECCCar car)
        {
            if (step == CopsStep && car && car.IsPlayer == false && stepTime > 1.5f) NextStep();
        }

        void OnItemCollected(ECCItem item)
        {
            if (step == CashStep && item && item.CashValue > 0 && stepTime > 1.5f) NextStep();
        }

        void OnNearMiss()
        {
            if (step == NearMissStep && stepTime > 1.5f) NextStep();
        }

        /// <summary>
        /// Closes the tutorial for good ( the X button ): it is saved as finished and never shown again
        /// </summary>
        public void Skip()
        {
            if (done) return;

            done = true;

            ECCSave.Data.tutorialStep = Steps.Length;
            ECCSave.Save();

            if (canvas) Destroy(canvas.gameObject);
        }

        void NextStep()
        {
            step++;

            ECCSave.Data.tutorialStep = Mathf.Max(ECCSave.Data.tutorialStep, step);
            ECCSave.Save();

            if (step >= Steps.Length)
            {
                done = true;

                Destroy(canvas.gameObject);

                return;
            }

            ECCHaptics.Vibrate(ECCHaptics.Kind.Light);

            ShowStep();
        }
    }
}
