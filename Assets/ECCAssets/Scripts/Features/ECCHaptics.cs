using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Short phone vibrations: a strong one for crashes, a light one for power-ups and close passes, a double tap for rewards.
    /// On Android it uses the system Vibrator with a duration ( Handheld.Vibrate is one long buzz ); elsewhere Handheld.Vibrate.
    /// The player can turn it off ( saved in PlayerPrefs, on by default ).
    /// </summary>
    public static class ECCHaptics
    {
        public enum Kind
        {
            Light = 0,   // A close pass, a power-up pickup
            Medium = 1,  // Picking a power-up, a small reward
            Heavy = 2,   // The player's car is hit
            Reward = 3,  // Coins paid ( two short taps )
        }

        const string EnabledPlayerPrefs = "Vibration";

        // Vibrations closer together than this are skipped, so a pile-up doesn't buzz without a pause
        const float MinGap = 0.12f;

        static float lastTime = -10;

        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt(EnabledPlayerPrefs, 1) == 1; }
            set
            {
                PlayerPrefs.SetInt(EnabledPlayerPrefs, value ? 1 : 0);
                PlayerPrefs.Save();

                if (value) Vibrate(Kind.Medium);
            }
        }

        /// <summary>
        /// The duration in milliseconds of a vibration kind
        /// </summary>
        public static long Duration(Kind kind)
        {
            switch (kind)
            {
                case Kind.Light: return 18;
                case Kind.Medium: return 40;
                case Kind.Heavy: return 90;
                default: return 30;
            }
        }

        public static void Vibrate(Kind kind)
        {
            if (Enabled == false) return;

            float now = Time.unscaledTime;

            if (now - lastTime < MinGap) return;

            lastTime = now;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (kind == Kind.Reward) VibrateAndroid(new long[] { 0, 30, 70, 40 });
            else VibrateAndroid(Duration(kind));
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS has no duration control here; the light ones are skipped so they don't feel heavy
            if (kind == Kind.Heavy || kind == Kind.Reward) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator;
        static bool vibratorMissing;

        static AndroidJavaObject Vibrator
        {
            get
            {
                if (vibrator != null || vibratorMissing) return vibrator;

                try
                {
                    using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity");

                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("Vibration is not available: " + exception.Message);
                }

                if (vibrator == null) vibratorMissing = true;

                return vibrator;
            }
        }

        static void VibrateAndroid(long milliseconds)
        {
            try
            {
                if (Vibrator != null) Vibrator.Call("vibrate", milliseconds);
                else Handheld.Vibrate();
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
        }

        static void VibrateAndroid(long[] pattern)
        {
            try
            {
                if (Vibrator != null) Vibrator.Call("vibrate", pattern, -1);
                else Handheld.Vibrate();
            }
            catch (System.Exception)
            {
                Handheld.Vibrate();
            }
        }
#endif

        /// <summary>
        /// Never called: it only makes sure Unity adds the VIBRATE permission to the Android manifest
        /// ( Unity adds it when Handheld.Vibrate is used anywhere in the scripts )
        /// </summary>
        static void KeepPermission()
        {
            Handheld.Vibrate();
        }
    }
}
