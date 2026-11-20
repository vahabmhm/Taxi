using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Levels;
using EndlessCarChase.Modes;

namespace EndlessCarChase
{
    /// <summary>
    /// The main menu in CrowdRush's layout, built from code on top of the existing menu canvas ( CanvasMenu ):
    /// - the purple «گاراژ» button top left, the coin balance top right, the title
    /// - the white level card: chapter ribbon, objective badge, «مرحله N», its goal, the stars, «شروع مرحله» and «مراحل»,
    ///   and under them the separate purple «حالت بی‌نهایت» button with the record ( so the two modes are clearly apart )
    /// - two columns of candy buttons for the daily features, with count badges and "!" dots
    /// - the shared coin balance button ( main menu and garage )
    /// - the daily popup sequence at the first safe main-menu entry ( daily challenge first, then the login calendar )
    /// The old menu buttons are hidden, and the new ones call their original listeners, so the scene wiring is kept.
    /// Created by the game controller; place it on a GameObject in the scene to change its settings.
    /// </summary>
    public class ECCMenuHub : MonoBehaviour
    {
        [Tooltip("Coin button position in the main menu, from the top right corner of the safe area")]
        public Vector2 menuCoinPosition = new Vector2(-30, -34);

        [Tooltip("Coin button position in the garage, from the top right corner of the safe area")]
        public Vector2 garageCoinPosition = new Vector2(-30, -34);

        ECCGameController gameController;
        GameObject menuCanvas;

        Canvas canvas;
        RectTransform safeArea;
        ECCCoinBalanceButton coinButton;
        RectTransform background;
        RectTransform menuRoot;

        Button oldStartButton;
        Button oldGarageButton;

        Text subtitle;
        GameObject levelSelectCanvas;

        bool lastMenuVisible = false;
        bool lastGarageVisible = false;

        static ECCMenuHub instance;

        public static ECCMenuHub Instance
        {
            get { return instance; }
        }

        public static ECCMenuHub Create(ECCGameController gameController)
        {
            ECCMenuHub hub = FindObjectOfType<ECCMenuHub>();

            if (hub == null) hub = new GameObject("MenuHub").AddComponent<ECCMenuHub>();

            hub.Build(gameController);

            return hub;
        }

        /// <summary>
        /// True while the main menu ( not the garage, not the game ) is on screen
        /// </summary>
        public bool IsMainMenuVisible
        {
            get { return gameController && gameController.gameStarted == false && menuCanvas && menuCanvas.activeInHierarchy; }
        }

        public bool IsGarageVisible
        {
            get { return gameController && gameController.gameStarted == false && gameController.shopMenu && gameController.shopMenu.gameObject.activeInHierarchy; }
        }

        void Build(ECCGameController controller)
        {
            if (canvas) return;

            instance = this;
            gameController = controller;

            menuCanvas = GameObject.Find("CanvasMenu");

            safeArea = ECCUIKit.CreateSafeCanvas("CanvasMenuHub", 62, out canvas);
            canvas.transform.SetParent(transform, false);

            BuildMenu();

            // The coin balance is shared by the main menu and the garage, so it's outside the menu root
            coinButton = ECCCoinBalanceButton.Create(safeArea, new Vector2(1, 1), menuCoinPosition);
            coinButton.GetComponent<RectTransform>().pivot = new Vector2(1, 1);
            coinButton.transform.localScale = Vector3.one * 1.12f;

            canvas.enabled = false;
        }

        void Update()
        {
            if (canvas == null) return;

            bool menuVisible = IsMainMenuVisible;
            bool garageVisible = IsGarageVisible;

            if (menuVisible == lastMenuVisible && garageVisible == lastGarageVisible) return;

            lastMenuVisible = menuVisible;
            lastGarageVisible = garageVisible;

            canvas.enabled = menuVisible || garageVisible;

            ((RectTransform)coinButton.transform).anchoredPosition = garageVisible ? garageCoinPosition : menuCoinPosition;

            if (menuRoot) menuRoot.gameObject.SetActive(menuVisible);
            if (background) background.gameObject.SetActive(menuVisible);

            if (menuVisible) OnMainMenuShown();
        }

        // ----- The CrowdRush style menu -----

        // Design size of the middle column ( title, level card, buttons ); it shrinks on short screens
        const float ColumnHeight = 1266;
        const float CardWidth = 640;
        const float CardHeight = 590;
        const float GridButtonWidth = 308;
        const float GridButtonHeight = 92;

        class FeatureButton
        {
            public Button button;
            public RectTransform badge;
            public Text badgeText;
            public Image dot;
        }

        readonly Dictionary<string, FeatureButton> features = new Dictionary<string, FeatureButton>();

