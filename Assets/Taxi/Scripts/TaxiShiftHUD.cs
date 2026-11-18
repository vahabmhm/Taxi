using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Modes;

namespace EndlessCarChase
{
    /// <summary>
    /// The endless shift's panel at the top of the screen: the shift clock ( a bar that turns red when it runs low ),
    /// the money of the shift and the passengers delivered. "+12 ثانیه" pops up when a delivery adds time
    /// </summary>
    public class TaxiShiftHUD : MonoBehaviour
    {
        ECCGameController gameController;
        ECCEndlessMode mode;
        Canvas canvas;

        Image clockFill;
        Text clockText;
        Text moneyText;
        Text passengersText;
        Text punchText;
        float punchTime;

        Image eventChip;
        Text eventText;

        const float BarWidth = 360;

        public static TaxiShiftHUD Create(ECCGameController controller, ECCEndlessMode mode)
        {
            TaxiShiftHUD hud = new GameObject("TaxiShiftHUD").AddComponent<TaxiShiftHUD>();
            hud.gameController = controller;
            hud.mode = mode;
            hud.Build();

            return hud;
        }

        void Build()
        {
            RectTransform safe = ECCUIKit.CreateSafeCanvas("CanvasTaxiShift", 44, out canvas);

            Vector2 top = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            RectTransform panel = ECCRuntimeUI.CreateRect("Shift", safe, top, mode.hudPosition, new Vector2(BarWidth + 60, 110));

            Image back = ECCRuntimeUI.CreateImage("Back", panel, center, Vector2.zero, new Vector2(BarWidth + 60, 110), new Color(0.06f, 0.05f, 0.14f, 0.78f));
            back.raycastTarget = false;
            ECCCarTheme.Round(back);

            clockText = ECCRuntimeUI.CreateText("Clock", panel, string.Empty, 26, top, new Vector2(0, -22), new Vector2(BarWidth, 34), Color.white);

            Image track = ECCRuntimeUI.CreateImage("Track", panel, top, new Vector2(0, -50), new Vector2(BarWidth, 12), new Color(1, 1, 1, 0.15f));
            clockFill = ECCRuntimeUI.CreateImage("Fill", track.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(BarWidth, 12), ECCRuntimeUI.GoodColor);
            clockFill.rectTransform.pivot = new Vector2(0, 0.5f);

            moneyText = ECCRuntimeUI.CreateText("Money", panel, string.Empty, 24, top, new Vector2(-90, -82), new Vector2(200, 32), TaxiColors.PlayerYellow);
            passengersText = ECCRuntimeUI.CreateText("Passengers", panel, string.Empty, 24, top, new Vector2(100, -82), new Vector2(200, 32), Color.white);

            punchText = ECCRuntimeUI.CreateText("Punch", panel, string.Empty, 30, top, new Vector2(0, -130), new Vector2(300, 40), ECCRuntimeUI.GoodColor);

            // The city event and its time left, under the clock
            eventChip = ECCRuntimeUI.CreateImage("Event", panel, top, new Vector2(0, -130), new Vector2(330, 34), Color.white);
            eventChip.raycastTarget = false;
            ECCCarTheme.Round(eventChip);

            eventText = ECCRuntimeUI.CreateText("Text", eventChip.transform, string.Empty, 20, center, Vector2.zero, new Vector2(320, 34), Color.white);
            eventChip.gameObject.SetActive(false);
        }

        void UpdateEvent()
        {
            TaxiCityEvents events = TaxiCityEvents.Instance;

            bool active = events && events.Active != TaxiCityEventType.None;

            eventChip.gameObject.SetActive(active);

            // The "+12 ثانیه" punch goes under the event chip while there is one
            punchText.rectTransform.anchoredPosition = new Vector2(0, active ? -172 : -130);

            if (active == false) return;

            Color color = TaxiCityEvents.GetColor(events.Active);

            eventChip.color = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, 0.9f);

            ECCRuntimeUI.SetText(eventText, TaxiCityEvents.GetName(events.Active) + ": " + ECCUIKit.PersianDigits(Mathf.CeilToInt(events.TimeLeft)) + " ثانیه");
        }

        public void Punch(string text)
        {
            ECCRuntimeUI.SetText(punchText, text);
            punchTime = 1.4f;
        }

        public void Hide()
        {
            if (canvas) canvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (canvas) Destroy(canvas.gameObject);
        }

        void Update()
        {
            if (mode == null || gameController == null) return;

            if (canvas) canvas.gameObject.SetActive(gameController.gameStarted && gameController.isGameOver == false);

            float left = mode.TimeLeft;
            float fraction = mode.LevelProgress;

            clockFill.rectTransform.sizeDelta = new Vector2(BarWidth * fraction, 12);
            clockFill.color = left <= 15 ? Color.Lerp(ECCRuntimeUI.BadColor, Color.white, Mathf.PingPong(Time.time * 3, 0.5f)) : (left <= 30 ? ECCRuntimeUI.AccentColor : ECCRuntimeUI.GoodColor);

            ECCRuntimeUI.SetText(clockText, "وقت شیفت: " + ECCUIKit.PersianDigits(Mathf.CeilToInt(left)) + " ثانیه");

            ECCRuntimeUI.SetText(moneyText, "درآمد: " + ECCUIKit.PersianDigits(Mathf.Max(0, gameController.score)));

            TaxiSession session = TaxiSession.Instance;

            ECCRuntimeUI.SetText(passengersText, "مسافر: " + ECCUIKit.PersianDigits(session ? session.PlayerDeliveries : 0));

            UpdateEvent();

            if (punchTime > 0)
            {
                punchTime -= Time.deltaTime;

                punchText.color = new Color(punchText.color.r, punchText.color.g, punchText.color.b, Mathf.Clamp01(punchTime * 2));

                if (punchTime <= 0) ECCRuntimeUI.SetText(punchText, string.Empty);
            }
        }
    }
}
