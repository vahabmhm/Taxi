using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Reward feedback: a floating "+X [coin]" in the middle of the screen, and a light confetti burst.
    /// Both use unscaled time, so they also play over paused result screens.
    /// Usage: ECCRewardFx.ShowCoins(150);  ECCRewardFx.Confetti();
    /// </summary>
    public class ECCRewardFx : MonoBehaviour
    {
        const float CoinDuration = 2;
        const int ConfettiPieces = 70;
        const float ConfettiDuration = 2.6f;

        static ECCRewardFx instance;

        RectTransform root;

        static ECCRewardFx Instance
        {
            get
            {
                if (instance == null)
                {
                    Canvas canvas = ECCUIKit.CreateCanvas("CanvasRewardFx", 97);

                    // Feedback never blocks taps
                    Destroy(canvas.GetComponent<GraphicRaycaster>());

                    instance = canvas.gameObject.AddComponent<ECCRewardFx>();
                    instance.root = (RectTransform)canvas.transform;
                }

                return instance;
            }
        }

        /// <summary>
        /// Shows "+X" with a coin icon, which grows, floats up and fades over about two seconds
        /// </summary>
        public static void ShowCoins(int amount)
        {
            if (amount <= 0) return;

            ECCHaptics.Vibrate(ECCHaptics.Kind.Reward);

            Instance.StartCoroutine(Instance.CoinRoutine(amount));
        }

        public static void Confetti()
        {
            Instance.StartCoroutine(Instance.ConfettiRoutine());
        }

        IEnumerator CoinRoutine(int amount)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            RectTransform group = ECCRuntimeUI.CreateRect("CoinReward", root, center, new Vector2(0, 40), new Vector2(360, 110));

            // Left to right: "+150" then the coin, which reads naturally in Persian too ( the number and its unit )
            ECCUIKit.CreateNumberText("Amount", group, "+" + ECCWallet.Format(amount), 72, center, new Vector2(-50, 0), new Vector2(260, 100), ECCRuntimeUI.StarColor);
            ECCUIKit.CreateCoinIcon("Coin", group, center, new Vector2(120, 0), 96);

            CanvasGroup fade = group.gameObject.AddComponent<CanvasGroup>();

            for (float time = 0; time < CoinDuration; time += Time.unscaledDeltaTime)
            {
                float progress = time / CoinDuration;

                float scale = progress < 0.15f ? Mathf.Lerp(0.5f, 1.15f, progress / 0.15f) : (progress < 0.25f ? Mathf.Lerp(1.15f, 1, (progress - 0.15f) / 0.1f) : 1);

                group.localScale = Vector3.one * scale;
                group.anchoredPosition = new Vector2(0, 40 + progress * 110);
                fade.alpha = progress < 0.65f ? 1 : Mathf.Lerp(1, 0, (progress - 0.65f) / 0.35f);

                yield return null;
            }

            Destroy(group.gameObject);
        }

        IEnumerator ConfettiRoutine()
        {
            RectTransform layer = ECCRuntimeUI.CreateRect("Confetti", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ECCUIKit.Stretch(layer);

            Color[] colors = { ECCRuntimeUI.AccentColor, ECCRuntimeUI.GoodColor, ECCRuntimeUI.StarColor, new Color(0.3f, 0.6f, 1), new Color(1, 0.35f, 0.6f) };

            RectTransform[] pieces = new RectTransform[ConfettiPieces];
            Vector2[] velocities = new Vector2[ConfettiPieces];
            float[] spins = new float[ConfettiPieces];

            Vector2 size = root.rect.size;

            for (int index = 0; index < ConfettiPieces; index++)
            {
                Image piece = ECCRuntimeUI.CreateImage("Piece", layer, new Vector2(0.5f, 1), Vector2.zero, new Vector2(Random.Range(10f, 18f), Random.Range(16f, 28f)), colors[index % colors.Length]);
                piece.raycastTarget = false;

                pieces[index] = piece.rectTransform;
                pieces[index].anchoredPosition = new Vector2(Random.Range(-0.5f, 0.5f) * size.x, Random.Range(0, 120f));
                pieces[index].localEulerAngles = Vector3.forward * Random.Range(0, 360f);

                velocities[index] = new Vector2(Random.Range(-120f, 120f), Random.Range(-60f, 120f));
                spins[index] = Random.Range(-540f, 540f);
            }

            for (float time = 0; time < ConfettiDuration; time += Time.unscaledDeltaTime)
            {
                float deltaTime = Time.unscaledDeltaTime;

                for (int index = 0; index < ConfettiPieces; index++)
                {
                    velocities[index].y -= 520 * deltaTime;
                    velocities[index].x *= 1 - deltaTime * 0.8f;

                    pieces[index].anchoredPosition += velocities[index] * deltaTime;
                    pieces[index].Rotate(0, 0, spins[index] * deltaTime);
                }

                yield return null;
            }

            Destroy(layer.gameObject);
        }
    }
}
