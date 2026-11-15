using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Modes;

namespace EndlessCarChase
{
    /// <summary>
    /// Shows the wanted level of the endless mode: a row of stars, the score multiplier,
    /// a progress bar towards the next level, and a short announcement when the level rises.
    /// It's built from code, so no prefab or scene setup is needed.
    /// </summary>
    public class ECCWantedHUD : MonoBehaviour
    {
        static readonly Color StarOnColor = new Color(1, 0.8f, 0.1f, 1);
        static readonly Color StarOffColor = new Color(1, 1, 1, 0.25f);
        static readonly Color AlertColor = new Color(1, 0.3f, 0.2f, 1);

        ECCGameController gameController;
        ECCEndlessMode endlessMode;

        Canvas canvas;
        RectTransform[] stars;
        Image[] starImages;
        Text multiplierText;
        Image progressFill;
        Text announcementText;

        Coroutine announcementRoutine;
        bool isHidden = false;

        public static ECCWantedHUD Create(ECCGameController gameController, ECCEndlessMode endlessMode)
        {
            Canvas canvas = ECCRuntimeUI.CreateCanvas("CanvasWantedLevel", 50);

            ECCWantedHUD hud = canvas.gameObject.AddComponent<ECCWantedHUD>();
            hud.gameController = gameController;
            hud.endlessMode = endlessMode;
            hud.canvas = canvas;
            hud.Build(endlessMode.maxWantedLevel, endlessMode.hudPosition);

            return hud;
        }

        void Build(int starCount, Vector2 position)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);
            Vector2 center = new Vector2(0.5f, 0.5f);

            float starSpacing = 44;
            float rowWidth = starSpacing * starCount;

            RectTransform panel = ECCRuntimeUI.CreateRect("WantedPanel", canvas.transform, topCenter, position, new Vector2(rowWidth + 120, 90));

            ECCRuntimeUI.CreateText("Title", panel, "تحت تعقیب", 22, topCenter, Vector2.zero, new Vector2(rowWidth, 28), Color.white);

            // The stars are diamonds ( rotated squares ) so they don't depend on a font glyph
            stars = new RectTransform[starCount];
            starImages = new Image[starCount];

            for (int index = 0; index < starCount; index++)
            {
                float x = -rowWidth * 0.5f + starSpacing * (index + 0.5f);

                Image star = ECCRuntimeUI.CreateImage("Star" + (index + 1), panel, topCenter, new Vector2(x, -48), new Vector2(24, 24), StarOffColor);
                star.rectTransform.pivot = center;
                star.rectTransform.localEulerAngles = Vector3.forward * 45;
                star.raycastTarget = false;

                stars[index] = star.rectTransform;
                starImages[index] = star;
            }

            multiplierText = ECCRuntimeUI.CreateText("Multiplier", panel, "x1", 26, topCenter, new Vector2(rowWidth * 0.5f + 40, -48), new Vector2(70, 34), StarOnColor);

            // Progress towards the next wanted level
            Image progressBack = ECCRuntimeUI.CreateImage("Progress", panel, topCenter, new Vector2(0, -76), new Vector2(rowWidth, 6), new Color(0, 0, 0, 0.4f));
            progressBack.raycastTarget = false;

            progressFill = ECCRuntimeUI.CreateImage("Fill", progressBack.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(rowWidth, 6), AlertColor);
            progressFill.raycastTarget = false;
            progressFill.rectTransform.anchorMin = new Vector2(0, 0);
            progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            progressFill.rectTransform.sizeDelta = new Vector2(0, 0);

            announcementText = ECCRuntimeUI.CreateText("Announcement", canvas.transform, string.Empty, 48, center, new Vector2(0, 120), new Vector2(800, 80), AlertColor);
            announcementText.gameObject.SetActive(false);
        }

        void Update()
        {
            // Follow the visibility of the game canvas, so the HUD hides while paused
            bool visible = isHidden == false && (gameController == null || gameController.gameCanvas == null || gameController.gameCanvas.gameObject.activeInHierarchy);

            if (canvas.enabled != visible) canvas.enabled = visible;

            if (endlessMode && progressFill)
            {
                float width = ((RectTransform)progressFill.rectTransform.parent).rect.width;

                progressFill.rectTransform.sizeDelta = new Vector2(width * endlessMode.LevelProgress, 0);
            }
        }

        public void SetWantedLevel(int wantedLevel, int scoreMultiplier, bool announce)
        {
            for (int index = 0; index < starImages.Length; index++)
            {
                starImages[index].color = index < wantedLevel ? StarOnColor : StarOffColor;
            }

            multiplierText.text = "x" + scoreMultiplier;

            if (announce)
            {
                if (announcementRoutine != null) StopCoroutine(announcementRoutine);

                announcementRoutine = StartCoroutine(Announce(wantedLevel));
            }
        }

        public void Hide()
        {
            isHidden = true;
        }

        IEnumerator Announce(int wantedLevel)
        {
            announcementText.text = ECCPersianText.Fix("سطح تعقیب " + wantedLevel + "!");
            announcementText.gameObject.SetActive(true);

            RectTransform newStar = wantedLevel - 1 < stars.Length ? stars[wantedLevel - 1] : null;

            float duration = 1.8f;

            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float progress = time / duration;

                // Pop in, hold, then fade out
                float scale = progress < 0.15f ? Mathf.Lerp(1.6f, 1, progress / 0.15f) : 1;
                float alpha = progress < 0.7f ? 1 : Mathf.Lerp(1, 0, (progress - 0.7f) / 0.3f);

                announcementText.rectTransform.localScale = Vector3.one * scale;
                announcementText.color = new Color(AlertColor.r, AlertColor.g, AlertColor.b, alpha);

                if (newStar) newStar.localScale = Vector3.one * (1 + Mathf.Abs(Mathf.Sin(time * 10)) * 0.5f * (1 - progress));

                yield return null;
            }

            if (newStar) newStar.localScale = Vector3.one;

            announcementText.gameObject.SetActive(false);

            announcementRoutine = null;
        }
    }
}
