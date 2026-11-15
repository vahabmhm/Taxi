using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The health ( mana ) bar that floats above every car, the player's included.
    /// Green while healthy, yellow when worn down, red near destruction. On the last stretch it flashes, pulses and shows a warning sign.
    /// It lives on its own object so the car's size and turning don't stretch or spin it, and it removes itself with its car
    /// </summary>
    public class ECCCarHealthBar : MonoBehaviour
    {
        // Parts of the max health below which the bar turns yellow, red, and starts warning
        public static float yellowThreshold = 0.6f;
        public static float redThreshold = 0.3f;
        public static float warningThreshold = 0.2f;

        static readonly Color GreenColor = new Color(0.3f, 0.9f, 0.3f, 1);
        static readonly Color YellowColor = new Color(1, 0.85f, 0.15f, 1);
        static readonly Color RedColor = new Color(1, 0.2f, 0.15f, 1);
        static readonly Color FrameColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);

        // Size of the bar in world units
        const float BarWidth = 1.3f;
        const float BarHeight = 0.16f;
        const float UnitsToPixels = 100;

        ECCCar car;
        Transform cameraTransform;
        Canvas canvas;
        Image frame;
        Image fill;
        RectTransform warning;
        Image warningCircle;

        // How much bigger the bars are drawn ( the chase camera is far away )
        public static float PlayerBarScale = 2.2f;
        public static float OtherBarScale = 1.5f;

        float heightAboveCar = 1.4f;
        float shownHealth = -1;
        float shownMax = -1;
        bool warningOn = false;

        /// <summary>
        /// Creates the bar for a car. Call it after the car's max health is set
        /// </summary>
        public static ECCCarHealthBar Attach(ECCCar car)
        {
            GameObject barObject = new GameObject("ManaBar_" + car.name, typeof(RectTransform));

            ECCCarHealthBar bar = barObject.AddComponent<ECCCarHealthBar>();
            bar.car = car;
            bar.Build();

            return bar;
        }

        void Build()
        {
            // Sit clearly above the roof of the car, whatever its size. Measured from the car's meshes: the renderer bounds
            // are wrong on the first frame, while the spawn animation squashes the car flat ( the bar then sank into the car )
            heightAboveCar = Mathf.Max(0.5f, car.TopHeight) + 0.8f;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 5;

            RectTransform root = (RectTransform)transform;
            root.sizeDelta = new Vector2(BarWidth, BarHeight) * UnitsToPixels;
            // Readable from the far chase camera: the player's bar is the biggest
            root.localScale = Vector3.one / UnitsToPixels * (car.IsPlayer ? PlayerBarScale : OtherBarScale);

            frame = CreateImage("Frame", root, new Vector2(BarWidth * UnitsToPixels + 8, BarHeight * UnitsToPixels + 8), FrameColor);

            fill = CreateImage("Fill", frame.transform, new Vector2(BarWidth, BarHeight) * UnitsToPixels, GreenColor);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // The warning sign: a red disc with "!" above the bar, shown only near destruction
            warningCircle = CreateImage("Warning", root, new Vector2(34, 34), RedColor);
            warningCircle.sprite = ECCUIKit.CircleSprite;
            warning = warningCircle.rectTransform;
            warning.anchoredPosition = new Vector2(0, BarHeight * UnitsToPixels + 18);

            GameObject markObject = new GameObject("Mark", typeof(RectTransform));
            markObject.transform.SetParent(warning, false);

            Text mark = markObject.AddComponent<Text>();
            mark.font = ECCRuntimeUI.DefaultFont;
            mark.text = "!";
            mark.fontSize = 28;
            mark.fontStyle = FontStyle.Bold;
            mark.alignment = TextAnchor.MiddleCenter;
            mark.color = Color.white;
            mark.raycastTarget = false;
            ((RectTransform)markObject.transform).sizeDelta = new Vector2(34, 34);

            warning.gameObject.SetActive(false);

            Refresh();
            LateUpdate();
        }

        static Image CreateImage(string name, Transform parent, Vector2 size, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.sprite = ECCUIKit.WhiteSprite;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;

            return image;
        }

        /// <summary>
        /// The bar color for a part of the max health
        /// </summary>
        public static Color ColorFor(float healthPart)
        {
            if (healthPart <= redThreshold) return RedColor;
            if (healthPart <= yellowThreshold) return YellowColor;

            return GreenColor;
        }

        void Refresh()
        {
            shownHealth = car.health;
            shownMax = car.healthMax;

            float part = shownMax > 0 ? Mathf.Clamp01(shownHealth / shownMax) : 0;

            fill.fillAmount = part;
            fill.color = ColorFor(part);

            // The player is warned as soon as the bar turns red
            bool shouldWarn = shownHealth > 0 && part <= (car.IsPlayer ? Mathf.Max(warningThreshold, redThreshold) : warningThreshold);

            if (shouldWarn != warningOn)
            {
                warningOn = shouldWarn;

                warning.gameObject.SetActive(warningOn);

                // A red pulsing frame around the screen while the player's car is in danger
                if (car.IsPlayer) ECCDangerFrame.Set(warningOn);

                if (warningOn == false)
                {
                    frame.rectTransform.localScale = Vector3.one;
                    frame.color = FrameColor;
                }

                // Tell the player once each time their car drops into danger
                if (warningOn && car.IsPlayer) ECCRunMessages.CarInDanger();
            }
        }

        void LateUpdate()
        {
            // The car is gone ( destroyed or exploded ), so remove its bar too
            if (car == null)
            {
                if (warningOn) ECCDangerFrame.Set(false);

                Destroy(gameObject);
                return;
            }

            ECCGameController gameController = ECCGameController.Instance;

            // Only show bars during a run, never in the garage or menus
            bool visible = car.gameObject.activeInHierarchy && car.health > 0 && gameController && gameController.gameStarted;

            if (canvas.enabled != visible) canvas.enabled = visible;

            if (visible == false)
            {
                if (warningOn && car.IsPlayer) ECCDangerFrame.Set(false);

                return;
            }

            if (warningOn && car.IsPlayer) ECCDangerFrame.Set(true);

            if (car.health != shownHealth || car.healthMax != shownMax) Refresh();

            transform.position = car.transform.position + Vector3.up * heightAboveCar;

            if (cameraTransform == null && Camera.main) cameraTransform = Camera.main.transform;

            // Always face the camera
            if (cameraTransform) transform.rotation = cameraTransform.rotation;

            if (warningOn)
            {
                // Unscaled, so it keeps pulsing during the slow motion of a crash
                float wave = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7));

                frame.rectTransform.localScale = Vector3.one * (1 + 0.15f * wave);
                frame.color = Color.Lerp(FrameColor, new Color(1, 0.3f, 0.3f, 0.9f), wave);
                fill.color = Color.Lerp(RedColor, Color.white, wave * 0.6f);

                warning.localScale = Vector3.one * (1 + 0.3f * wave);
                warningCircle.color = new Color(RedColor.r, RedColor.g, RedColor.b, 0.5f + 0.5f * wave);
            }
        }
    }

    /// <summary>
    /// A red frame that pulses around the screen edges while the player's car is in danger ( its bar is red )
    /// </summary>
    public class ECCDangerFrame : MonoBehaviour
    {
        static ECCDangerFrame instance;

        readonly Image[] edges = new Image[4];
        bool on = false;

        public static void Set(bool show)
        {
            if (instance == null)
            {
                if (show == false) return;

                Canvas canvas = ECCUIKit.CreateCanvas("CanvasDangerFrame", 45);
                instance = canvas.gameObject.AddComponent<ECCDangerFrame>();
                instance.Build(canvas.transform);
            }

            if (instance.on == show) return;

            instance.on = show;
            instance.gameObject.SetActive(show);
        }

        void Build(Transform parent)
        {
            // Bottom, top, left, right
            Vector2[] anchorsMin = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(1, 0) };
            Vector2[] anchorsMax = { new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), new Vector2(1, 1) };
            Vector2[] pivots = { new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0, 0.5f), new Vector2(1, 0.5f) };
            Vector2[] sizes = { new Vector2(0, 46), new Vector2(0, 46), new Vector2(46, 0), new Vector2(46, 0) };

            for (int index = 0; index < 4; index++)
            {
                Image edge = ECCRuntimeUI.CreateImage("Edge" + index, parent, pivots[index], Vector2.zero, sizes[index], new Color(1, 0.1f, 0.1f, 0.5f));
                edge.rectTransform.anchorMin = anchorsMin[index];
                edge.rectTransform.anchorMax = anchorsMax[index];
                edge.rectTransform.pivot = pivots[index];
                edge.rectTransform.sizeDelta = sizes[index];
                edge.raycastTarget = false;

                edges[index] = edge;
            }
        }

        void Update()
        {
            // Unscaled, so it keeps pulsing during slow motion
            float wave = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6));

            foreach (Image edge in edges) edge.color = new Color(1, 0.1f, 0.1f, 0.15f + 0.4f * wave);
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
