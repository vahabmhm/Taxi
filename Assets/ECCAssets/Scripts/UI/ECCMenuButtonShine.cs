using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The one shared glossy style of the main menu buttons: a soft highlight on the top half and a diagonal light streak
    /// that passes across the button every few seconds ( ECCMenuStyleConfig ). Each button gets its own random offset so
    /// they never all flash together; the primary play button sweeps a little more often.
    /// The streak lives in a RectMask2D stretched over the button, so it never leaks outside it; nothing it adds is a
    /// raycast target and nothing changes the button's size or layout. It waits while the menu is hidden ( inactive ),
    /// while a popup covers the menu, during an ad and while the app is in the background. No per-frame allocations.
    /// Separate from ECCPulse ( the badge / attention pulse scales the button; the shine only moves inside it ).
    /// </summary>
    public class ECCMenuButtonShine : MonoBehaviour
    {
        static Sprite streakSprite;
        static int count;

        RectTransform button;
        RectTransform streak;
        Image streakImage;

        float wait;
        float time = -1;
        bool primary;
        bool appPaused;

        public bool IsSweeping
        {
            get { return time >= 0; }
        }

        /// <summary>
        /// Adds the style to a button ( once )
        /// </summary>
        public static ECCMenuButtonShine Apply(Button target, bool primary = false)
        {
            if (target == null) return null;

            ECCMenuButtonShine shine = target.GetComponent<ECCMenuButtonShine>();

            if (shine) return shine;

            shine = target.gameObject.AddComponent<ECCMenuButtonShine>();
            shine.primary = primary;
            shine.Build();

            return shine;
        }

        void Build()
        {
            ECCMenuStyleConfig config = ECCMenuStyleConfig.Instance;

            button = (RectTransform)transform;

            Vector2 center = new Vector2(0.5f, 0.5f);

            if (config.gloss)
            {
                // The top half, a little lighter
                Image gloss = ECCRuntimeUI.CreateImage("Gloss", button, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero, new Color(1, 1, 1, config.glossAlpha));
                gloss.rectTransform.anchorMin = new Vector2(0, 0.5f);
                gloss.rectTransform.anchorMax = new Vector2(1, 1);
                gloss.rectTransform.offsetMin = new Vector2(8, 0);
                gloss.rectTransform.offsetMax = new Vector2(-8, -5);
                ECCCarTheme.Round(gloss);
                gloss.raycastTarget = false;

                // Behind the label and the icon
                gloss.rectTransform.SetAsFirstSibling();
            }

            if (config.SweepAllowed == false) return;

            RectTransform clip = ECCRuntimeUI.CreateRect("ShineClip", button, center, Vector2.zero, Vector2.zero);
            clip.anchorMin = Vector2.zero;
            clip.anchorMax = Vector2.one;
            clip.offsetMin = new Vector2(4, 4);
            clip.offsetMax = new Vector2(-4, -4);
            clip.gameObject.AddComponent<RectMask2D>();

            streakImage = ECCRuntimeUI.CreateImage("Streak", clip, center, Vector2.zero, new Vector2(46, 260), new Color(1, 1, 1, config.streakAlpha));
            streakImage.sprite = StreakSprite;
            streakImage.raycastTarget = false;

            streak = streakImage.rectTransform;
            streak.localRotation = Quaternion.Euler(0, 0, -22);
            streak.gameObject.SetActive(false);

            // Staggered start
            count++;
            wait = 0.8f + (count * 0.61f) % 2.4f + Random.Range(0, 0.6f);
        }

        float Interval
        {
            get
            {
                ECCMenuStyleConfig config = ECCMenuStyleConfig.Instance;

                float interval = Random.Range(Mathf.Max(0.5f, config.sweepIntervalMin), Mathf.Max(config.sweepIntervalMin, config.sweepIntervalMax));

                return primary ? interval * Mathf.Clamp(config.primaryIntervalFactor, 0.2f, 1) : interval;
            }
        }

        void OnApplicationPause(bool paused)
        {
            appPaused = paused;
        }

        void OnDisable()
        {
            // Hidden menu: stop mid-sweep cleanly
            if (streak) streak.gameObject.SetActive(false);

            time = -1;
        }

        /// <summary>
        /// Advances the effect ( Update calls it with the unscaled frame time )
        /// </summary>
        public void Step(float deltaTime)
        {
            if (streak == null) return;

            // Covered by a popup or an ad, or in the background: wait
            if (appPaused || ECCModalTracker.AnyOpen || ECCAds.IsBusy)
            {
                if (time >= 0)
                {
                    time = -1;
                    streak.gameObject.SetActive(false);
                    wait = Interval;
                }

                return;
            }

            if (time < 0)
            {
                wait -= deltaTime;

                if (wait > 0) return;

                time = 0;
                streak.gameObject.SetActive(true);
            }

            time += deltaTime;

            float duration = Mathf.Max(0.1f, ECCMenuStyleConfig.Instance.sweepDuration);
            float progress = Mathf.Clamp01(time / duration);

            // From just outside the left edge to just outside the right edge ( ease in-out )
            float eased = progress * progress * (3 - 2 * progress);
            float half = button.rect.width > 1 ? button.rect.width * 0.5f + 60 : button.sizeDelta.x * 0.5f + 60;

            streak.anchoredPosition = new Vector2(Mathf.Lerp(-half, half, eased), 0);

            if (progress >= 1)
            {
                time = -1;
                streak.gameObject.SetActive(false);
                wait = Interval;
            }
        }

        void Update()
        {
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>
        /// A soft vertical band: transparent at both sides, bright in the middle
        /// </summary>
        static Sprite StreakSprite
        {
            get
            {
                if (streakSprite == null)
                {
                    const int width = 32;

                    Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
                    texture.wrapMode = TextureWrapMode.Clamp;

                    Color[] pixels = new Color[width];

                    for (int x = 0; x < width; x++)
                    {
                        float t = (x + 0.5f) / width;
                        float alpha = Mathf.Sin(t * Mathf.PI);

                        pixels[x] = new Color(1, 1, 1, alpha * alpha);
                    }

                    texture.SetPixels(pixels);
                    texture.Apply();

                    streakSprite = Sprite.Create(texture, new Rect(0, 0, width, 1), new Vector2(0.5f, 0.5f), 100);
                }

                return streakSprite;
            }
        }
    }
}
