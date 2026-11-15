using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum TaxiLight { Green, Yellow, Red }

    /// <summary>
    /// The traffic lights of the city's crossroads. Every crossroad switches between the streets along Z and along X
    /// ( green, yellow, then red while the other street has green ), each with its own offset so the city isn't in step.
    /// - near the player every approach to a crossroad gets a glowing stop line across its lane and a small signal
    ///   lamp on the corner, in the color of its light, so the light is easy to read from above
    /// - traffic cars and rival taxis stop at the line on yellow and red ( ECCTraffic, TaxiRival )
    /// - the player driving into a crossroad on red is a violation ( TaxiPolice )
    /// </summary>
    public class TaxiTrafficLights : MonoBehaviour
    {
        [Tooltip("Seconds of a whole light cycle ( both streets get green once )")]
        public float cycle = 16;

        [Tooltip("Seconds of yellow at the end of each green")]
        public float yellowTime = 2;

        [Tooltip("Crossroads closer than this to the player show their stop lines and lamps")]
        public float showDistance = 80;

        public static TaxiTrafficLights Instance { get; private set; }

        /// <summary>
        /// Called when the player drives into a crossroad on red ( the crossroad )
        /// </summary>
        public static event System.Action<Vector2Int> PlayerRanRedLight;

        class Signal
        {
            public GameObject root;
            public Renderer[] alongZ;
            public Renderer[] alongX;
        }

        readonly Dictionary<Vector2Int, Signal> signals = new Dictionary<Vector2Int, Signal>();
        readonly List<Vector2Int> removeList = new List<Vector2Int>();
        readonly Stack<Signal> pool = new Stack<Signal>();

        Material red, yellow, green, pole;
        ECCGameController gameController;

        bool playerWasInCrossroad;
        Vector2Int lastCrossroad;

        public static TaxiTrafficLights Enable(ECCGameController controller)
        {
            if (Instance) return Instance;

            Instance = controller.gameObject.AddComponent<TaxiTrafficLights>();
            Instance.gameController = controller;

            return Instance;
        }

        void Awake()
        {
            red = ECCRuntimeVisuals.CreateMaterial(new Color(1, 0.12f, 0.1f), 1.6f);
            yellow = ECCRuntimeVisuals.CreateMaterial(new Color(1, 0.8f, 0.1f), 1.6f);
            green = ECCRuntimeVisuals.CreateMaterial(new Color(0.15f, 1, 0.3f), 1.4f);
            pole = ECCRuntimeVisuals.CreateMaterial(new Color(0.2f, 0.2f, 0.24f), 0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ----- The lights -----

        static int Hash(Vector2Int crossroad)
        {
            unchecked
            {
                int hash = crossroad.x * 73856093 ^ crossroad.y * 19349663;
                hash ^= hash >> 13;
                hash *= 0x5bd1e995;
                hash ^= hash >> 15;

                return hash & 0x7fffffff;
            }
        }

        /// <summary>
        /// The light of a crossroad for the cars driving along Z ( alongZ ) or along X, at a time
        /// </summary>
        public TaxiLight GetLight(Vector2Int crossroad, bool alongZ, float time)
        {
            return LightAt(crossroad, alongZ, time, cycle, yellowTime);
        }

        public TaxiLight GetLight(Vector2Int crossroad, bool alongZ)
        {
            return GetLight(crossroad, alongZ, Time.time);
        }

        /// <summary>
        /// The light rule itself ( also used by the tests )
        /// </summary>
        public static TaxiLight LightAt(Vector2Int crossroad, bool alongZ, float time, float cycle, float yellowTime)
        {
            float half = cycle * 0.5f;
            float offset = (Hash(crossroad) % 1000) / 1000f * cycle;

            float phase = Mathf.Repeat(time + offset + (alongZ ? 0 : half), cycle);

            if (phase < half - yellowTime) return TaxiLight.Green;
            if (phase < half) return TaxiLight.Yellow;

            return TaxiLight.Red;
        }

        /// <summary>
        /// Must a car at this position, driving this way, wait at the next crossroad's stop line? True on yellow and red
        /// while it is between lookAhead and the line ( a car that already passed the line drives on )
        /// </summary>
        public bool MustStopAhead(Vector3 position, Vector3 forward, float lookAhead, out float distanceToLine)
        {
            Vector2Int crossroad;

            distanceToLine = TaxiCity.DistanceToNextCrossroad(position, forward, out crossroad);

            if (distanceToLine < 0 || distanceToLine > lookAhead) return false;

            return GetLight(crossroad, TaxiCity.IsAlongZ(forward)) != TaxiLight.Green;
        }

        // ----- The player -----

        void Update()
        {
            ECCCar player = gameController ? gameController.playerObject : null;

            if (player == null || TaxiCity.IsActive == false) return;

            CheckPlayer(player);

            UpdateSignals(player.transform.position);
        }

        void CheckPlayer(ECCCar player)
        {
            Vector2Int crossroad;
            bool inside = TaxiCity.InCrossroad(player.transform.position, out crossroad);

            // Driving into a crossroad: the light of the street the car comes along
            if (inside && (playerWasInCrossroad == false || crossroad != lastCrossroad) && player.CurrentSpeed > 1 && gameController.gameStarted && gameController.isGameOver == false)
            {
                if (GetLight(crossroad, TaxiCity.IsAlongZ(player.transform.forward)) == TaxiLight.Red && PlayerRanRedLight != null) PlayerRanRedLight(crossroad);
            }

            playerWasInCrossroad = inside;
            lastCrossroad = crossroad;
        }

        // ----- Stop lines and lamps near the player -----

        void UpdateSignals(Vector3 center)
        {
            float spacing = TaxiCity.Spacing;
            int range = Mathf.CeilToInt(showDistance / spacing);

            Vector2Int middle = TaxiCity.NearestCrossroad(center);

            removeList.Clear();

            foreach (KeyValuePair<Vector2Int, Signal> pair in signals)
            {
                if (Mathf.Abs(pair.Key.x - middle.x) > range + 1 || Mathf.Abs(pair.Key.y - middle.y) > range + 1) removeList.Add(pair.Key);
            }

            foreach (Vector2Int key in removeList)
            {
                signals[key].root.SetActive(false);
                pool.Push(signals[key]);
                signals.Remove(key);
            }

            for (int x = middle.x - range; x <= middle.x + range; x++)
            {
                for (int y = middle.y - range; y <= middle.y + range; y++)
                {
                    Vector2Int key = new Vector2Int(x, y);

                    Signal signal;

                    if (signals.TryGetValue(key, out signal) == false)
                    {
                        signal = pool.Count > 0 ? pool.Pop() : BuildSignal();
                        signal.root.transform.position = TaxiCity.CrossroadCenter(key);
                        signal.root.SetActive(true);
                        signals[key] = signal;
                    }

                    Paint(signal.alongZ, GetLight(key, true));
                    Paint(signal.alongX, GetLight(key, false));
                }
            }
        }

        void Paint(Renderer[] parts, TaxiLight light)
        {
            Material material = light == TaxiLight.Green ? green : (light == TaxiLight.Yellow ? yellow : red);

            foreach (Renderer part in parts)
            {
                if (part.sharedMaterial != material) part.sharedMaterial = material;
            }
        }

        /// <summary>
        /// The four stop lines ( one in the right lane of each approach ) and the four corner lamps of a crossroad,
        /// around its center
        /// </summary>
        Signal BuildSignal()
        {
            Signal signal = new Signal();

            signal.root = new GameObject("TrafficSignal");
            signal.root.transform.SetParent(transform, false);

            List<Renderer> alongZ = new List<Renderer>();
            List<Renderer> alongX = new List<Renderer>();

            float edge = TaxiCity.CrossHalf + 0.5f;
            float lane = TaxiCity.LaneOffset;
            const float lineHeight = 0.07f;

            // Driving +Z ( coming from -Z, right lane at +X ), -Z, +X ( right lane at -Z ), -X
            alongZ.Add(Line(signal.root.transform, new Vector3(lane - 0.4f, lineHeight, -edge), new Vector3(4.6f, 0.04f, 0.7f)));
            alongZ.Add(Line(signal.root.transform, new Vector3(-lane + 0.4f, lineHeight, edge), new Vector3(4.6f, 0.04f, 0.7f)));
            alongX.Add(Line(signal.root.transform, new Vector3(-edge, lineHeight, -lane + 0.4f), new Vector3(0.7f, 0.04f, 4.6f)));
            alongX.Add(Line(signal.root.transform, new Vector3(edge, lineHeight, lane - 0.4f), new Vector3(0.7f, 0.04f, 4.6f)));

            // A lamp on the corner at the right of every approach
            float corner = TaxiCity.SidewalkOffset + 0.3f;

            alongZ.Add(Lamp(signal.root.transform, new Vector3(corner, 0, -edge)));
            alongZ.Add(Lamp(signal.root.transform, new Vector3(-corner, 0, edge)));
            alongX.Add(Lamp(signal.root.transform, new Vector3(-edge, 0, -corner)));
            alongX.Add(Lamp(signal.root.transform, new Vector3(edge, 0, corner)));

            signal.alongZ = alongZ.ToArray();
            signal.alongX = alongX.ToArray();

            return signal;
        }

        Renderer Line(Transform parent, Vector3 position, Vector3 size)
        {
            return ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, parent, position, size, green).GetComponent<Renderer>();
        }

        Renderer Lamp(Transform parent, Vector3 position)
        {
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, parent, position + Vector3.up * 1.6f, new Vector3(0.25f, 1.6f, 0.25f), pole);

            Transform head = ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, parent, position + Vector3.up * 3.5f, Vector3.one * 1.1f, green);

            return head.GetComponent<Renderer>();
        }
    }
}
