using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Short messages that pop up in the middle of the screen and fade out, like "Nitro!" or "Mission complete".
    /// Usage: ECCToast.Show("متن", Color.white);
    /// </summary>
    public class ECCToast : MonoBehaviour
    {
        static ECCToast instance;

        Text messageText;
        Text detailText;
        Coroutine showRoutine;

        public static void Show(string message, Color color, string detail = "", float duration = 1.8f)
        {
            if (instance == null) Create();

            if (instance.showRoutine != null) instance.StopCoroutine(instance.showRoutine);

            instance.showRoutine = instance.StartCoroutine(instance.ShowRoutine(message, detail, color, duration));
        }

        static void Create()
        {
            Canvas canvas = ECCRuntimeUI.CreateCanvas("CanvasToast", 90);

            instance = canvas.gameObject.AddComponent<ECCToast>();

            Vector2 center = new Vector2(0.5f, 0.5f);

            instance.messageText = ECCRuntimeUI.CreateText("Message", canvas.transform, string.Empty, 46, center, new Vector2(0, 150), new Vector2(900, 70), Color.white);
            instance.detailText = ECCRuntimeUI.CreateText("Detail", canvas.transform, string.Empty, 26, center, new Vector2(0, 100), new Vector2(900, 40), Color.white);

            instance.messageText.gameObject.SetActive(false);
            instance.detailText.gameObject.SetActive(false);
        }

        IEnumerator ShowRoutine(string message, string detail, Color color, float duration)
        {
            ECCRuntimeUI.SetText(messageText, message);
            ECCRuntimeUI.SetText(detailText, detail);

            messageText.gameObject.SetActive(true);
            detailText.gameObject.SetActive(detail != string.Empty);

            // Unscaled time, so toasts also work while the game is paused or in slow motion
            for (float time = 0; time < duration; time += Time.unscaledDeltaTime)
            {
                float progress = time / duration;

                float scale = progress < 0.12f ? Mathf.Lerp(1.5f, 1, progress / 0.12f) : 1;
                float alpha = progress < 0.7f ? 1 : Mathf.Lerp(1, 0, (progress - 0.7f) / 0.3f);

                messageText.rectTransform.localScale = Vector3.one * scale;
                messageText.color = new Color(color.r, color.g, color.b, alpha);
                detailText.color = new Color(1, 1, 1, alpha);

                yield return null;
            }

            messageText.gameObject.SetActive(false);
            detailText.gameObject.SetActive(false);

            showRoutine = null;
        }
    }
}
