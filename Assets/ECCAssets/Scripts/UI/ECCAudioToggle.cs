using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The sound / music button of the new UI, used on the main menu and the pause screen: a round candy button ( blue
    /// for sound, purple for music ) with a darker shade under it and the game's speaker / note icon. While muted it
    /// turns gray with a red slash across the icon.
    /// It wraps the scene's old button: tapping calls the old button's listener ( ECCAudioControl.ToggleSound /
    /// ToggleMusic ), so the volume and its saved setting work as before, and the state is read back from PlayerPrefs.
    /// Usage: ECCAudioToggle.Create(parent, anchor, position, size, oldButton, isMusic)
    /// </summary>
    public class ECCAudioToggle : MonoBehaviour
    {
        static readonly Color SoundColor = ECCCarTheme.CandyBlue;
        static readonly Color MusicColor = ECCCarTheme.Hex(0x8E5CF7);
        static readonly Color VibrationColor = ECCCarTheme.Hex(0x14B8A6);

        Button oldButton;
        bool music;
        bool vibration;
        Image face;
        Image shade;
        Image icon;
        Image slash;

        public static ECCAudioToggle Create(Transform parent, Vector2 anchor, Vector2 position, float size, Button oldButton, bool music)
        {
            RectTransform root = ECCRuntimeUI.CreateRect(music ? "MusicToggle" : "SoundToggle", parent, anchor, position, new Vector2(size, size));

            // The card styling of popups leaves it as it is
            root.gameObject.AddComponent<ECCKeepColors>();

            ECCAudioToggle toggle = root.gameObject.AddComponent<ECCAudioToggle>();
            toggle.oldButton = oldButton;
            toggle.music = music;
            toggle.Build(root, size);

            return toggle;
        }

        /// <summary>
        /// The same round button for the phone vibration setting ( ECCHaptics ), with a drawn phone icon
        /// </summary>
        public static ECCAudioToggle CreateVibration(Transform parent, Vector2 anchor, Vector2 position, float size)
        {
            RectTransform root = ECCRuntimeUI.CreateRect("VibrationToggle", parent, anchor, position, new Vector2(size, size));

            root.gameObject.AddComponent<ECCKeepColors>();

            ECCAudioToggle toggle = root.gameObject.AddComponent<ECCAudioToggle>();
            toggle.vibration = true;
            toggle.Build(root, size);

            return toggle;
        }

        /// <summary>
        /// A phone with buzz lines on both sides, drawn with rounded bars ( there is no vibration icon in the art )
        /// </summary>
        static void DrawPhone(Transform parent, float size)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            Image phone = ECCRuntimeUI.CreateImage("Phone", parent, center, Vector2.zero, new Vector2(size * 0.3f, size * 0.5f), Color.white);
            ECCCarTheme.Round(phone);
            phone.pixelsPerUnitMultiplier = 48 / (size * 0.12f);
            phone.raycastTarget = false;

            Image screen = ECCRuntimeUI.CreateImage("Screen", phone.transform, center, Vector2.zero, new Vector2(size * 0.2f, size * 0.36f), new Color(0, 0, 0, 0.25f));
            ECCCarTheme.Round(screen);
            screen.pixelsPerUnitMultiplier = 48 / (size * 0.06f);
            screen.raycastTarget = false;

            for (int side = -1; side <= 1; side += 2)
            {
                for (int line = 0; line < 2; line++)
                {
                    float x = side * size * (0.23f + line * 0.08f);
                    float height = size * (0.26f - line * 0.08f);

                    Image buzz = ECCRuntimeUI.CreateImage("Buzz", parent, center, new Vector2(x, 0), new Vector2(size * 0.045f, height), Color.white);
                    ECCCarTheme.Round(buzz);
                    buzz.pixelsPerUnitMultiplier = 48 / (size * 0.045f);
                    buzz.raycastTarget = false;
                }
            }
        }

        void Build(RectTransform root, float size)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            shade = ECCUIKit.CreateCircle("Shade", root, center, new Vector2(0, -size * 0.07f), size, Color.gray);
            shade.rectTransform.pivot = center;
            shade.raycastTarget = false;

            face = ECCUIKit.CreateCircle("Face", root, center, Vector2.zero, size, Color.gray);
            face.rectTransform.pivot = center;

            // A soft shine on the top half
            Image shine = ECCUIKit.CreateCircle("Shine", face.transform, new Vector2(0.5f, 1), new Vector2(0, -size * 0.28f), 0, new Color(1, 1, 1, 0.2f));
            shine.rectTransform.pivot = center;
            shine.rectTransform.sizeDelta = new Vector2(size * 0.7f, size * 0.38f);
            shine.raycastTarget = false;

            Image oldImage = oldButton ? oldButton.targetGraphic as Image : null;

            icon = ECCRuntimeUI.CreateImage("Icon", face.transform, center, Vector2.zero, new Vector2(size * 0.6f, size * 0.6f), Color.white);
            icon.rectTransform.pivot = center;
            icon.sprite = oldImage ? oldImage.sprite : null;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            if (vibration)
            {
                icon.color = new Color(1, 1, 1, 0);
                DrawPhone(icon.transform, size);
            }

            Shadow iconShadow = icon.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(0, 0, 0, 0.25f);
            iconShadow.effectDistance = new Vector2(0, -2);

            // The "off" slash: a red bar with a white edge, across the icon
            Image slashEdge = ECCRuntimeUI.CreateImage("Slash", face.transform, center, Vector2.zero, new Vector2(size * 0.78f, size * 0.15f), Color.white);
            slashEdge.rectTransform.pivot = center;
            slashEdge.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            ECCCarTheme.Round(slashEdge);
            slashEdge.pixelsPerUnitMultiplier = 48 / (size * 0.15f);
            slashEdge.raycastTarget = false;

            Image slashBar = ECCRuntimeUI.CreateImage("Bar", slashEdge.transform, center, Vector2.zero, new Vector2(size * 0.72f, size * 0.08f), ECCCarTheme.CandyRed);
            slashBar.rectTransform.pivot = center;
            ECCCarTheme.Round(slashBar);
            slashBar.pixelsPerUnitMultiplier = 48 / (size * 0.08f);
            slashBar.raycastTarget = false;

            slash = slashEdge;

            Button button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Toggle);

            // Marks the button as styled, so a card keeps the round face
            face.gameObject.AddComponent<ECCCandyShade>();

            root.gameObject.AddComponent<ECCPressScale>();

            Refresh();
        }

        void Toggle()
        {
            if (vibration) ECCHaptics.Enabled = ECCHaptics.Enabled == false;
            else if (oldButton) oldButton.onClick.Invoke();

            Refresh();
        }

        void OnEnable()
        {
            // The other screen may have changed the setting
            if (face) Refresh();
        }

        bool IsOn
        {
            get
            {
                if (vibration) return ECCHaptics.Enabled;

                return PlayerPrefs.GetFloat(music ? "MusicVolume" : "SoundVolume", 1) > 0;
            }
        }

        void Refresh()
        {
            bool on = IsOn;

            Color color = on ? (vibration ? VibrationColor : (music ? MusicColor : SoundColor)) : ECCCarTheme.CandyGray;

            face.color = color;
            shade.color = new Color(color.r * 0.68f, color.g * 0.68f, color.b * 0.68f, 1);
            if (vibration == false) icon.color = new Color(1, 1, 1, on ? 1 : 0.7f);
            slash.gameObject.SetActive(on == false);
        }
    }
}
