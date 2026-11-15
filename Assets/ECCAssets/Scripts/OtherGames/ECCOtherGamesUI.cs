using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// «سایر بازی‌ها»: a two-column, vertically scrolling grid of the developer's games ( right to left ).
    /// Each card shows the Cafe Bazaar icon ( or a placeholder ), the title, a short description, and opens the listing.
    /// With an empty catalog it explains that the list is coming and links to the developer page instead.
    /// </summary>
    public class ECCOtherGamesUI : MonoBehaviour
    {
        const float PanelWidth = 680;
        const float ListHeight = 660;
        const float CardWidth = 270;
        const float CardHeight = 316;

        static readonly Color Teal = ECCCarTheme.Hex(0x14B8A6);

        static readonly Color TileColor = ECCCarTheme.Hex(0xF1F4F9);
        static readonly Color TileShade = ECCCarTheme.Hex(0xD3D9E4);

        static ECCOtherGamesUI open;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            if (open) return;

            List<ECCGameEntry> games = ECCGameCatalog.Instance.EligibleGames;

            ECCResultCard card = ECCPopupKit.Create("CanvasOtherGames", 72, "سایر بازی‌ها", Teal, "Icons/play", games.Count > 0 ? "بازی‌های دیگه‌ی ما در کافه‌بازار؛ روی هر کدوم بزن تا صفحه‌اش باز بشه" : string.Empty, false, delegate { if (open) open.Close(); }, PanelWidth, 96);

            open = card.Panel.gameObject.AddComponent<ECCOtherGamesUI>();
            open.Build(card, games);

            card.Finish(false);
        }

        void Build(ECCResultCard card, List<ECCGameEntry> games)
        {
            if (games.Count == 0)
            {
                card.InfoBox("Icons/play", Teal, "فهرست بازی‌های ما به‌زودی اینجا اضافه می‌شه. تا اون موقع همه‌ی بازی‌ها رو در کافه‌بازار ببین.", 96);

                Button developer = card.WideButton("ButtonDeveloper", "دیدن بازی‌ها در کافه‌بازار", ECCCarTheme.CandyGreen, "Icons/play", 92, 30);
                developer.onClick.AddListener(delegate { ECCStoreLinks.OpenUrl(ECCGameCatalog.Instance.developerPageUrl); });

                return;
            }

            // Scroll view under the header, with the scrollbar on the left ( the end side in RTL )
            RectTransform scrollArea = card.CustomRow(ListHeight);

            ScrollRect scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;

            RectTransform viewport = ECCRuntimeUI.CreateRect("Viewport", scrollArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(24, 0);
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            Image dragArea = viewport.gameObject.AddComponent<Image>();
            dragArea.color = new Color(0, 0, 0, 0.001f);

            RectTransform content = ECCRuntimeUI.CreateRect("Content", viewport, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);

            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardWidth, CardHeight);
            grid.spacing = new Vector2(16, 18);
            grid.padding = new RectOffset(6, 6, 8, 20);
            grid.startCorner = GridLayoutGroup.Corner.UpperRight;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = CreateScrollbar(scrollArea);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            foreach (ECCGameEntry game in games) CreateCard(content, game);
        }

        internal static Scrollbar CreateScrollbar(RectTransform scrollArea)
        {
            Image track = ECCRuntimeUI.CreateImage("Scrollbar", scrollArea, new Vector2(0, 0.5f), Vector2.zero, new Vector2(14, 0), new Color(1, 1, 1, 0.1f));
            track.rectTransform.anchorMin = new Vector2(0, 0);
            track.rectTransform.anchorMax = new Vector2(0, 1);
            track.rectTransform.pivot = new Vector2(0, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(14, 0);

            RectTransform slidingArea = ECCRuntimeUI.CreateRect("SlidingArea", track.transform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ECCUIKit.Stretch(slidingArea);

            Image handle = ECCRuntimeUI.CreateImage("Handle", slidingArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ECCRuntimeUI.AccentColor);
            ECCUIKit.Stretch(handle.rectTransform);

            Scrollbar scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;

            return scrollbar;
        }

        static void CreateCard(RectTransform content, ECCGameEntry game)
        {
            // A light gray tile on the white card, with dark ink texts ( the card styling skips texts inside buttons,
            // so the tile sets its own colors and is marked as already styled )
            Image card = ECCRuntimeUI.CreateImage("Game_" + game.packageName, content, new Vector2(0.5f, 1), Vector2.zero, new Vector2(CardWidth, CardHeight), TileColor);
            ECCCarTheme.Round(card);
            card.gameObject.AddComponent<ECCKeepColors>();
            card.gameObject.AddComponent<ECCCandyShade>();

            Shadow lip = card.gameObject.AddComponent<Shadow>();
            lip.effectColor = TileShade;
            lip.effectDistance = new Vector2(0, -5);

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card;

            string package = game.packageName;
            button.onClick.AddListener(delegate { ECCStoreLinks.OpenBazaarListing(package); });

            // A soft teal halo behind the icon
            Image halo = ECCUIKit.CreateCircle("Halo", card.transform, new Vector2(0.5f, 1), new Vector2(0, -8), 158, new Color(Teal.r, Teal.g, Teal.b, 0.16f));
            halo.rectTransform.pivot = new Vector2(0.5f, 1);
            halo.raycastTarget = false;

            CreateGameIcon(card.transform, game, new Vector2(0.5f, 1), new Vector2(0, -16), 138);

            Text title = ECCRuntimeUI.CreateText("Title", card.transform, game.title, 24, new Vector2(0.5f, 1), new Vector2(0, -174), new Vector2(CardWidth - 20, 40), ECCCarTheme.CardInk);
            ECCCarTheme.InkText(title, ECCCarTheme.CardInk);
            title.font = ECCCarTheme.TitleFont;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 24;

            if (string.IsNullOrEmpty(game.description) == false)
            {
                Text description = ECCRuntimeUI.CreateText("Description", card.transform, game.description, 17, new Vector2(0.5f, 1), new Vector2(0, -226), new Vector2(CardWidth - 24, 52), ECCCarTheme.CardSubInk);
                ECCCarTheme.InkText(description, ECCCarTheme.CardSubInk);
                description.horizontalOverflow = HorizontalWrapMode.Wrap;
                description.verticalOverflow = VerticalWrapMode.Truncate;
                description.resizeTextForBestFit = true;
                description.resizeTextMinSize = 12;
                description.resizeTextMaxSize = 17;
            }

            // The call to action at the bottom of the card
            ECCPopupKit.Chip(card.transform, "Install", "دریافت از کافه‌بازار", ECCCarTheme.CandyGreen, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(CardWidth - 40, 34), 16);
        }

        /// <summary>
        /// The game's icon, or a neutral placeholder with the first letter of its name when no icon was imported
        /// </summary>
        public static Image CreateGameIcon(Transform parent, ECCGameEntry game, Vector2 anchor, Vector2 position, float size)
        {
            Image icon = ECCRuntimeUI.CreateImage("Icon", parent, anchor, position, new Vector2(size, size), Color.white);
            icon.rectTransform.pivot = new Vector2(0.5f, anchor.y);
            icon.raycastTarget = false;

            Sprite known = game.icon ? game.icon : ECCGameIcons.GetCached(game);

            if (known)
            {
                icon.sprite = known;
                icon.preserveAspect = true;

                return icon;
            }

            // A placeholder with the first letter of the name, replaced when the icon arrives
            icon.color = new Color(0.3f, 0.3f, 0.4f, 1);

            string letter = string.IsNullOrEmpty(game.title) ? "?" : game.title.Substring(0, 1);

            Text placeholder = ECCRuntimeUI.CreateText("Placeholder", icon.transform, letter, Mathf.RoundToInt(size * 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), new Color(1, 1, 1, 0.8f));

            ECCGameIcons.Load(game, delegate (Sprite sprite)
            {
                if (icon == null || sprite == null) return;

                icon.sprite = sprite;
                icon.color = Color.white;
                icon.preserveAspect = true;

                if (placeholder) placeholder.gameObject.SetActive(false);
            });

            return icon;
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
