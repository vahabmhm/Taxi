using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The banner that guides the player during a level: a white CrowdRush card that slides down from the top of the
    /// screen with a round colored badge ( an icon ), a bold title and a line of explanation, like «تا می‌تونی دلار جمع
    /// کن!» or «داری از مقصد دور می‌شی!». Several hints show one after another; a hint already waiting isn't queued twice.
    /// Doesn't block taps; works while paused ( unscaled time ).
    /// The in-run event messages ( ECCRunMessages: danger, crashes, police wrecked... ) use it too, with a priority:
    /// an Urgent hint cuts the current one short and goes first, a Minor one ( frequent bonuses ) never waits in the queue.
    /// Usage: ECCHintBanner.Show("Icons/target", ECCCarTheme.CandyOrange, title, text)
    /// </summary>
    public class ECCHintBanner : MonoBehaviour
    {
        public enum Kind
        {
            Normal = 0,
            Minor = 1,  // Dropped if anything waits; a showing Minor hint is replaced at once
            Urgent = 2, // Goes first and cuts the showing hint short
        }

        const float Width = 640;
        const float Height = 132;
        const float StayTime = 3;

        const float ShownY = -24;

        struct Hint
        {
            public string icon;
            public Color color;
            public string title;
            public string text;
            public float stay;
            public Kind kind;
            public Action onShown;
        }

        static ECCHintBanner instance;

        readonly Queue<Hint> queue = new Queue<Hint>();

        RectTransform card;
        Image stripe;
        Image badge;
        Image badgeShade;
        Image icon;
        Text titleText;
        Text bodyText;
        bool showing = false;
        string current;
        Kind currentKind;
        bool cutShort = false;

        /// <summary>
        /// Shows a hint. icon is a theme icon ( "Icons/target" ), or "dollar" for the dollar coin
        /// </summary>
        public static void Show(string icon, Color color, string title, string text, float stay = StayTime, Kind kind = Kind.Normal, Action onShown = null)
        {
            if (instance == null) Create();

            if (title == instance.current) return;

            foreach (Hint waiting in instance.queue)
            {
                if (waiting.title == title) return;
            }

            Hint hint = new Hint { icon = icon, color = color, title = title, text = text, stay = stay, kind = kind, onShown = onShown };

            if (kind == Kind.Minor)
            {
                // A frequent bonus never piles up: it only shows when nothing else waits, replacing another bonus at once
                if (instance.queue.Count > 0 || (instance.showing && instance.currentKind != Kind.Minor)) return;

                if (instance.showing) instance.cutShort = true;
            }
            else if (kind == Kind.Urgent)
            {
                // Goes before everything that waits, and the showing hint leaves now
                List<Hint> waiting = new List<Hint>(instance.queue);
                waiting.RemoveAll(other => other.kind == Kind.Minor);

                instance.queue.Clear();
                instance.queue.Enqueue(hint);

                foreach (Hint other in waiting) instance.queue.Enqueue(other);

                if (instance.showing) instance.cutShort = true;
                else instance.StartCoroutine(instance.ShowQueue());

                return;
            }
            else if (instance.showing && instance.currentKind == Kind.Minor)
            {
                instance.cutShort = true;
            }

            instance.queue.Enqueue(hint);

            if (instance.showing == false) instance.StartCoroutine(instance.ShowQueue());
        }

        /// <summary>
        /// The title of the hint on screen, or null
        /// </summary>
        public static string Current
        {
            get { return instance != null ? instance.current : null; }
        }

        static void Create()
        {
            RectTransform safe = ECCUIKit.CreateSafeCanvas("CanvasHintBanner", 92, out Canvas canvas);

            // Kept across scene changes would show a banner over the wrong screen: it belongs to this scene
            instance = canvas.gameObject.AddComponent<ECCHintBanner>();
            instance.Build(safe);
        }

        void Build(RectTransform safe)
        {
            Vector2 top = new Vector2(0.5f, 1);

            Image shadow = ECCRuntimeUI.CreateImage("Shadow", safe, top, Vector2.zero, new Vector2(Width + 40, Height + 40), new Color(0, 0, 0, 0.35f));
            shadow.sprite = ECCCarTheme.CardSprite("shadow");
            shadow.type = Image.Type.Sliced;
            shadow.raycastTarget = false;

            Image panel = ECCRuntimeUI.CreateImage("Banner", safe, top, Vector2.zero, new Vector2(Width, Height), Color.white);
            panel.sprite = ECCCarTheme.CardSprite("panel");
            panel.type = Image.Type.Sliced;
            panel.pixelsPerUnitMultiplier = 1.6f;
            panel.raycastTarget = false;
            card = panel.rectTransform;

            ECCFollowRect follow = shadow.gameObject.AddComponent<ECCFollowRect>();
            follow.target = card;
            follow.padding = new Vector4(20, 14, 20, 28);

            // A colored stripe on the right edge and the badge
            stripe = ECCRuntimeUI.CreateImage("Stripe", card, new Vector2(1, 0.5f), new Vector2(-14, 0), new Vector2(10, Height - 40), Color.white);
            stripe.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.Round(stripe);
            stripe.pixelsPerUnitMultiplier = 48 / 5f;
            stripe.raycastTarget = false;

            badgeShade = ECCUIKit.CreateCircle("BadgeShade", card, new Vector2(1, 0.5f), new Vector2(-40, -5), 92, Color.white);
            badgeShade.rectTransform.pivot = new Vector2(1, 0.5f);
            badgeShade.raycastTarget = false;

            badge = ECCUIKit.CreateCircle("Badge", card, new Vector2(1, 0.5f), new Vector2(-40, 0), 92, Color.white);
            badge.rectTransform.pivot = new Vector2(1, 0.5f);
            badge.raycastTarget = false;

            Vector2 center = new Vector2(0.5f, 0.5f);

            icon = ECCRuntimeUI.CreateImage("Icon", badge.transform, center, Vector2.zero, new Vector2(56, 56), Color.white);
            icon.rectTransform.pivot = center;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            float textRight = -150;
            float textWidth = Width - 150 - 36;

            titleText = ECCRuntimeUI.CreateText("Title", card, string.Empty, 30, new Vector2(1, 1), new Vector2(textRight, -20), new Vector2(textWidth, 44), ECCCarTheme.CardInk);
            titleText.rectTransform.pivot = new Vector2(1, 1);
            titleText.alignment = TextAnchor.MiddleRight;
            titleText.font = ECCCarTheme.TitleFont;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 18;
            titleText.resizeTextMaxSize = 30;

            bodyText = ECCRuntimeUI.CreateText("Text", card, string.Empty, 21, new Vector2(1, 1), new Vector2(textRight, -66), new Vector2(textWidth, 48), ECCCarTheme.CardSubInk);
            bodyText.rectTransform.pivot = new Vector2(1, 1);
            bodyText.alignment = TextAnchor.UpperRight;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.resizeTextForBestFit = true;
            bodyText.resizeTextMinSize = 14;
            bodyText.resizeTextMaxSize = 21;
            ECCCarTheme.InkText(bodyText, ECCCarTheme.CardSubInk);

            card.gameObject.SetActive(false);
        }

        void Apply(Hint hint)
        {
            stripe.color = hint.color;
            badge.color = hint.color;
            badgeShade.color = Color.Lerp(hint.color, Color.black, 0.3f);

            if (hint.icon == "dollar")
            {
                // The dollar coin fills the badge
                icon.sprite = ECCCoinBalanceButton.DollarSprite;
                icon.rectTransform.sizeDelta = new Vector2(84, 84);
            }
            else
            {
                icon.sprite = ECCCarTheme.Icon(hint.icon);
                icon.rectTransform.sizeDelta = new Vector2(54, 54);
            }

            Color ink = Color.Lerp(hint.color, ECCCarTheme.CardInk, 0.35f);

            ECCRuntimeUI.SetText(titleText, hint.title);
            ECCCarTheme.InkText(titleText, ink);

            ECCRuntimeUI.SetText(bodyText, hint.text);
        }

        IEnumerator ShowQueue()
        {
            showing = true;

            while (queue.Count > 0)
            {
                Hint hint = queue.Dequeue();
                current = hint.title;
                currentKind = hint.kind;
                cutShort = false;

                Apply(hint);

                card.gameObject.SetActive(true);

                if (hint.onShown != null) hint.onShown();

                float hidden = Height + 40;

                // Slide down with a small overshoot, stay, slide back up
                for (float time = 0; time < 0.35f; time += Time.unscaledDeltaTime)
                {
                    float progress = time / 0.35f;
                    float eased = 1 + 1.7f * Mathf.Pow(progress - 1, 3) + 0.7f * Mathf.Pow(progress - 1, 2);

                    card.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(hidden, ShownY, eased));

                    yield return null;
                }

                card.anchoredPosition = new Vector2(0, ShownY);

                for (float time = 0; time < hint.stay && cutShort == false; time += Time.unscaledDeltaTime) yield return null;

                // Leaves faster when another hint is pushing in
                float leaveTime = cutShort ? 0.12f : 0.25f;

                for (float time = 0; time < leaveTime; time += Time.unscaledDeltaTime)
                {
                    card.anchoredPosition = new Vector2(0, Mathf.Lerp(ShownY, hidden, time / leaveTime));

                    yield return null;
                }

                card.gameObject.SetActive(false);
                current = null;
            }

            showing = false;
        }

        /// <summary>
        /// Hides the banner at once and drops waiting hints ( the run is over )
        /// </summary>
        public static void Clear()
        {
            if (instance == null) return;

            instance.queue.Clear();
            instance.StopAllCoroutines();
            instance.card.gameObject.SetActive(false);
            instance.showing = false;
            instance.current = null;
            instance.cutShort = false;
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
