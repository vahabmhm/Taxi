using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The pause button and the pause screen in the CrowdRush look of the new UI.
    /// - the old ButtonPause on the game canvas becomes a purple candy button with a white pause icon ( same place,
    ///   same Pause(true) listener )
    /// - the old pause canvas stays the one that ECCGameController shows and hides, but its old art is hidden; while it's
    ///   active a white card is shown instead ( the shared ECCResultCard layout ): ribbon «توقف», a pause badge,
    ///   «ادامه‌ی بازی», «شروع دوباره», the same sound and music buttons as the main menu ( ECCAudioToggle ) and the
    ///   round red close button. The buttons call the old buttons' listeners, so restart and the sound / music
    ///   settings work exactly as before.
    /// Usage: ECCPauseMenu.Setup(gameController) from ECCGameController.Start
    /// </summary>
    public class ECCPauseMenu : MonoBehaviour
    {
        static readonly Color PausePurple = ECCCarTheme.Hex(0xA45CFF);

        ECCGameController gameController;
        Canvas canvas;
        Button oldRestart;
        Button oldSound;
        Button oldMusic;

        public static void Setup(ECCGameController gameController)
        {
            if (gameController.gameCanvas) StylePauseButton(gameController.gameCanvas);

            if (gameController.pauseCanvas && gameController.pauseCanvas.GetComponent<ECCPauseMenu>() == null)
            {
                ECCPauseMenu menu = gameController.pauseCanvas.gameObject.AddComponent<ECCPauseMenu>();
                menu.gameController = gameController;
                menu.Build();
            }
        }

        /// <summary>
        /// A purple candy button with a pause icon, in place of the old pause button art
        /// </summary>
        static void StylePauseButton(Transform gameCanvas)
        {
            foreach (Button button in gameCanvas.GetComponentsInChildren<Button>(true))
            {
                if (button.name != "ButtonPause") continue;

                Image face = button.targetGraphic as Image;

                if (face == null) continue;

                RectTransform rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(54, 54);

                face.color = PausePurple;
                ECCCarTheme.Chunky(button);

                // The game canvas is 600 units tall, so a smaller lip than on the 1000 unit screens
                foreach (Shadow shadow in button.GetComponents<Shadow>())
                {
                    if (shadow.GetType() == typeof(Shadow)) shadow.effectDistance = new Vector2(0, -5);
                }

                Transform shine = face.transform.Find("Shine");
                if (shine) ((RectTransform)shine).offsetMax = new Vector2(-4, -3);

                // A plain tint, no gray resting color
                ColorBlock colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1);
                button.colors = colors;

                if (face.transform.Find("PauseIcon") == null) CreatePauseIcon(face.transform, 26);
            }
        }

        /// <summary>
        /// Two rounded white bars with a soft shadow
        /// </summary>
        static RectTransform CreatePauseIcon(Transform parent, float size)
        {
            RectTransform icon = ECCRuntimeUI.CreateRect("PauseIcon", parent, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(size, size));

            float width = size * 0.3f;

            for (int side = -1; side <= 1; side += 2)
            {
                Image bar = ECCRuntimeUI.CreateImage(side < 0 ? "Left" : "Right", icon, new Vector2(0.5f, 0.5f), new Vector2(side * size * 0.22f, 0), new Vector2(width, size), Color.white);
                ECCCarTheme.Round(bar);
                bar.raycastTarget = false;

                // Corners about half the bar's width ( the rounded sprite's border is 24 pixels )
                bar.pixelsPerUnitMultiplier = 48 / width;

                Shadow shadow = bar.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.25f);
                shadow.effectDistance = new Vector2(0, -2);
            }

            return icon;
        }

        void Build()
        {
            // Keep the old buttons for their listeners, and hide the old art ( the canvases stop drawing and taking taps )
            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == "ButtonRestart") oldRestart = button;
                else if (button.name == "ButtonSound") oldSound = button;
                else if (button.name == "ButtonMusic") oldMusic = button;
            }

            foreach (Canvas oldCanvas in GetComponentsInChildren<Canvas>(true)) oldCanvas.enabled = false;

            canvas = ECCUIKit.CreateCanvas("CanvasPauseMenu", 60);

            const float width = 560;

            RectTransform panel = ECCRuntimeUI.CreateModal(canvas.transform, new Vector2(width, 400));
            panel.gameObject.AddComponent<ECCPopIn>();

            // The shared card layout: everything stacked with fixed gaps
            ECCResultCard card = ECCResultCard.On(panel, "توقف", PausePurple, width);

            Image badge = card.Badge(null, PausePurple, false, 112);
            CreatePauseIcon(badge.transform, 48);

            card.Line("بازی متوقف شده", 24, 36);

            Button resume = card.WideButton("ButtonResume", "ادامه‌ی بازی", ECCCarTheme.CandyGreen, "Icons/play", 96, 32);
            resume.onClick.AddListener(Resume);

            if (oldRestart)
            {
                Button restart = card.WideButton("ButtonRestart", "شروع دوباره", ECCCarTheme.CandyOrange, null, 80, 28);
                restart.onClick.AddListener(delegate { oldRestart.onClick.Invoke(); });
            }

            // The same sound and music buttons as the main menu, with their names under them
            if (oldSound || oldMusic)
            {
                card.Space(6);

                RectTransform row = card.CustomRow(128);

                if (oldSound) CreateAudioButton(row, oldSound, false, 150, "صدا");
                if (oldMusic) CreateAudioButton(row, oldMusic, true, 0, "موسیقی");

                CreateVibrationButton(row, -150);
            }

            Button close = ECCUIKit.CreateCloseButton(panel);
            close.onClick.AddListener(Resume);

            card.Finish();

            canvas.gameObject.SetActive(gameObject.activeInHierarchy);
        }

        static void CreateAudioButton(RectTransform row, Button oldButton, bool music, float x, string label)
        {
            const float size = 86;

            Vector2 top = new Vector2(0.5f, 1);

            ECCAudioToggle.Create(row, top, new Vector2(x, 0), size, oldButton, music);

            Text caption = ECCRuntimeUI.CreateText("Label", row, label, 22, top, new Vector2(x, -size - 6), new Vector2(150, 32), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(caption, ECCCarTheme.CardSubInk);
        }

        static void CreateVibrationButton(RectTransform row, float x)
        {
            const float size = 86;

            Vector2 top = new Vector2(0.5f, 1);

            ECCAudioToggle.CreateVibration(row, top, new Vector2(x, 0), size);

            Text caption = ECCRuntimeUI.CreateText("Label", row, "لرزش", 22, top, new Vector2(x, -size - 6), new Vector2(150, 32), ECCCarTheme.CardSubInk);
            ECCCarTheme.InkText(caption, ECCCarTheme.CardSubInk);
        }

        void Resume()
        {
            if (gameController && gameController.isPaused) gameController.Unpause();
        }

        void OnEnable()
        {
            if (canvas) canvas.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (canvas) canvas.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (canvas) Destroy(canvas.gameObject);
        }
    }
}
