using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The main menu buttons' gloss and light sweep ( ECCMenuButtonShine ). The game uses Resources/ECCMenuStyleConfig if
    /// it exists, otherwise these defaults. Assets > Create > Endless Car Chase > Menu Style Config
    /// </summary>
    [CreateAssetMenu(fileName = "ECCMenuStyleConfig", menuName = "Endless Car Chase/Menu Style Config")]
    public class ECCMenuStyleConfig : ScriptableObject
    {
        const string ResourcePath = "ECCMenuStyleConfig";

        [Tooltip("The soft top highlight on every main menu button")]
        public bool gloss = true;

        [Range(0, 0.5f)] public float glossAlpha = 0.18f;

        [Tooltip("The light streak passing across the buttons")]
        public bool lightSweep = true;

        [Tooltip("Seconds one sweep takes")]
        public float sweepDuration = 0.75f;

        [Tooltip("Seconds between two sweeps of the same button ( random in this range )")]
        public float sweepIntervalMin = 3;
        public float sweepIntervalMax = 5;

        [Tooltip("The primary play button sweeps this much more often ( interval x this )")]
        public float primaryIntervalFactor = 0.7f;

        [Range(0, 1)] public float streakAlpha = 0.35f;

        [Tooltip("No light sweep on devices with less memory than this ( MB ); 0 never turns it off")]
        public int disableSweepBelowMemoryMB = 1536;

        static ECCMenuStyleConfig instance;

        public static ECCMenuStyleConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCMenuStyleConfig>(ResourcePath);

                    if (instance == null) instance = CreateInstance<ECCMenuStyleConfig>();
                }

                return instance;
            }
        }

        public bool SweepAllowed
        {
            get
            {
                if (lightSweep == false) return false;

                int memory = SystemInfo.systemMemorySize;

                return disableSweepBelowMemoryMB <= 0 || memory <= 0 || memory >= disableSweepBelowMemoryMB;
            }
        }
    }
}
