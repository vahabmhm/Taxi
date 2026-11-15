using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The «گردونه شانس» screen: coin balance, the wheel with its prize segments, the spin button, status and close.
    /// The wheel speeds up, then slows down smoothly and stops on the segment chosen by ECCLuckyWheel.Spin.
    /// Car theme: a night purple panel, and the wheel drawn as a tire with tread blocks, a chrome rim and a hub cap.
    /// </summary>
    public class ECCLuckyWheelUI : MonoBehaviour
    {
        const float WheelSize = 500;
        const float SpinDuration = 4.8f;
        const int FullTurns = 6;

        static ECCLuckyWheelUI open;

        RectTransform wheel;
        Button spinButton;
        Text statusText;
        bool spinning = false;

        static readonly Color[] SegmentColors =
        {
            ECCCarTheme.Hex(0xD85A30),
            ECCCarTheme.Hex(0x378ADD),
            ECCCarTheme.Hex(0x639922),
            ECCCarTheme.Hex(0x7F77DD),
        };

        const int TreadCount = 28;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            if (open) return;

            // A spin interrupted by closing the app is paid now ( once )
            int recovered = ECCLuckyWheel.PayPending();

            RectTransform panel = ECCUIKit.CreatePopup("CanvasLuckyWheel", 72, new Vector2(680, 900));

            // The shared CrowdRush card ( white )

            open = panel.gameObject.AddComponent<ECCLuckyWheelUI>();
            open.Build(panel);

            if (recovered > 0) ECCRewardFx.ShowCoins(recovered);
        }

        void Build(RectTransform panel)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 bottomCenter = new Vector2(0.5f, 0);
            Vector2 center = new Vector2(0.5f, 0.5f);

            ECCCoinBalanceButton coins = ECCCoinBalanceButton.Create(panel, new Vector2(0, 1), new Vector2(40, -14));
            ((RectTransform)coins.transform).pivot = new Vector2(0, 1);
            coins.transform.localScale = Vector3.one * 0.8f;

            ECCRuntimeUI.CreateText("Title", panel, "گردونه شانس", 40, topCenter, new Vector2(90, -48), new Vector2(300, 60), ECCCarTheme.Yellow);

            Button close = ECCUIKit.CreateCloseButton(panel);
            close.onClick.AddListener(Close);

            // The tire: a dark ring with tread blocks around it ( it doesn't turn, only the rim inside does )
            Image tire = ECCUIKit.CreateCircle("Tire", panel, center, new Vector2(0, 40), WheelSize + 84, ECCCarTheme.Asphalt);
            tire.raycastTarget = false;

            for (int index = 0; index < TreadCount; index++)
            {
                float angle = index * 360f / TreadCount;
                Vector2 direction = new Vector2(Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));

                Image tread = ECCRuntimeUI.CreateImage("Tread" + index, tire.transform, center, direction * (WheelSize + 84) * 0.5f, new Vector2(30, 18), ECCCarTheme.AsphaltLight);
                tread.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                tread.rectTransform.localEulerAngles = Vector3.forward * -angle;
                tread.raycastTarget = false;
            }

            // The chrome rim around the prize segments
            Image frame = ECCUIKit.CreateCircle("Frame", panel, center, new Vector2(0, 40), WheelSize + 26, ECCCarTheme.Hex(0xD3D1C7));
            frame.raycastTarget = false;

            wheel = ECCRuntimeUI.CreateRect("Wheel", frame.transform, center, Vector2.zero, new Vector2(WheelSize, WheelSize));

            ECCWheelPrize[] prizes = ECCLuckyWheel.Prizes;
            float segment = 360f / prizes.Length;

            for (int index = 0; index < prizes.Length; index++)
            {
                // Segment i covers [i, i+1) * segment degrees, clockwise from the top
                Image slice = ECCUIKit.CreateCircle("Segment" + index, wheel, center, Vector2.zero, WheelSize, SegmentColors[index % SegmentColors.Length]);
                slice.type = Image.Type.Filled;
                slice.fillMethod = Image.FillMethod.Radial360;
                slice.fillOrigin = (int)Image.Origin360.Top;
                slice.fillClockwise = true;
                slice.fillAmount = 1f / prizes.Length;
                slice.rectTransform.localEulerAngles = Vector3.forward * -index * segment;
                slice.raycastTarget = false;

                // The prize label, reading outwards along the middle of the segment
                float middle = (index + 0.5f) * segment * Mathf.Deg2Rad;
                Vector2 position = new Vector2(Mathf.Sin(middle), Mathf.Cos(middle)) * WheelSize * 0.33f;

                RectTransform label = ECCRuntimeUI.CreateRect("Prize" + index, wheel, center, position, new Vector2(120, 90));
                label.localEulerAngles = Vector3.forward * -(index + 0.5f) * segment;

                ECCUIKit.CreateNumberText("Amount", label, ECCWallet.Format(prizes[index].coins), 30, center, new Vector2(0, 16), new Vector2(120, 40), Color.white);
                ECCUIKit.CreateCoinIcon("Coin", label, center, new Vector2(0, -20), 34);
            }

            // The hub cap, with five lug nuts ( it turns with the wheel )
            Image hub = ECCUIKit.CreateCircle("Hub", wheel, center, Vector2.zero, 120, ECCCarTheme.Concrete);
            hub.raycastTarget = false;
            Image hubCap = ECCUIKit.CreateCircle("Cap", hub.transform, center, Vector2.zero, 92, ECCCarTheme.Hex(0xD3D1C7));
            hubCap.raycastTarget = false;

            for (int index = 0; index < 5; index++)
            {
                float angle = index * 72 * Mathf.Deg2Rad;

                Image nut = ECCUIKit.CreateCircle("Nut" + index, hubCap.transform, center, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 28, 16, ECCCarTheme.AsphaltLight);
                nut.raycastTarget = false;
            }

            ECCUIKit.CreateCoinIcon("HubCoin", hubCap.transform, center, Vector2.zero, 30);

            // The pointer at the top, pointing down into the wheel
            Image pointer = ECCRuntimeUI.CreateDiamond("Pointer", frame.transform, topCenter, new Vector2(0, -4), 46, ECCCarTheme.SirenRed);
            pointer.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Outline pointerOutline = pointer.gameObject.AddComponent<Outline>();
            pointerOutline.effectColor = new Color(0, 0, 0, 0.6f);

            statusText = ECCRuntimeUI.CreateText("Status", panel, string.Empty, 24, bottomCenter, new Vector2(0, 160), new Vector2(620, 40), ECCCarTheme.NightText);

            spinButton = ECCRuntimeUI.CreateButton("ButtonSpin", panel, "چرخاندن گردونه", 32, bottomCenter, new Vector2(0, 75), new Vector2(380, 90), ECCCarTheme.Yellow);
            ECCCarTheme.InkText(ECCRuntimeUI.GetLabel(spinButton), ECCCarTheme.YellowInk);

            Shadow spinLip = spinButton.GetComponent<Shadow>();
            if (spinLip) spinLip.effectColor = ECCCarTheme.YellowDark;
            ECCUIKit.KeepColorWhenDisabled(spinButton);
            spinButton.onClick.AddListener(StartSpin);

            // Show today's result if the wheel was already used
            if (ECCLuckyWheel.IsAvailableToday == false && ECCSave.Data.wheelResult >= 0 && ECCSave.Data.wheelResult < prizes.Length)
            {
                wheel.localEulerAngles = Vector3.forward * ECCLuckyWheel.AngleForSegment(ECCSave.Data.wheelResult, prizes.Length);
            }

            RefreshState();
        }

        void RefreshState()
        {
            bool available = ECCLuckyWheel.IsAvailableToday;

            spinButton.interactable = available && spinning == false;
            spinButton.targetGraphic.color = available ? ECCCarTheme.Yellow : ECCCarTheme.Concrete;
            ECCPulse.Set(spinButton, available && spinning == false, 0.05f);

            if (spinning) ECCRuntimeUI.SetText(statusText, "در حال چرخش...");
            else if (available) ECCRuntimeUI.SetText(statusText, "امروز یک چرخش رایگان داری!");
            else ECCRuntimeUI.SetText(statusText, "چرخش امروز انجام شد. فردا دوباره بچرخان!");
        }

        void StartSpin()
        {
            if (spinning) return;

            int result = ECCLuckyWheel.Spin();

            if (result < 0)
            {
                RefreshState();
                return;
            }

            spinning = true;

            RefreshState();

            StartCoroutine(SpinRoutine(result));
        }

        IEnumerator SpinRoutine(int result)
        {
            int count = ECCLuckyWheel.Prizes.Length;
            float segment = 360f / count;

            float start = Mathf.Repeat(wheel.localEulerAngles.z, 360);

            // Land inside the chosen segment, not always on its exact middle. Clockwise spin: the angle goes down
            float landing = ECCLuckyWheel.AngleForSegment(result, count) + Random.Range(-0.35f, 0.35f) * segment;
            float target = landing - 360 * FullTurns;

            while (target > start - 360 * FullTurns) target -= 360;

            for (float time = 0; time < SpinDuration; time += Time.unscaledDeltaTime)
            {
                float progress = time / SpinDuration;

                float eased = Ease(progress);

                wheel.localEulerAngles = Vector3.forward * Mathf.LerpUnclamped(start, target, eased);

                yield return null;
            }

            wheel.localEulerAngles = Vector3.forward * target;

            spinning = false;

            int paid = ECCLuckyWheel.PayPending();

            if (paid > 0)
            {
                ECCRewardFx.ShowCoins(paid);
                ECCToast.Show("برنده شدی!", ECCRuntimeUI.StarColor, ECCWallet.Format(paid) + " سکه");
            }

            RefreshState();
        }

        /// <summary>
        /// A short acceleration, then a long smooth slow-down ( the speed is continuous where the two parts meet )
        /// </summary>
        public static float Ease(float progress)
        {
            const float accelerate = 0.12f;
            const float joint = 3 * accelerate / (2 + accelerate);

            progress = Mathf.Clamp01(progress);

            if (progress < accelerate) return joint * progress * progress / (accelerate * accelerate);

            float u = (progress - accelerate) / (1 - accelerate);

            return joint + (1 - joint) * (1 - Mathf.Pow(1 - u, 3));
        }

        void Close()
        {
            // Closing during the spin is allowed: the saved prize is paid right away ( once )
            if (spinning)
            {
                int paid = ECCLuckyWheel.PayPending();

                if (paid > 0) ECCRewardFx.ShowCoins(paid);
            }

            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }
    }
}