        RectTransform column;
        int gridIndex = 0;

        // The level card
        Text chapterText;
        Text levelTitle;
        Text levelNumber;
        Text levelGoal;
        Text starsText;
        Image levelBadge;
        Button playLevel;
        Button openMap;
        Button endlessButton;
        Text endlessRecord;
        GameObject recordChip;

        void BuildMenu()
        {
            // Full screen background ( also behind the notch ): the night sky with a siren bar
            background = ECCRuntimeUI.CreateRect("MenuBackground", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ECCUIKit.Stretch(background);
            background.SetAsFirstSibling();

            Image night = ECCRuntimeUI.CreateImage("Night", background, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(ECCCarTheme.Night.r, ECCCarTheme.Night.g, ECCCarTheme.Night.b, 0.9f));
            ECCUIKit.Stretch(night.rectTransform);

            // A soft light behind the card
            Image glow = ECCRuntimeUI.CreateImage("Glow", background, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(900, 900), new Color(ECCCarTheme.NightBright.r, ECCCarTheme.NightBright.g, ECCCarTheme.NightBright.b, 0.55f));
            glow.sprite = ECCCarTheme.CardSprite("glow");
            glow.raycastTarget = false;

            ECCCarTheme.CreateSirenBar("Siren", background, 10);

            menuRoot = ECCRuntimeUI.CreateRect("CrowdMenu", safeArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ECCUIKit.Stretch(menuRoot);

            AdoptOldMenu();

            // The garage, top left ( the coin balance holds the top right ), like CrowdRush's shop button
            Button garage = CandyButton(menuRoot, "Garage", "گاراژ", ECCCarTheme.Hex(0xA45CFF), "GarageIcon", new Vector2(230, 84), 28);
            RectTransform garageRect = (RectTransform)garage.transform;
            garageRect.anchorMin = garageRect.anchorMax = garageRect.pivot = new Vector2(0, 1);
            garageRect.anchoredPosition = new Vector2(24, -26);
            garage.onClick.AddListener(OpenGarage);

            // The middle column, scaled down as one piece on short screens
            RectTransform area = ECCRuntimeUI.CreateRect("ColumnArea", menuRoot, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(0, 110);
            area.offsetMax = new Vector2(0, -126);

            column = ECCRuntimeUI.CreateRect("Column", area, new Vector2(0.5f, 1), Vector2.zero, new Vector2(720, ColumnHeight));
            ECCFitHeight fit = column.gameObject.AddComponent<ECCFitHeight>();
            fit.designHeight = ColumnHeight;

            Vector2 topCenter = new Vector2(0.5f, 1);

            Text title = ECCRuntimeUI.CreateText("Title", column, "تاکسی شهر", 64, topCenter, new Vector2(0, -6), new Vector2(620, 84), Color.white);
            ECCCarTheme.CandyLabel(title);
            title.rectTransform.pivot = topCenter;

            subtitle = ECCRuntimeUI.CreateText("Subtitle", column, "مسافر سوار کن، از رقیب‌ها جلو بزن!", 24, topCenter, new Vector2(0, -88), new Vector2(620, 36), ECCCarTheme.NightText);
            subtitle.rectTransform.pivot = topCenter;
            subtitle.GetComponent<Outline>().enabled = false;

            // The driver's rank under the title; tapping it opens the rank popup
            ECCRuntimeUI.SetText(subtitle, TaxiRankUI.MenuLine());
            subtitle.color = ECCCarTheme.Hex(0xFFD54A);
            subtitle.raycastTarget = true;
            subtitle.gameObject.AddComponent<Button>().onClick.AddListener(TaxiRankUI.Open);

            BuildLevelCard(new Vector2(0, -186));

            // Two columns of candy buttons under the card ( right to left )
            float gridTop = -186 - CardHeight - 36;

            AddFeature("missions", "مأموریت‌های امروز", ECCCarTheme.Hex(0x8E5CF7), null, Color.white, ECCDailyMissionsUI.Open, gridTop);
            AddFeature("challenge", "چالش روز", ECCCarTheme.Hex(0xFF6B4A), null, Color.white, delegate { ECCChallengePopup.Open(gameController); }, gridTop);
            AddFeature("wheel", "گردونه شانس", ECCCarTheme.CandyBlue, null, Color.white, ECCLuckyWheelUI.Open, gridTop);
            AddFeature("othergames", "سایر بازی‌ها", ECCCarTheme.Hex(0x14B8A6), null, Color.white, ECCOtherGamesUI.Open, gridTop);
            AddFeature("login", "جایزه ورود روزانه", ECCCarTheme.Hex(0xF4B400), null, Color.white, ECCLoginRewardsUI.Open, gridTop);
            AddFeature("achievements", "دستاوردها", ECCCarTheme.Hex(0xE0A100), null, Color.white, ECCAchievementsUI.Open, gridTop);

            // The weekly star league: a wide button under the grid, with the player's rank
            BuildLeagueButton(gridTop - 3 * (GridButtonHeight + 24));

            ECCAchievements.Changed += RefreshBadges;

            ECCMissions.Changed += RefreshBadges;

            ECCWeeklyLeague.Changed += OnLeagueChanged;

            // Today's login counts for the ten-day rewards
            ECCLoginRewards.RegisterLogin();

            // A wheel spin interrupted by closing the app is paid once
            int recovered = ECCLuckyWheel.PayPending();
            if (recovered > 0) ECCRewardFx.ShowCoins(recovered);

            RefreshBadges();
            RefreshMode();
        }

        /// <summary>
        /// Hides the old menu ( title, GO!, shop and more games buttons ) and keeps their listeners for the new buttons.
        /// The sound and music buttons become themed buttons on the new menu, bottom left
        /// </summary>
        void AdoptOldMenu()
        {
            if (menuCanvas == null) return;

            foreach (Button existing in menuCanvas.GetComponentsInChildren<Button>(true))
            {
                switch (existing.name)
                {
                    case "ButtonStartGame":
                        oldStartButton = existing;
                        existing.gameObject.SetActive(false);
                        break;

                    case "ButtonShop":
                        oldGarageButton = existing;
                        existing.gameObject.SetActive(false);
                        break;

                    case "ButtonMoreGames":
                        RewireOtherGamesButton(existing);
                        existing.gameObject.SetActive(false);
                        break;

                    case "ButtonSound":
                        AdoptAudioButton(existing, new Vector2(94, 24), false);
                        break;

                    case "ButtonMusic":
                        AdoptAudioButton(existing, new Vector2(0, 24), true);
                        break;
                }
            }

            // The phone vibration setting, next to the sound and music buttons
            ECCAudioToggle.CreateVibration(menuRoot, new Vector2(0.5f, 0), new Vector2(-94, 24), 82);

            Transform oldTitle = menuCanvas.transform.Find("Title");

            if (oldTitle) oldTitle.gameObject.SetActive(false);
        }

        /// <summary>
        /// A themed sound / music button ( ECCAudioToggle ) in place of an old one. The old button is hidden but kept:
        /// the new one calls its listener
        /// </summary>
        void AdoptAudioButton(Button button, Vector2 position, bool music)
        {
            // Bottom center, in one row with the vibration button ( sound on the right, then music, then vibration )
            ECCAudioToggle.Create(menuRoot, new Vector2(0.5f, 0), position, 82, button, music);

            button.gameObject.SetActive(false);
        }

        /// <summary>
        /// A CrowdRush candy button with an icon on the right and a heavy label
        /// </summary>
        static Button CandyButton(Transform parent, string name, string label, Color color, string icon, Vector2 size, int fontSize)
        {
            Button button = ECCRuntimeUI.CreateButton("Button" + name, parent, label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size, color);

            Text text = ECCRuntimeUI.GetLabel(button);

            if (string.IsNullOrEmpty(icon) == false)
            {
                float iconSize = Mathf.Min(size.y * 0.42f, 40);

                ECCCarTheme.CreateIcon("Icon", button.transform, icon, new Vector2(1, 0.5f), new Vector2(-14 - iconSize * 0.5f, 2), iconSize, Color.white).rectTransform.pivot = new Vector2(0.5f, 0.5f);

                text.rectTransform.sizeDelta = new Vector2(size.x - iconSize - 34, size.y);
                text.rectTransform.anchoredPosition = new Vector2(-(iconSize + 14) * 0.5f, 2);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 14;
                text.resizeTextMaxSize = fontSize;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            // The shared glossy style with the staggered light sweep ( the play button sweeps a little more often )
            ECCMenuButtonShine.Apply(button, name == "PlayLevel");

            return button;
        }

        /// <summary>
        /// The white level card: a ribbon with the chapter, the level's badge, number and goal, the stars, «شروع مرحله»
        /// and «مراحل», and under them the separate purple «حالت بی‌نهایت» button with the record
        /// </summary>
        void BuildLevelCard(Vector2 position)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 topRight = new Vector2(1, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            Image shadow = ECCRuntimeUI.CreateImage("CardShadow", column, topCenter, position + new Vector2(0, 10), new Vector2(CardWidth + 44, CardHeight + 48), new Color(0, 0, 0, 0.35f));
            shadow.sprite = ECCCarTheme.CardSprite("shadow");
            shadow.type = Image.Type.Sliced;
            shadow.raycastTarget = false;
            shadow.rectTransform.pivot = topCenter;

            Image card = ECCRuntimeUI.CreateImage("LevelCard", column, topCenter, position, new Vector2(CardWidth, CardHeight), new Color(1, 1, 1, 0.97f));
            card.sprite = ECCCarTheme.CardSprite("panel");
            card.type = Image.Type.Sliced;
            card.pixelsPerUnitMultiplier = 1.4f;
            card.rectTransform.pivot = topCenter;

            RectTransform rect = card.rectTransform;

            // Ribbon over the top edge: «فصل ۱ · شهر»
            Image ribbon = ECCRuntimeUI.CreateImage("Ribbon", rect, topCenter, new Vector2(0, 30), new Vector2(560, 96), ECCCarTheme.PlateBlue);
            ribbon.sprite = ECCCarTheme.CardSprite("ribbon");
            ribbon.type = Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 1.4f;
            ribbon.raycastTarget = false;

            chapterText = ECCRuntimeUI.CreateText("Chapter", ribbon.transform, string.Empty, 30, new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(460, 50), Color.white);
            ECCCarTheme.CandyLabel(chapterText);

            levelTitle = ECCRuntimeUI.CreateText("LevelTitle", rect, string.Empty, 23, topCenter, new Vector2(0, -60), new Vector2(CardWidth - 60, 34), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(levelTitle, ECCCarTheme.CardSubInk);

            // The level's objective badge ( right ), then its number and goal
            levelBadge = ECCUIKit.CreateCircle("Badge", rect, topRight, new Vector2(-96, -158), 124, ECCCarTheme.PlateBlue);
            levelBadge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            levelBadge.raycastTarget = false;

            Image ring = ECCRuntimeUI.CreateImage("Ring", levelBadge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(138, 138), new Color(1, 1, 1, 0.8f));
            ring.sprite = ECCCarTheme.Icon("Icons/ring");
            ring.raycastTarget = false;

            ECCPulse.Set(levelBadge, true, 0.04f);

            levelNumber = ECCRuntimeUI.CreateText("Number", rect, string.Empty, 58, topRight, new Vector2(-180, -128), new Vector2(420, 74), ECCCarTheme.CardInk);
            levelNumber.rectTransform.pivot = new Vector2(1, 0.5f);
            levelNumber.alignment = TextAnchor.MiddleRight;
            levelNumber.font = ECCCarTheme.TitleFont;
            ECCCarTheme.InkText(levelNumber, ECCCarTheme.CardInk);

            levelGoal = ECCRuntimeUI.CreateText("Goal", rect, string.Empty, 22, topRight, new Vector2(-180, -196), new Vector2(420, 64), ECCCarTheme.PlateBlue);
            levelGoal.rectTransform.pivot = new Vector2(1, 0.5f);
            levelGoal.alignment = TextAnchor.MiddleRight;
            ECCCarTheme.InkText(levelGoal, ECCCarTheme.Hex(0x1F5F9E));

            // Stars: a gold pill
            Image stars = ECCCarTheme.CreatePanel("Stars", rect, bottomCenter, new Vector2(0, 268), new Vector2(300, 52), new Color(1, 0.78f, 0.24f, 0.2f));
            ECCCarTheme.CreateIcon("Star", stars.transform, "Icons/star", new Vector2(1, 0.5f), new Vector2(-30, 1), 40, ECCCarTheme.Hex(0xFFC83D));

            starsText = ECCRuntimeUI.CreateText("Text", stars.transform, string.Empty, 24, new Vector2(0.5f, 0.5f), new Vector2(-20, 1), new Vector2(230, 44), ECCCarTheme.CandyOrangeShade);
            starsText.font = ECCCarTheme.TitleFont;
            ECCCarTheme.InkText(starsText, ECCCarTheme.CandyOrangeShade);

            // «شروع مرحله» ( right, primary ) and «مراحل» ( left )
            playLevel = CandyButton(rect, "PlayLevel", "شروع مرحله", ECCCarTheme.CandyGreen, "Icons/play", new Vector2(338, 96), 32);
            ((RectTransform)playLevel.transform).anchorMin = ((RectTransform)playLevel.transform).anchorMax = bottomCenter;
            ((RectTransform)playLevel.transform).anchoredPosition = new Vector2(126, 186);
            playLevel.onClick.AddListener(delegate { StartChase(GameModeType.Levels); });
            ECCPulse.Set(playLevel, true, 0.03f);

            openMap = CandyButton(rect, "LevelMap", "مراحل", ECCCarTheme.CandyBlue, "Icons/levels", new Vector2(220, 96), 30);
            ((RectTransform)openMap.transform).anchorMin = ((RectTransform)openMap.transform).anchorMax = bottomCenter;
            ((RectTransform)openMap.transform).anchoredPosition = new Vector2(-168, 186);
            openMap.onClick.AddListener(OpenLevelSelect);

            // The separate endless mode, with its record on the left
            endlessButton = CandyButton(rect, "Endless", "حالت بی‌نهایت", ECCCarTheme.Hex(0xA45CFF), null, new Vector2(CardWidth - 60, 96), 28);
            ((RectTransform)endlessButton.transform).anchorMin = ((RectTransform)endlessButton.transform).anchorMax = bottomCenter;
            ((RectTransform)endlessButton.transform).anchoredPosition = new Vector2(0, 74);
            endlessButton.onClick.AddListener(delegate { StartChase(GameModeType.Endless); });

            Image chip = ECCCarTheme.CreatePanel("Record", endlessButton.transform, new Vector2(0, 0.5f), new Vector2(18, 2), new Vector2(170, 60), new Color(0, 0, 0, 0.22f));
            chip.rectTransform.pivot = new Vector2(0, 0.5f);
            ECCCarTheme.CreateIcon("Trophy", chip.transform, "Icons/trophy", new Vector2(1, 0.5f), new Vector2(-26, 0), 38, ECCCarTheme.Hex(0xFFC83D));

            endlessRecord = ECCUIKit.CreateNumberText("Value", chip.transform, "0", 26, new Vector2(0.5f, 0.5f), new Vector2(-20, 0), new Vector2(120, 50), Color.white);
            recordChip = chip.gameObject;

            Text endlessLabel = ECCRuntimeUI.GetLabel(endlessButton);
            endlessLabel.rectTransform.anchoredPosition += new Vector2(70, 0);
            endlessLabel.rectTransform.sizeDelta -= new Vector2(160, 0);
        }

        /// <summary>
        /// Adds a candy button to the two-column grid under the level card ( right column first ), with a count badge on
        /// its top right corner and a "!" dot on its top left
        /// </summary>
        public Button AddFeature(string id, string label, Color color, string icon, Color iconColor, UnityEngine.Events.UnityAction onClick)
        {
            return AddFeature(id, label, color, icon, iconColor, onClick, -186 - CardHeight - 36);
        }

        Button AddFeature(string id, string label, Color color, string icon, Color iconColor, UnityEngine.Events.UnityAction onClick, float gridTop)
        {
            int row = gridIndex / 2;
            bool right = gridIndex % 2 == 0;

            gridIndex++;

            Button button = CandyButton(column, id, label, color, icon, new Vector2(GridButtonWidth, GridButtonHeight), 26);

            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2((right ? 1 : -1) * (GridButtonWidth * 0.5f + 12), gridTop - row * (GridButtonHeight + 24));

            button.onClick.AddListener(onClick);

            FeatureButton feature = new FeatureButton { button = button };

            // Count badge, top right: a white ring around a red disc
            Image badge = ECCUIKit.CreateCircle("Badge", rect, new Vector2(1, 1), new Vector2(-10, -6), 52, Color.white);
            badge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            badge.raycastTarget = false;

            Image badgeDisc = ECCUIKit.CreateCircle("Disc", badge.transform, new Vector2(0.5f, 0.5f), Vector2.zero, 42, ECCCarTheme.CandyRed);
            badgeDisc.raycastTarget = false;

            feature.badge = badge.rectTransform;
            feature.badgeText = ECCUIKit.CreateNumberText("Count", badge.transform, "0", 26, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42, 42), Color.white);
            badge.gameObject.SetActive(false);

            // "!" dot, top left: something is waiting there
            Image dot = ECCUIKit.CreateCircle("Dot", rect, new Vector2(0, 1), new Vector2(12, -6), 40, ECCCarTheme.CandyRed);
            dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dot.raycastTarget = false;

            Outline dotRing = dot.gameObject.AddComponent<Outline>();
            dotRing.effectColor = Color.white;
            dotRing.effectDistance = new Vector2(2, -2);

            Text mark = ECCRuntimeUI.CreateText("Mark", dot.transform, string.Empty, 26, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(40, 40), Color.white);
            mark.text = "!";
            mark.font = ECCCarTheme.TitleFont;

            feature.dot = dot;
            dot.gameObject.SetActive(false);

            features[id] = feature;

            return button;
        }

        /// <summary>
        /// Plays a mode: the mode is selected, then the chase starts through the old start button ( its scene wiring hides
        /// the menu and starts the game with the selected car )
        /// </summary>
        void StartChase(GameModeType mode)
        {
            if (ECCModalTracker.AnyOpen || levelSelectCanvas || gameController == null) return;

            if (ECCGameModeManager.IsModeAvailable(mode) == false) return;

            gameController.ChangeGameMode(mode);

            if (oldStartButton) oldStartButton.onClick.Invoke();
            else gameController.StartGame();
        }

        void OpenGarage()
        {
            if (ECCModalTracker.AnyOpen || levelSelectCanvas || oldGarageButton == null) return;

            oldGarageButton.onClick.Invoke();
        }

        static string ObjectiveIcon(ECCObjectiveType objective)
        {
            switch (objective)
            {
                case ECCObjectiveType.DestroyCops: return "Icons/target";
                case ECCObjectiveType.CollectCash: return "Icons/star";
                case ECCObjectiveType.Escape: return "Icons/play";
                case ECCObjectiveType.DestroyBoss: return "Icons/crown";
                case ECCObjectiveType.Heist: return "Icons/gift";
                case ECCObjectiveType.NoDamage: return "Icons/shield";
                case ECCObjectiveType.Checkpoints: return "Icons/bolt";
                default: return "Icons/clock";
            }
        }

        /// <summary>
        /// The level card ( chapter, level, goal, stars ) and the endless record
        /// </summary>
        void RefreshMode()
        {
            if (levelNumber == null) return;

            ECCLevelCatalog catalog = ECCLevelCatalog.Instance;
            ECCLevelData level = catalog.SelectedLevel;

            int total = catalog.AllLevels.Count;
            int stars = 0;

            foreach (ECCLevelData data in catalog.AllLevels) stars += ECCSave.GetStars(data.id);

            bool levelsAvailable = ECCGameModeManager.IsModeAvailable(GameModeType.Levels) && level != null;

            playLevel.gameObject.SetActive(levelsAvailable);
            openMap.gameObject.SetActive(levelsAvailable);

            if (levelsAvailable)
            {
                string chapter = level.chapterIndex >= 0 && level.chapterIndex < catalog.chapters.Length ? catalog.chapters[level.chapterIndex].title : string.Empty;

                ECCRuntimeUI.SetText(chapterText, "فصل " + ECCUIKit.PersianDigits(level.chapterIndex + 1) + (string.IsNullOrEmpty(chapter) ? string.Empty : " · " + chapter));
                ECCRuntimeUI.SetText(levelTitle, level.title);
                ECCRuntimeUI.SetText(levelNumber, "مرحله‌ی " + level.NumberInChapter);
                ECCRuntimeUI.SetText(levelGoal, ECCPersianText.Wrap(level.ObjectiveDescription, 30));

                Transform oldIcon = levelBadge.transform.Find("Icon");
                if (oldIcon) Destroy(oldIcon.gameObject);

                ECCCarTheme.CreateIcon("Icon", levelBadge.transform, ObjectiveIcon(level.objective), new Vector2(0.5f, 0.5f), Vector2.zero, 74, Color.white);
            }
            else
            {
                ECCRuntimeUI.SetText(chapterText, "حالت مرحله‌ای");
                ECCRuntimeUI.SetText(levelTitle, string.Empty);
                ECCRuntimeUI.SetText(levelNumber, "به‌زودی");
                ECCRuntimeUI.SetText(levelGoal, string.Empty);
            }

            ECCRuntimeUI.SetText(starsText, ECCUIKit.PersianDigits(stars) + " / " + ECCUIKit.PersianDigits(total * 3) + " ستاره");

            // Endless: the record ( the endless high score key is the scene's, see ECCEndlessMode )
            int record = PlayerPrefs.GetInt(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "HighScore", 0);

            recordChip.SetActive(record > 0);
            endlessRecord.text = ECCWallet.Format(record);

            endlessButton.gameObject.SetActive(ECCGameModeManager.IsModeAvailable(GameModeType.Endless));
        }

        void OpenLevelSelect()
        {
            if (levelSelectCanvas || ECCModalTracker.AnyOpen) return;

            // The level map fills the screen, on a portrait canvas like the other new screens
            Canvas levelCanvas;
            RectTransform levelSafeArea = ECCUIKit.CreateSafeCanvas("CanvasMenuLevelSelect", 70, out levelCanvas);
            levelSelectCanvas = levelCanvas.gameObject;

            ECCLevelSelectUI.Open(levelSafeArea, delegate
            {
                if (levelSelectCanvas) Destroy(levelSelectCanvas);

                levelSelectCanvas = null;

                RefreshMode();
            });
        }

        /// <summary>
        /// The existing "More games" menu button opened the developer page in the browser. It now opens the
        /// «سایر بازی‌ها» screen ( which still links to that page when the game list is empty )
        /// </summary>
        static void RewireOtherGamesButton(Button button)
        {
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                button.onClick.SetPersistentListenerState(index, UnityEngine.Events.UnityEventCallState.Off);
            }

            button.onClick.AddListener(ECCOtherGamesUI.Open);

            Text label = button.GetComponentInChildren<Text>(true);

            if (label)
            {
                label.font = ECCRuntimeUI.DefaultFont;
                ECCRuntimeUI.SetText(label, "سایر بازی‌ها");
            }
        }

        void OnApplicationPause(bool paused)
        {
            // Coming back to the game on a new day counts as that day's login
            if (paused == false && canvas)
            {
                ECCLoginRewards.RegisterLogin();
                RefreshBadges();
            }
        }

        void OnDestroy()
        {
            ECCMissions.Changed -= RefreshBadges;
            ECCAchievements.Changed -= RefreshBadges;
            ECCWeeklyLeague.Changed -= OnLeagueChanged;

            if (instance == this) instance = null;
        }

        /// <summary>
        /// Shows a number badge ( hidden at 0 ). The tab pulses gently while the count is above 0
        /// </summary>
        public void SetBadge(string id, int count)
        {
            FeatureButton feature;

            if (features.TryGetValue(id, out feature) == false) return;

            feature.badge.gameObject.SetActive(count > 0);
            feature.badgeText.text = count.ToString();

            ECCPulse.Set(feature.button, count > 0, 0.05f);
        }

        /// <summary>
        /// Shows a small "available" dot, and a gentle pulse
        /// </summary>
        public void SetAvailable(string id, bool available)
        {
            FeatureButton feature;

            if (features.TryGetValue(id, out feature) == false) return;

            feature.dot.gameObject.SetActive(available);

            ECCPulse.Set(feature.button, available, 0.04f);
        }

        /// <summary>
        /// Refreshes the menu indicators after a daily feature changed ( wheel spun, challenge won, reward claimed )
        /// </summary>
        public static void RefreshIndicators()
        {
            if (instance) instance.RefreshBadges();
        }

        /// <summary>
        /// Refreshes every badge and indicator ( called by events, and when the menu appears )
        /// </summary>
        public void RefreshBadges()
        {
            SetBadge("missions", ECCMissions.ClaimableCount);
            SetBadge("achievements", ECCAchievements.ClaimableCount);

            // The challenge tab glows while today's challenge isn't won, or its 2X is still available
            SetAvailable("wheel", ECCLuckyWheel.IsAvailableToday);
            SetAvailable("login", ECCLoginRewards.CanClaimToday);

            SetAvailable("challenge", ECCDailyChallenge.IsCompletedToday == false || ECCDailyChallenge.IsDoubleUsedToday == false);
        }

        Button leagueButton;
        Text leagueRank;

        /// <summary>
        /// «لیگ هفتگی» across the two grid columns, a crown on the right and the rank in a chip on the left
        /// </summary>
        void BuildLeagueButton(float top)
        {
            float width = GridButtonWidth * 2 + 24;

            leagueButton = CandyButton(column, "League", "لیگ هفتگی", ECCWeeklyLeagueUI.LeagueColor, null, new Vector2(width, GridButtonHeight), 26);

            RectTransform rect = (RectTransform)leagueButton.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, top);

            Text label = ECCRuntimeUI.GetLabel(leagueButton);
            label.rectTransform.sizeDelta -= new Vector2(190, 0);
            label.rectTransform.anchoredPosition += new Vector2(95, 0);

            Image chip = ECCCarTheme.CreatePanel("RankChip", leagueButton.transform, new Vector2(0, 0.5f), new Vector2(18, 2), new Vector2(170, 60), new Color(0, 0, 0, 0.22f));
            chip.rectTransform.pivot = new Vector2(0, 0.5f);
            chip.raycastTarget = false;

            leagueRank = ECCRuntimeUI.CreateText("Rank", chip.transform, string.Empty, 26, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(160, 52), Color.white);
            leagueRank.resizeTextForBestFit = true;
            leagueRank.resizeTextMinSize = 14;
            leagueRank.resizeTextMaxSize = 26;

            leagueLock = ECCCarTheme.CreateIcon("Lock", chip.transform, "Icons/lock", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 38, Color.white);
            leagueLock.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            leagueButton.onClick.AddListener(OnLeagueTapped);

            RefreshLeague(false);
        }

        Image leagueLock;

        /// <summary>
        /// Locked: a small «باشه» info popup ( never over another popup ). Unlocked: the league screen
        /// </summary>
        void OnLeagueTapped()
        {
            if (ECCModalTracker.AnyOpen) return;

            if (ECCWeeklyLeague.IsUnlocked) ECCWeeklyLeagueUI.Open();
            else ECCWeeklyLeagueUI.OpenLocked();
        }

        /// <summary>
        /// Shows the current rank on the league button. On the visible menu it also counts as the rank the player saw
        /// </summary>
        void RefreshLeague(bool seen)
        {
            if (leagueRank == null) return;

            bool unlocked = ECCWeeklyLeague.IsUnlocked;

            // Locked: a lock instead of a rank, no numbers at all
            leagueLock.gameObject.SetActive(unlocked == false);
            leagueRank.gameObject.SetActive(unlocked);

            // A one-time attention pulse after the unlock, until the league screen was opened once
            ECCPulse.Set(leagueButton, unlocked && ECCWeeklyLeague.WasOpened == false, 0.05f);

            if (unlocked == false) return;

            int rank = ECCWeeklyLeague.Rank;

            ECCRuntimeUI.SetText(leagueRank, "رتبه " + ECCUIKit.PersianDigits(rank));

            if (seen)
            {
                ECCWeeklyLeague.MarkRankShown(rank);

                // A player unlocked by an update ( or whose unlocking win screen closed early ) gets the message here, once
                if (ECCWeeklyLeague.UnlockMessagePending && ECCWeeklyLeague.PendingUnlockLevel == null)
                {
                    ECCWeeklyLeague.MarkUnlockMessageShown();
                    ECCToast.Show(ECCWeeklyLeague.UnlockMessage, ECCWeeklyLeagueUI.LeagueColor);
                }
            }
        }

        void OnLeagueChanged()
        {
            RefreshLeague(false);
        }

        /// <summary>
        /// Called every time the main menu appears
        /// </summary>
        void OnMainMenuShown()
        {
            RefreshLeague(true);
            RefreshBadges();
            RefreshMode();

            if (popupFlowRunning == false) StartCoroutine(DailyPopupFlow());
        }

        bool popupFlowRunning = false;

        /// <summary>
        /// The automatic daily popups at a safe main-menu entry, one at a time, in priority order:
        /// 1. the daily challenge ( once per day, never on the install day )
        /// 2. the login reward calendar ( after the challenge popup was dealt with )
        /// The other-game recommendation is never shown here ( only after a normal level win ).
        /// </summary>
        IEnumerator DailyPopupFlow()
        {
            popupFlowRunning = true;

            // Let the menu settle first
            yield return new WaitForSecondsRealtime(0.6f);

            if (CanShowDailyPopup && ECCDailyChallenge.ShouldAutoShowPopup && ECCDailyChallenge.IsCompletedToday == false)
            {
                ECCChallengePopup.Open(gameController);

                // Recorded when it's actually on screen
                if (ECCChallengePopup.IsOpen) ECCDailyChallenge.MarkPopupShown();
            }

            while (ECCModalTracker.AnyOpen || ECCAds.IsBusy) yield return null;

            // 2. The login calendar, once a day while today's reward is waiting ( the reward stays claimable from the menu )
            yield return new WaitForSecondsRealtime(0.3f);

            if (CanShowDailyPopup && ECCLoginRewards.ShouldAutoShowPopup)
            {
                ECCLoginRewardsUI.Open();

                if (ECCLoginRewardsUI.IsOpen) ECCLoginRewards.MarkPopupShown();
            }

            while (ECCModalTracker.AnyOpen) yield return null;

            // 3. After a trial of a locked car: its buy offer, once
            if (ECCCarTrial.HasPendingOffer)
            {
                yield return new WaitForSecondsRealtime(0.3f);

                if (CanShowDailyPopup) ECCCarTrial.ShowOffer(gameController);

                while (ECCModalTracker.AnyOpen) yield return null;
            }

            RefreshBadges();

            popupFlowRunning = false;
        }

        /// <summary>
        /// True when a daily popup may open: the main menu is on screen and nothing else is open or loading
        /// </summary>
        bool CanShowDailyPopup
        {
            get { return IsMainMenuVisible && ECCModalTracker.AnyOpen == false && ECCAds.IsBusy == false && levelSelectCanvas == null; }
        }
    }

    /// <summary>
    /// Scales a rect down ( never up ) so its design height fits in its parent's height
    /// </summary>
    public class ECCFitHeight : MonoBehaviour
    {
        public float designHeight = 760;

        float lastParentHeight = -1;

        void Update()
        {
            RectTransform parent = transform.parent as RectTransform;

            if (parent == null) return;

            float height = parent.rect.height;

            if (Mathf.Approximately(height, lastParentHeight)) return;

            lastParentHeight = height;

            transform.localScale = Vector3.one * Mathf.Clamp(height / designHeight, 0.5f, 1);
        }
    }
}
