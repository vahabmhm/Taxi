using System;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// The level map, full screen in the CrowdRush look: a white card over the whole safe area with the orange
    /// «انتخاب مرحله» ribbon and the round close button, a chapter switcher ( «فصل ۷ از ۳۰», arrows on both sides ),
    /// a framed road map of the chapter ( a winding road from «شروع» at the bottom to the trophy at the top, the levels
    /// as stops along it, the ground tinted and decorated like the chapter's place; it scrolls inside the frame and opens
    /// at the selected level ), the details of
    /// the selected level and a green «تایید» button.
    /// Usage: ECCLevelSelectUI.Open(safeAreaOfAPortraitCanvas, onClose)
    /// </summary>
    public class ECCLevelSelectUI : MonoBehaviour
    {
        const float CardMargin = 16;
        // The road map: the distance between two stops, the margin above the first and below the last, and how far the
        // road swings to the sides
        const float StopSpacing = 128;
        const float MapMargin = 150;
        const float Swing = 185;

        static readonly Color FrameColor = ECCCarTheme.Hex(0xEEF1F6);
        static readonly Color Gold = ECCCarTheme.Hex(0xFFC83D);

        ECCLevelCatalog catalog;
        Action onClose;

        RectTransform card;
        RectTransform frame;
        RectTransform map;
        ScrollRect mapScroll;
        Text detailTitle;
        Text detailGoal;
        Image chapterPill;
        Text chapterTitle;
        Text chapterStars;
        ECCStarChestBar chestBar;
        Image chapterStar;
        Image chapterLock;
        Button previousChapter;
        Button nextChapter;

        int shownChapter;

        public static ECCLevelSelectUI Open(Transform safeArea, Action onClose)
        {
            ECCLevelSelectUI levelSelect = safeArea.gameObject.AddComponent<ECCLevelSelectUI>();
            levelSelect.onClose = onClose;
            levelSelect.Build((RectTransform)safeArea);

            return levelSelect;
        }

        void Build(RectTransform safeArea)
        {
            catalog = ECCLevelCatalog.Instance;

            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            // A full screen night background behind the safe area ( it also blocks taps to the menu )
            Canvas canvas = safeArea.GetComponentInParent<Canvas>();

            Image background = ECCRuntimeUI.CreateImage("Background", canvas.transform, center, Vector2.zero, Vector2.zero, ECCCarTheme.Night);
            ECCUIKit.Stretch(background.rectTransform);
            background.transform.SetAsFirstSibling();

            // The white card fills the safe area, under the ribbon
            Image shadow = ECCRuntimeUI.CreateImage("CardShadow", safeArea, center, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.35f));
            Stretch(shadow.rectTransform, CardMargin - 12, 92, CardMargin - 12, CardMargin - 20);
            shadow.sprite = ECCCarTheme.CardSprite("shadow");
            shadow.type = Image.Type.Sliced;
            shadow.raycastTarget = false;

            Image cardImage = ECCRuntimeUI.CreateImage("Card", safeArea, center, Vector2.zero, Vector2.zero, Color.white);
            Stretch(cardImage.rectTransform, CardMargin, 100, CardMargin, CardMargin);
            cardImage.sprite = ECCCarTheme.CardSprite("panel");
            cardImage.type = Image.Type.Sliced;
            cardImage.pixelsPerUnitMultiplier = 1.4f;
            card = cardImage.rectTransform;

            Image ribbon = ECCRuntimeUI.CreateImage("Ribbon", card, topCenter, new Vector2(0, 40), new Vector2(440, 84), ECCCarTheme.CandyOrange);
            ribbon.sprite = ECCCarTheme.CardSprite("ribbon");
            ribbon.type = Image.Type.Sliced;
            ribbon.pixelsPerUnitMultiplier = 1.4f;
            ribbon.raycastTarget = false;

            Text title = ECCRuntimeUI.CreateText("Title", ribbon.transform, "انتخاب مرحله", 34, center, new Vector2(0, 4), new Vector2(400, 60), Color.white);
            ECCCarTheme.CandyLabel(title);

            Button close = ECCUIKit.CreateCloseButton(card);
            close.onClick.AddListener(Close);

            // The portrait canvas ( ECCUIKit.CreateCanvas ) is always 720 units wide
            float cardWidth = 720 - CardMargin * 2;

            BuildChapterBar(cardWidth);

            // The chapter's star chests, between the chapter name and the map
            chestBar = ECCStarChestBar.Create(card, new Vector2(0, -136), cardWidth - 40);

            // The framed box with the levels
            Image frameImage = ECCRuntimeUI.CreateImage("Frame", card, center, Vector2.zero, Vector2.zero, FrameColor);
            Stretch(frameImage.rectTransform, 20, 232, 20, 250);
            frameImage.sprite = ECCCarTheme.CardSprite("panel");
            frameImage.type = Image.Type.Sliced;
            frameImage.pixelsPerUnitMultiplier = 2.4f;
            frame = frameImage.rectTransform;

            map = CreateScrollMap(frame, out mapScroll);

            // The selected level's details and the confirm button
            detailTitle = ECCRuntimeUI.CreateText("DetailTitle", card, string.Empty, 26, bottomCenter, new Vector2(0, 200), new Vector2(cardWidth - 40, 40), ECCCarTheme.CardInk);
            ECCCarTheme.InkText(detailTitle, ECCCarTheme.CardInk);
            detailTitle.font = ECCCarTheme.TitleFont;
            detailTitle.resizeTextForBestFit = true;
            detailTitle.resizeTextMinSize = 16;
            detailTitle.resizeTextMaxSize = 26;
            detailTitle.horizontalOverflow = HorizontalWrapMode.Wrap;

            detailGoal = ECCRuntimeUI.CreateText("DetailGoal", card, string.Empty, 22, bottomCenter, new Vector2(0, 158), new Vector2(cardWidth - 40, 36), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(detailGoal, ECCCarTheme.CardSubInk);
            detailGoal.resizeTextForBestFit = true;
            detailGoal.resizeTextMinSize = 14;
            detailGoal.resizeTextMaxSize = 22;
            detailGoal.horizontalOverflow = HorizontalWrapMode.Wrap;

            Button confirm = ECCRuntimeUI.CreateButton("ButtonConfirm", card, "تایید", 32, bottomCenter, new Vector2(0, 30), new Vector2(Mathf.Min(440, cardWidth - 60), 96), ECCCarTheme.CandyGreen);
            confirm.onClick.AddListener(Close);
            ECCCarTheme.CreateIcon("Icon", confirm.transform, "Icons/check", new Vector2(1, 0.5f), new Vector2(-50, 2), 44, Color.white);

            ECCLevelData selected = catalog.SelectedLevel;

            ShowChapter(selected != null ? selected.chapterIndex : 0);
        }

        static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>
        /// A vertical scroll view that fills the frame ( clipped to it ). Its content is the road map, sized by RefreshMap
        /// </summary>
        static RectTransform CreateScrollMap(RectTransform frame, out ScrollRect scroll)
        {
            RectTransform viewport = ECCRuntimeUI.CreateRect("Viewport", frame, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(viewport, 6, 6, 6, 6);
            viewport.gameObject.AddComponent<RectMask2D>();

            Image dragArea = viewport.gameObject.AddComponent<Image>();
            dragArea.color = new Color(0, 0, 0, 0.001f);

            RectTransform content = ECCRuntimeUI.CreateRect("Map", viewport, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);

            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            scroll.viewport = viewport;
            scroll.content = content;

            return content;
        }

        /// <summary>
        /// The chapter switcher under the ribbon: a pill in the place's color with «فصل ۷ از ۳۰ · شهر · رهگیرها» and
        /// the chapter's stars ( or a lock ), and round arrows: the previous chapter on the right, the next on the left
        /// ( right to left reading order )
        /// </summary>
        void BuildChapterBar(float cardWidth)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            const float arrowSize = 80;
            const float barHeight = 96;
            const float y = -82;

            float pillWidth = cardWidth - 40 - (arrowSize + 14) * 2;

            chapterPill = ECCRuntimeUI.CreateImage("ChapterPill", card, topCenter, new Vector2(0, y), new Vector2(pillWidth, barHeight), ECCCarTheme.CandyBlue);
            chapterPill.rectTransform.pivot = center;
            ECCCarTheme.Round(chapterPill);
            chapterPill.raycastTarget = false;

            Shadow lip = chapterPill.gameObject.AddComponent<Shadow>();
            lip.effectDistance = new Vector2(0, -6);

            ECCCandyShade shade = chapterPill.gameObject.AddComponent<ECCCandyShade>();
            shade.Init(chapterPill, lip);

            chapterTitle = ECCRuntimeUI.CreateText("Title", chapterPill.transform, string.Empty, 24, center, new Vector2(0, 16), new Vector2(pillWidth - 20, 40), Color.white);
            ECCCarTheme.CandyLabel(chapterTitle);
            chapterTitle.resizeTextForBestFit = true;
            chapterTitle.resizeTextMinSize = 14;
            chapterTitle.resizeTextMaxSize = 24;
            chapterTitle.horizontalOverflow = HorizontalWrapMode.Wrap;

            chapterStars = ECCRuntimeUI.CreateText("Stars", chapterPill.transform, string.Empty, 20, center, new Vector2(-14, -22), new Vector2(200, 30), Color.white);
            ECCCarTheme.CandyLabel(chapterStars);

            chapterStar = ECCCarTheme.CreateIcon("Star", chapterPill.transform, "Icons/star", center, new Vector2(30, -22), 26, Gold);
            chapterStar.rectTransform.pivot = center;

            chapterLock = ECCCarTheme.CreateIcon("Lock", chapterPill.transform, "Icons/lock", center, new Vector2(0, -22), 28, Color.white);
            chapterLock.rectTransform.pivot = center;

            float arrowX = pillWidth * 0.5f + 14 + arrowSize * 0.5f;

            previousChapter = CreateChapterArrow("ButtonPreviousChapter", new Vector2(arrowX, y), arrowSize, true, -1);
            nextChapter = CreateChapterArrow("ButtonNextChapter", new Vector2(-arrowX, y), arrowSize, false, 1);
        }

        Button CreateChapterArrow(string name, Vector2 position, float size, bool pointsRight, int direction)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Image shade = ECCUIKit.CreateCircle(name + "Shade", card, topCenter, position + new Vector2(0, -5), size, ECCCarTheme.Hex(0x7D8496));
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            Image face = ECCUIKit.CreateCircle(name, card, topCenter, position, size, ECCCarTheme.CandyGray);
            face.rectTransform.pivot = center;

            // The chevron icon points left
            Image icon = ECCCarTheme.CreateIcon("Icon", face.transform, "Icons/chevron", center, Vector2.zero, size * 0.5f, Color.white);
            icon.rectTransform.pivot = center;
            if (pointsRight) icon.rectTransform.localEulerAngles = new Vector3(0, 0, 180);

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(delegate { ShowChapter(shownChapter + direction); });
            face.gameObject.AddComponent<ECCPressScale>();

            return button;
        }

        static Color PlaceColor(ECCLevelTheme theme)
        {
            switch (theme)
            {
                case ECCLevelTheme.Desert: return ECCCarTheme.CandyOrange;
                case ECCLevelTheme.Forest: return ECCCarTheme.CandyGreen;
                case ECCLevelTheme.City: return ECCCarTheme.CandyBlue;
                case ECCLevelTheme.Rain: return ECCCarTheme.Hex(0x4C5BD4);
                case ECCLevelTheme.Highway: return ECCCarTheme.Hex(0x14B8A6);
                case ECCLevelTheme.Ruins: return ECCCarTheme.Hex(0xB5652B);
                default: return ECCCarTheme.Hex(0x8E5CF7);
            }
        }

        /// <summary>
        /// A chapter is open once its first level is unlocked
        /// </summary>
        bool IsChapterOpen(int chapter)
        {
            ECCLevelData[] levels = catalog.chapters[chapter].levels;

            return levels.Length > 0 && catalog.IsUnlocked(levels[0]);
        }

        void ShowChapter(int chapter)
        {
            int count = catalog.chapters.Length;

            if (count == 0) return;

            shownChapter = Mathf.Clamp(chapter, 0, count - 1);

            ECCChapterData data = catalog.chapters[shownChapter];
            bool open = IsChapterOpen(shownChapter);

            chapterPill.color = open ? PlaceColor(data.theme) : ECCCarTheme.CandyGray;

            ECCRuntimeUI.SetText(chapterTitle, "فصل " + ECCUIKit.PersianDigits(shownChapter + 1) + " از " + ECCUIKit.PersianDigits(count) + " · " + data.title);

            chapterStars.gameObject.SetActive(open);
            chapterStar.gameObject.SetActive(open);
            chapterLock.gameObject.SetActive(open == false);

            if (open)
            {
                chapterStars.text = ECCUIKit.PersianDigits(catalog.GetChapterStars(shownChapter)) + " / " + ECCUIKit.PersianDigits(data.levels.Length * 3);
                chapterStars.rectTransform.anchoredPosition = new Vector2(-14, -22);
                chapterStar.rectTransform.anchoredPosition = new Vector2(chapterStars.preferredWidth * 0.5f + 4, -22);
            }

            if (chestBar) chestBar.Show(shownChapter, open);

            SetArrow(previousChapter, shownChapter > 0);
            SetArrow(nextChapter, shownChapter < count - 1);

            RefreshMap();
        }

        static void SetArrow(Button arrow, bool available)
        {
            arrow.interactable = available;
            arrow.targetGraphic.color = available ? ECCCarTheme.CandyBlue : new Color(ECCCarTheme.CandyGray.r, ECCCarTheme.CandyGray.g, ECCCarTheme.CandyGray.b, 0.5f);

            Transform shade = arrow.transform.parent.Find(arrow.name + "Shade");
            if (shade) shade.gameObject.SetActive(available);
        }

        /// <summary>
        /// The ground color and the decorations' color of a place ( the road map's background )
        /// </summary>
        static void PlaceColors(ECCLevelTheme theme, out Color ground, out Color decor)
        {
            switch (theme)
            {
                case ECCLevelTheme.City: ground = ECCCarTheme.Hex(0xDCE8F5); decor = ECCCarTheme.Hex(0xB3C7E0); break;
                case ECCLevelTheme.Desert: ground = ECCCarTheme.Hex(0xF7E3B5); decor = ECCCarTheme.Hex(0xE9C27E); break;
                case ECCLevelTheme.Forest: ground = ECCCarTheme.Hex(0xD3EDC6); decor = ECCCarTheme.Hex(0x86C873); break;
                case ECCLevelTheme.Rain: ground = ECCCarTheme.Hex(0xCDD5EC); decor = ECCCarTheme.Hex(0x97A6D6); break;
                case ECCLevelTheme.Highway: ground = ECCCarTheme.Hex(0xD5EFE7); decor = ECCCarTheme.Hex(0x93D5C2); break;
                case ECCLevelTheme.Ruins: ground = ECCCarTheme.Hex(0xEADACB); decor = ECCCarTheme.Hex(0xC4A184); break;
                default: ground = ECCCarTheme.Hex(0xE6EAF1); decor = ECCCarTheme.Hex(0xC5CCD8); break;
            }
        }

        /// <summary>
        /// The chapter's road map: the tinted, decorated ground, a winding road with a dashed center line from «شروع» at
        /// the bottom to a trophy at the top, and the levels as round stops along it ( passed: green with their stars,
        /// open: blue, locked: gray with a lock, the last one a bigger red boss stop with a crown; the selected one orange,
        /// pulsing, with an «اینجا» pin )
        /// </summary>
        void RefreshMap()
        {
            for (int index = map.childCount - 1; index >= 0; index--)
            {
                GameObject child = map.GetChild(index).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            ECCChapterData chapter = catalog.chapters[shownChapter];
            ECCLevelData selected = catalog.SelectedLevel;
            ECCLevelData[] levels = chapter.levels;

            int count = Mathf.Max(1, levels.Length);
            float height = MapMargin * 2 + (count - 1) * StopSpacing;

            map.sizeDelta = new Vector2(0, height);

            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Color ground, decor;
            PlaceColors(chapter.theme, out ground, out decor);

            Image groundImage = ECCRuntimeUI.CreateImage("Ground", map, center, Vector2.zero, Vector2.zero, ground);
            ECCUIKit.Stretch(groundImage.rectTransform);

            // The stops, from the bottom ( level 1 ) up, swinging left and right
            Vector2[] stops = new Vector2[count];

            for (int index = 0; index < count; index++)
            {
                stops[index] = new Vector2(Swing * Mathf.Sin(index * 1.05f + 0.35f), -(height - MapMargin) + index * StopSpacing);
            }

            // The road through them, smoothed ( Catmull-Rom ), from a little below the first stop to a little above the last
            System.Collections.Generic.List<Vector2> road = new System.Collections.Generic.List<Vector2>();

            road.Add(stops[0] + new Vector2(0, -MapMargin * 0.8f));

            for (int index = 0; index < count - 1; index++)
            {
                Vector2 p0 = stops[Mathf.Max(0, index - 1)];
                Vector2 p1 = stops[index];
                Vector2 p2 = stops[index + 1];
                Vector2 p3 = stops[Mathf.Min(count - 1, index + 2)];

                for (int step = 0; step < 8; step++) road.Add(CatmullRom(p0, p1, p2, p3, step / 8f));
            }

            road.Add(stops[count - 1]);
            road.Add(stops[count - 1] + new Vector2(0, MapMargin * 0.55f));

            CreateDecorations(chapter.theme, decor, road, height);

            // The road: a dark edge, the asphalt, then the dashed center line
            DrawRoad(road, 62, ECCCarTheme.Hex(0x3A3F4E));
            DrawRoad(road, 48, ECCCarTheme.Hex(0x5C6273));

            for (int index = 0; index + 1 < road.Count; index += 2)
            {
                Vector2 a = road[index];
                Vector2 b = Vector2.Lerp(a, road[index + 1], 0.6f);

                CreateSegment("Dash", a, b, 5, new Color(1, 1, 1, 0.85f), false);
            }

            // «شروع» under the first stop, the trophy over the last
            CreateStartFlag(road[0]);

            Image trophy = ECCCarTheme.CreateIcon("Trophy", map, "Icons/trophy", top, road[road.Count - 1] + new Vector2(0, 26), 58, ECCCarTheme.Hex(0xFFB020));
            trophy.rectTransform.pivot = center;

            // The stops
            for (int index = 0; index < levels.Length; index++) CreateStop(levels[index], stops[index], index == levels.Length - 1, levels[index] == selected);

            ShowDetails(selected != null && selected.chapterIndex == shownChapter ? selected : null);

            // Open the map at the selected level ( or the first open one, or the start )
            float focus = stops[0].y;

            for (int index = 0; index < levels.Length; index++)
            {
                if (levels[index] == selected || (selected == null || selected.chapterIndex != shownChapter) && catalog.IsUnlocked(levels[index])) focus = stops[index].y;
            }

            StopAllCoroutines();
            StartCoroutine(ScrollTo(focus, height));
        }

        System.Collections.IEnumerator ScrollTo(float y, float height)
        {
            // The viewport's size is known once the layout ran
            yield return null;

            if (mapScroll == null) yield break;

            float view = mapScroll.viewport.rect.height;

            if (height <= view) yield break;

            // Put the focused stop in the middle of the view
            mapScroll.verticalNormalizedPosition = Mathf.Clamp01(1 - (-y - view * 0.5f) / (height - view));
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3);
        }

        void DrawRoad(System.Collections.Generic.List<Vector2> road, float width, Color color)
        {
            for (int index = 0; index + 1 < road.Count; index++) CreateSegment("Road", road[index], road[index + 1], width, color, true);
        }

        /// <summary>
        /// A straight piece from a to b. Rounded pieces ( a capsule ) overlap at the joints, so the road bends smoothly
        /// </summary>
        Image CreateSegment(string name, Vector2 a, Vector2 b, float width, Color color, bool rounded)
        {
            Vector2 delta = b - a;

            Image segment = ECCRuntimeUI.CreateImage(name, map, new Vector2(0.5f, 1), (a + b) * 0.5f, new Vector2(delta.magnitude + (rounded ? width : 0), width), color);
            segment.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            segment.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            segment.raycastTarget = false;

            if (rounded)
            {
                ECCCarTheme.Round(segment);
                segment.pixelsPerUnitMultiplier = 48 / width;
            }

            return segment;
        }

        /// <summary>
        /// Shapes of the place on the ground, away from the road: blocks of buildings in the city, dunes in the desert,
        /// trees in the forest, puddles on the rainy road, bushes by the highway, rubble in the ruins
        /// </summary>
        void CreateDecorations(ECCLevelTheme theme, Color color, System.Collections.Generic.List<Vector2> road, float height)
        {
            System.Random random = new System.Random(4271 + shownChapter * 31);

            float halfWidth = 300;

            int placed = 0;

            for (int tries = 0; tries < 160 && placed < 26; tries++)
            {
                float size = 34 + (float)random.NextDouble() * 46;
                Vector2 position = new Vector2(((float)random.NextDouble() * 2 - 1) * (halfWidth - size * 0.5f), -size * 0.5f - (float)random.NextDouble() * (height - size));

                bool clear = true;

                foreach (Vector2 point in road)
                {
                    if ((point - position).sqrMagnitude < (size * 0.5f + 52) * (size * 0.5f + 52))
                    {
                        clear = false;
                        break;
                    }
                }

                if (clear == false) continue;

                placed++;

                CreateDecoration(theme, color, position, size, random);
            }
        }

        void CreateDecoration(ECCLevelTheme theme, Color color, Vector2 position, float size, System.Random random)
        {
            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            Color dark = new Color(color.r * 0.82f, color.g * 0.82f, color.b * 0.82f, 1);

            switch (theme)
            {
                case ECCLevelTheme.City:
                case ECCLevelTheme.Ruins:
                {
                    // A block: a rounded square with a darker roof line ( tilted in the ruins )
                    Image block = ECCRuntimeUI.CreateImage("Block", map, top, position, new Vector2(size, size * 0.85f), color);
                    block.rectTransform.pivot = center;
                    ECCCarTheme.Round(block);
                    block.pixelsPerUnitMultiplier = 48 / (size * 0.18f);
                    block.raycastTarget = false;

                    if (theme == ECCLevelTheme.Ruins) block.rectTransform.localEulerAngles = new Vector3(0, 0, (float)random.NextDouble() * 30 - 15);

                    Image roof = ECCRuntimeUI.CreateImage("Roof", block.transform, new Vector2(0.5f, 1), new Vector2(0, -size * 0.12f), new Vector2(size * 0.7f, size * 0.12f), dark);
                    roof.rectTransform.pivot = center;
                    roof.raycastTarget = false;
                    break;
                }

                case ECCLevelTheme.Forest:
                case ECCLevelTheme.Highway:
                {
                    // A tree ( a round crown with a darker shadow under it )
                    Image shade = ECCUIKit.CreateCircle("TreeShade", map, top, position + new Vector2(0, -size * 0.08f), size, dark);
                    shade.rectTransform.pivot = center;
                    shade.raycastTarget = false;

                    Image crown = ECCUIKit.CreateCircle("Tree", map, top, position, size * 0.92f, color);
                    crown.rectTransform.pivot = center;
                    crown.raycastTarget = false;
                    break;
                }

                default:
                {
                    // A dune / puddle: a flat oval
                    Image oval = ECCUIKit.CreateCircle("Oval", map, top, position, size, color);
                    oval.rectTransform.pivot = center;
                    oval.rectTransform.sizeDelta = new Vector2(size * 1.5f, size * 0.7f);
                    oval.raycastTarget = false;
                    break;
                }
            }
        }

        void CreateStartFlag(Vector2 position)
        {
            Image pill = ECCRuntimeUI.CreateImage("Start", map, new Vector2(0.5f, 1), position, new Vector2(110, 40), ECCCarTheme.CandyGreen);
            pill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ECCCarTheme.Round(pill);
            pill.pixelsPerUnitMultiplier = 48 / 20f;
            pill.raycastTarget = false;

            Text label = ECCRuntimeUI.CreateText("Text", pill.transform, "شروع", 20, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(100, 36), Color.white);
            ECCCarTheme.CandyLabel(label);
        }

        /// <summary>
        /// A level's round stop on the road
        /// </summary>
        void CreateStop(ECCLevelData level, Vector2 position, bool boss, bool isSelected)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            bool unlocked = catalog.IsUnlocked(level);
            bool passed = ECCLevelProgress.IsPassed(level);

            float size = boss ? 108 : 92;

            Color color;

            if (unlocked == false) color = ECCCarTheme.CandyGray;
            else if (isSelected) color = ECCCarTheme.CandyOrange;
            else if (boss) color = ECCCarTheme.CandyRed;
            else if (passed) color = ECCCarTheme.CandyGreen;
            else color = ECCCarTheme.CandyBlue;

            RectTransform root = ECCRuntimeUI.CreateRect("Stop" + level.Number, map, new Vector2(0.5f, 1), position, new Vector2(size, size));
            root.pivot = center;

            Image shade = ECCUIKit.CreateCircle("Shade", root, center, new Vector2(0, -7), size, new Color(color.r * 0.68f, color.g * 0.68f, color.b * 0.68f, 1));
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            Image face = ECCUIKit.CreateCircle("Face", root, center, Vector2.zero, size, color);
            face.rectTransform.pivot = center;

            Image ring = ECCRuntimeUI.CreateImage("Ring", face.transform, center, Vector2.zero, new Vector2(size + 6, size + 6), Color.white);
            ring.rectTransform.pivot = center;
            ring.sprite = ECCCarTheme.Icon("Icons/ring");
            ring.raycastTarget = false;

            if (unlocked)
            {
                Text number = ECCRuntimeUI.CreateText("Number", face.transform, level.NumberInChapter, boss ? 40 : 36, center, new Vector2(0, boss ? -6 : 2), new Vector2(size, size * 0.7f), Color.white);
                ECCCarTheme.CandyLabel(number);
            }
            else
            {
                Image lockIcon = ECCCarTheme.CreateIcon("Lock", face.transform, "Icons/lock", center, Vector2.zero, size * 0.46f, new Color(1, 1, 1, 0.9f));
                lockIcon.rectTransform.pivot = center;
            }

            // The boss stop wears a crown
            if (boss)
            {
                Image crown = ECCCarTheme.CreateIcon("Crown", root, "Icons/crown", center, new Vector2(0, size * 0.5f + 4), 46, ECCCarTheme.Hex(0xFFC83D));
                crown.rectTransform.pivot = center;
            }

            // The stars of a passed level, on a small white pill under the stop
            if (passed)
            {
                int stars = ECCSave.GetStars(level.id);

                Image pill = ECCRuntimeUI.CreateImage("Stars", root, center, new Vector2(0, -size * 0.5f - 12), new Vector2(92, 32), Color.white);
                pill.rectTransform.pivot = center;
                ECCCarTheme.Round(pill);
                pill.pixelsPerUnitMultiplier = 48 / 16f;
                pill.raycastTarget = false;

                for (int star = 0; star < 3; star++)
                {
                    Image icon = ECCCarTheme.CreateIcon("Star" + star, pill.transform, "Icons/star", center, new Vector2((1 - star) * 26, 0), 24, star < stars ? Gold : new Color(ECCCarTheme.CardInk.r, ECCCarTheme.CardInk.g, ECCCarTheme.CardInk.b, 0.2f));
                    icon.rectTransform.pivot = center;
                }
            }

            if (isSelected)
            {
                // «اینجا»: a white pin over the selected stop
                Image pin = ECCRuntimeUI.CreateImage("Pin", root, center, new Vector2(0, size * 0.5f + (boss ? 84 : 40)), new Vector2(96, 40), Color.white);
                pin.rectTransform.pivot = center;
                ECCCarTheme.Round(pin);
                pin.pixelsPerUnitMultiplier = 48 / 20f;
                pin.raycastTarget = false;

                Image tip = ECCRuntimeUI.CreateImage("Tip", pin.transform, new Vector2(0.5f, 0), new Vector2(0, -4), new Vector2(16, 16), Color.white);
                tip.rectTransform.pivot = center;
                tip.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                tip.raycastTarget = false;

                Text here = ECCRuntimeUI.CreateText("Text", pin.transform, "اینجا", 20, center, new Vector2(0, 1), new Vector2(90, 36), ECCCarTheme.CandyOrangeShade);
                ECCCarTheme.InkText(here, ECCCarTheme.CandyOrangeShade);
                here.font = ECCCarTheme.TitleFont;

                Shadow pinShadow = pin.gameObject.AddComponent<Shadow>();
                pinShadow.effectColor = new Color(0, 0, 0, 0.2f);
                pinShadow.effectDistance = new Vector2(0, -3);

                ECCPulse.Set(face, true, 0.06f);
            }

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.interactable = unlocked;
            button.onClick.AddListener(delegate { Select(level); });
            root.gameObject.AddComponent<ECCPressScale>();
        }

        void Select(ECCLevelData level)
        {
            catalog.SelectedLevel = level;

            RefreshMap();
        }

        void ShowDetails(ECCLevelData level)
        {
            if (level == null)
            {
                ECCRuntimeUI.SetText(detailTitle, "یک مرحله انتخاب کن");
                ECCRuntimeUI.SetText(detailGoal, string.Empty);

                return;
            }

            ECCRuntimeUI.SetText(detailTitle, level.DisplayName + ": " + level.title + "  -  جایزه: " + ECCUIKit.PersianDigits(level.reward) + " دلار");
            ECCRuntimeUI.SetText(detailGoal, "هدف: " + level.ObjectiveDescription);
        }

        void Close()
        {
            if (onClose != null) onClose();

            Destroy(this);
        }
    }
}
