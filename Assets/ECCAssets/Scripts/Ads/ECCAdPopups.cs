using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The shared pre-ad loading popup ( Task 10 ): shown by ECCAds before every rewarded video, for at least 2 seconds.
    /// It blocks taps on the UI behind it, so the button that started the ad can't be pressed again.
    /// </summary>
    public static class ECCAdLoadingPopup
    {
        /// <summary>
        /// The popup stays at least this long, even if the video is ready sooner
        /// </summary>
        public const float MinimumTime = 2;

        static GameObject open;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show()
        {
            if (open) return;

            Canvas canvas = ECCUIKit.CreateCanvas("CanvasAdLoading", 98);
            Object.DontDestroyOnLoad(canvas.gameObject);

            RectTransform panel = ECCRuntimeUI.CreateModal(canvas.transform, new Vector2(460, 300));
            panel.gameObject.AddComponent<ECCPopIn>();

            Vector2 center = new Vector2(0.5f, 0.5f);

            // A play icon inside a spinning ring
            Image ringBack = ECCUIKit.CreateCircle("RingBack", panel, center, new Vector2(0, 50), 120, new Color(1, 1, 1, 0.12f));
            ringBack.raycastTarget = false;

            Image ring = ECCUIKit.CreateCircle("Ring", panel, center, new Vector2(0, 50), 120, ECCRuntimeUI.AccentColor);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillAmount = 0.3f;
            ring.raycastTarget = false;
            ring.gameObject.AddComponent<ECCSpin>();

            Image hole = ECCUIKit.CreateCircle("Hole", panel, center, new Vector2(0, 50), 100, ECCRuntimeUI.PanelColor);
            hole.raycastTarget = false;

            Text play = ECCRuntimeUI.CreateText("Play", hole.transform, string.Empty, 46, center, new Vector2(4, 0), new Vector2(100, 100), Color.white);
            play.text = "▶";

            ECCRuntimeUI.CreateText("Message", panel, "ویدئو در حال بارگذاری...", 30, center, new Vector2(0, -60), new Vector2(440, 44), Color.white);
            ECCRuntimeUI.CreateText("Hint", panel, "چند لحظه صبر کن", 22, center, new Vector2(0, -102), new Vector2(440, 34), new Color(1, 1, 1, 0.65f));

            open = canvas.gameObject;
        }

        public static void Hide()
        {
            if (open) Object.Destroy(open);

            open = null;
        }
    }

    /// <summary>
    /// The shared NoAds panel, shown when no network can provide a video. No NoAds asset exists in this project
    /// ( and the older connectballs project isn't available ), so it's built from code in the game's popup style
    /// </summary>
    public static class ECCNoAdsPanel
    {
        static GameObject open;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Show()
        {
            if (open) return;

            RectTransform panel = ECCUIKit.CreatePopup("CanvasNoAds", 99, new Vector2(520, 340));
            Object.DontDestroyOnLoad(panel.root.gameObject);

            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);

            ECCRuntimeUI.CreateText("Title", panel, "تبلیغی موجود نیست", 36, topCenter, new Vector2(0, -50), new Vector2(480, 50), ECCRuntimeUI.AccentColor);
            ECCRuntimeUI.CreateText("Message", panel, "الان ویدئویی برای نمایش نداریم.\nلطفاً کمی بعد دوباره امتحان کن.", 24, topCenter, new Vector2(0, -135), new Vector2(480, 80), Color.white);

            Button ok = ECCRuntimeUI.CreateButton("ButtonOk", panel, "باشه", 28, bottomCenter, new Vector2(0, 55), new Vector2(200, 64), ECCRuntimeUI.ButtonColor);
            ok.onClick.AddListener(Hide);

            open = panel.root.gameObject;
        }

        public static void Hide()
        {
            if (open) Object.Destroy(open);

            open = null;
        }
    }
}
