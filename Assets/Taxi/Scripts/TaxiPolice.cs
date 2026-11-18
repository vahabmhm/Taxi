using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum TaxiViolation
    {
        RedLight = 0,      // «عبور از چراغ قرمز»
        TrafficCrash = 1,  // «تصادف با ماشین مردم»
        WrongWay = 2,      // «رانندگی در خلاف جهت»
        RamTaxi = 3,       // «کوبیدن به تاکسی دیگر»
        RamPolice = 4,     // «حمله به ماشین پلیس»
        WreckPolice = 5,   // «نابود کردن ماشین پلیس»
    }

    /// <summary>
    /// The traffic police. Breaking a rule ( a red light, a crash, driving on the wrong side, ramming ) adds heat
    /// and a fine to the player's record; the heat sets the wanted stars:
    /// - 1 star: one police car comes after the taxi, 2: two, 3: three and spike strips, 4: five and roadblocks,
    ///   5: seven and the helicopter. More violations while chased make it worse
    /// - caught ( a police car right next to the taxi while it stands still ): the fines are paid from the run's
    ///   money and the police leave
    /// - lost ( no police car close for a while ): the heat cools down star by star; at zero the police give up and
    ///   the fines are forgiven
    /// </summary>
    public class TaxiPolice : MonoBehaviour
    {
        public static TaxiPolice Instance { get; private set; }

        [Tooltip("Heat needed for 1 to 5 stars")]
        public float[] starHeat = { 15, 45, 90, 150, 230 };

        [Tooltip("Police cars chasing the taxi at 0 to 5 stars")]
        public int[] copsPerStar = { 0, 1, 2, 3, 5, 7 };

        [Tooltip("Heat lost per second once the police lost sight of the taxi")]
        public float coolRate = 7;

        [Tooltip("Seconds without a police car close before the heat cools down")]
        public float coolDelay = 4;

        [Tooltip("A police car closer than this sees the taxi")]
        public float sightDistance = 42;

        [Tooltip("A police car this close to a standing taxi catches it")]
        public float catchDistance = 6.5f;

        [Tooltip("Seconds the taxi must stand next to a police car to be caught")]
        public float catchTime = 1.6f;

        [Tooltip("Extra fine per star when caught")]
        public int finePerStar = 25;

        public float Heat { get; private set; }
        public int PendingFine { get; private set; }
        public int TotalFined { get; private set; }
        public int Violations { get; private set; }
        public int TimesCaught { get; private set; }

        // 0 to 1: how close the police are to catching the taxi ( for the HUD )
        public float CatchProgress { get; private set; }

        // True while the heat is cooling ( the stars blink on the HUD )
        public bool IsCooling { get; private set; }

        public int Stars
        {
            get { return StarsFor(Heat, starHeat); }
        }

        public static int StarsFor(float heat, float[] thresholds)
        {
            int stars = 0;

            for (int index = 0; index < thresholds.Length; index++)
            {
                if (heat >= thresholds[index]) stars = index + 1;
            }

            return stars;
        }

        public int MaxStarsThisRun { get; private set; }

        /// <summary>
        /// Called on every violation: the kind, the fine, the stars after it
        /// </summary>
        public static event Action<TaxiViolation, int, int> ViolationCommitted;

        // The fine paid, the stars it was caught at
        public static event Action<int, int> Caught;

        public static event Action Escaped;

        public static event Action<int> StarsChanged;

        readonly List<ECCCar> cops = new List<ECCCar>();
        readonly List<ECCCar> leaving = new List<ECCCar>();
        readonly Dictionary<TaxiViolation, float> lastViolation = new Dictionary<TaxiViolation, float>();

        ECCGameController gameController;
        List<ECCCar> policePrefabs = new List<ECCCar>();
        ECCHazardSpawner hazards;

        float unseenTime;
        float catchTimer;
        float spawnTimer;
        float wrongWayTime;
        int lastStars;

        // The most stars since the record was last cleared
        int recordStars;
        int prefabIndex;

        public static TaxiPolice Enable(ECCGameController controller)
        {
            if (Instance) return Instance;

            TaxiPolice police = controller.gameObject.AddComponent<TaxiPolice>();
            police.gameController = controller;
            police.policePrefabs = TakeChasePolice(controller);

            Instance = police;

            return police;
        }

        /// <summary>
        /// The police cars of the chase game: their prefabs are kept for the traffic police, and they no longer come by
        /// themselves ( only after a violation ). Safe to call more than once
        /// </summary>
        public static List<ECCCar> TakeChasePolice(ECCGameController controller)
        {
            ECCSpawnAroundObject spawner = controller ? controller.GetComponent<ECCSpawnAroundObject>() : null;

            if (spawner == null) return new List<ECCCar>();

            // Already taken from this scene's spawner
            if (spawner == chaseSpawner && chasePolice != null) return chasePolice;

            chaseSpawner = spawner;

            ECCSpawnTuning tuning = new ECCSpawnTuning(spawner);

            chasePolice = tuning.GetPolicePrefabs();

            tuning.ClearGroups(ECCSpawnTuning.GroupKind.Police);

            return chasePolice;
        }

        static List<ECCCar> chasePolice;
        static ECCSpawnAroundObject chaseSpawner;

        /// <summary>
        /// The fine of a violation and the heat it adds
        /// </summary>
        public static void GetPenalty(TaxiViolation violation, out int fine, out float heat)
        {
            switch (violation)
            {
                case TaxiViolation.RedLight: fine = 40; heat = 30; break;
                case TaxiViolation.TrafficCrash: fine = 60; heat = 35; break;
                case TaxiViolation.WrongWay: fine = 15; heat = 12; break;
                case TaxiViolation.RamTaxi: fine = 20; heat = 14; break;
                case TaxiViolation.RamPolice: fine = 100; heat = 55; break;
                default: fine = 150; heat = 70; break;
            }
        }

        public static string GetName(TaxiViolation violation)
        {
            switch (violation)
            {
                case TaxiViolation.RedLight: return "عبور از چراغ قرمز";
                case TaxiViolation.TrafficCrash: return "تصادف با ماشین مردم";
                case TaxiViolation.WrongWay: return "رانندگی در خلاف جهت";
                case TaxiViolation.RamTaxi: return "کوبیدن به تاکسی دیگر";
                case TaxiViolation.RamPolice: return "حمله به ماشین پلیس";
                default: return "نابود کردن ماشین پلیس";
            }
        }

        void Start()
        {
            hazards = gameController.GetComponent<ECCHazardSpawner>();

            TaxiTrafficLights.PlayerRanRedLight += OnRedLight;
            ECCTraffic.PlayerCrashed += OnTrafficCrash;
            ECCCar.CarsBumped += OnCarsBumped;
            ECCCar.CarDestroyed += OnCarDestroyed;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            TaxiTrafficLights.PlayerRanRedLight -= OnRedLight;
            ECCTraffic.PlayerCrashed -= OnTrafficCrash;
            ECCCar.CarsBumped -= OnCarsBumped;
            ECCCar.CarDestroyed -= OnCarDestroyed;
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0 && gameController.playerObject && gameController.playerObject.health > 0; }
        }

        // ----- Violations -----

        void OnRedLight(Vector2Int crossroad)
        {
            Commit(TaxiViolation.RedLight);
        }

        void OnTrafficCrash(ECCCar car)
        {
            Commit(TaxiViolation.TrafficCrash);
        }

        void OnCarsBumped(ECCCar striker, ECCCar victim)
        {
            if (striker == null || victim == null || striker.IsPlayer == false) return;

            if (cops.Contains(victim)) Commit(TaxiViolation.RamPolice);
            else if (victim.GetComponent<TaxiRival>()) Commit(TaxiViolation.RamTaxi);
        }

        void OnCarDestroyed(ECCCar car)
        {
            if (cops.Contains(car))
            {
                cops.Remove(car);

                if (IsPlaying) Commit(TaxiViolation.WreckPolice);
            }
        }

        /// <summary>
        /// A violation: heat and a fine on the record. The same kind counts once every few seconds
        /// </summary>
        public void Commit(TaxiViolation violation)
        {
            if (IsPlaying == false) return;

            float last;

            if (lastViolation.TryGetValue(violation, out last) && Time.time - last < 3) return;

            lastViolation[violation] = Time.time;

            int fine;
            float heat;

            GetPenalty(violation, out fine, out heat);

            // A police crackdown ( a city event of the shift ) doubles the fines
            if (TaxiCityEvents.Instance)
            {
                fine = Mathf.RoundToInt(fine * TaxiCityEvents.Instance.FineMultiplier);
                heat *= TaxiCityEvents.Instance.HeatMultiplier;
            }

            Violations++;
            PendingFine += fine;
            Heat = Mathf.Min(Heat + heat, starHeat[starHeat.Length - 1] + 60);

            // Seen: the cooling starts over
            unseenTime = 0;

            UpdateStars();

            ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);

            if (ViolationCommitted != null) ViolationCommitted(violation, fine, Stars);
        }

        void CheckWrongWay(ECCCar player)
        {
            Vector3 position = player.transform.position;
            Vector2Int crossroad;

            if (player.CurrentSpeed < 6 || TaxiCity.InCrossroad(position, out crossroad))
            {
                wrongWayTime = 0;
                return;
            }

            float spacing = TaxiCity.Spacing;
            Vector3 heading = TaxiCity.Snap(player.transform.forward);

            // The side of the street's center line the car drives on, seen from its heading ( right is positive )
            float side;

            if (heading.z != 0) side = (position.x - Mathf.Round(position.x / spacing) * spacing) * heading.z;
            else side = -(position.z - Mathf.Round(position.z / spacing) * spacing) * heading.x;

            wrongWayTime = side < -1.3f ? wrongWayTime + Time.deltaTime : 0;

            if (wrongWayTime > 3.5f)
            {
                wrongWayTime = 0;
                Commit(TaxiViolation.WrongWay);
            }
        }

        // ----- The chase -----

        void Update()
        {
            CleanUp();

            if (IsPlaying == false) return;

            ECCCar player = gameController.playerObject;

            CheckWrongWay(player);

            int stars = Stars;

            // Enough police cars for the stars
            spawnTimer -= Time.deltaTime;

            if (stars > 0 && cops.Count < copsPerStar[Mathf.Min(stars, copsPerStar.Length - 1)] && spawnTimer <= 0)
            {
                spawnTimer = 2.2f;
                SpawnCop(player, stars);
            }

            bool seen = false;
            float nearest = float.MaxValue;

            foreach (ECCCar cop in cops)
            {
                float distance = ECCRuntimeVisuals.FlatDistance(cop.transform.position, player.transform.position);

                nearest = Mathf.Min(nearest, distance);

                if (distance <= sightDistance) seen = true;
            }

            // Caught: a police car next to the taxi standing ( almost ) still
            if (stars > 0 && nearest <= catchDistance && player.CurrentSpeed < 3)
            {
                catchTimer += Time.deltaTime;

                if (catchTimer >= catchTime) Catch(player);
            }
            else
            {
                catchTimer = Mathf.Max(0, catchTimer - Time.deltaTime * 2);
            }

            CatchProgress = stars > 0 ? Mathf.Clamp01(catchTimer / catchTime) : 0;

            // Cooling down while unseen
            unseenTime = seen ? 0 : unseenTime + Time.deltaTime;

            IsCooling = Heat > 0 && unseenTime >= coolDelay;

            if (IsCooling)
            {
                Heat = Mathf.Max(0, Heat - coolRate * Time.deltaTime);

                if (Heat <= 0) Escape();
            }

            UpdateStars();
        }

        void UpdateStars()
        {
            int stars = Stars;

            MaxStarsThisRun = Mathf.Max(MaxStarsThisRun, stars);
            recordStars = Mathf.Max(recordStars, stars);

            if (hazards)
            {
                hazards.spikeStrips = stars >= 3;
                hazards.roadblocks = stars >= 4;
            }

            if (stars >= 5) ECCPoliceHelicopter.Spawn(gameController);
            else if (ECCPoliceHelicopter.Active && lastStars >= 5) ECCPoliceHelicopter.Active.Dismiss();

            if (stars != lastStars)
            {
                if (stars > lastStars) TaxiAudio.Play(TaxiAudio.Siren, 0.6f);

                lastStars = stars;

                if (StarsChanged != null) StarsChanged(stars);
            }
        }

        void SpawnCop(ECCCar player, int stars)
        {
            if (policePrefabs.Count == 0) return;

            ECCCar prefab = policePrefabs[prefabIndex++ % policePrefabs.Count];

            Vector3 away = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * Vector3.forward;
            Vector3 point = ECCRuntimeVisuals.OutOfView(player.transform.position, away, 40, gameController.groundLayer);

            if (TaxiCity.IsActive) point = TaxiCity.SpotNear(point, true).zone;

            Vector3 toPlayer = player.transform.position - point;
            toPlayer.y = 0;

            ECCCar cop = Instantiate(prefab, point, Quaternion.LookRotation(toPlayer.sqrMagnitude > 0.01f ? TaxiCity.Snap(toPlayer) : Vector3.forward));

            // Tougher and faster with more stars
            cop.speed += stars * 0.6f;

            ECCCopVariant.Toughen(cop, 0);

            if (stars >= 3) ECCCopVariant.RollAndApply(cop, 0.1f * stars, 0.08f * stars, stars >= 4 ? 0.1f * stars : 0);

            cops.Add(cop);
        }

        void Catch(ECCCar player)
        {
            int stars = Stars;
            int fine = PendingFine + stars * finePerStar;

            // Paid from the money of this run
            int paid = Mathf.Min(fine, Mathf.Max(0, gameController.score));

            if (paid > 0) gameController.ChangeScore(-paid);

            TotalFined += paid;
            TimesCaught++;

            // A short moment to drive off
            player.invulnerableTime = Mathf.Max(player.invulnerableTime, 3);

            ResetRecord();

            TaxiAudio.Play(TaxiAudio.Bad);

            if (Caught != null) Caught(paid, stars);
        }

        void Escape()
        {
            // Only a real chase ( at least one star ) is an escape; a small violation is simply forgotten
            bool chased = recordStars > 0;

            ResetRecord();

            if (chased == false) return;

            ECCMissions.Report(ECCMissionType.EscapePolice, 1);

            if (Escaped != null) Escaped();
        }

        /// <summary>
        /// The record is cleared and the police drive away
        /// </summary>
        void ResetRecord()
        {
            Heat = 0;
            PendingFine = 0;
            recordStars = 0;
            catchTimer = 0;
            CatchProgress = 0;
            IsCooling = false;

            foreach (ECCCar cop in cops)
            {
                if (cop == null) continue;

                Vector3 away = cop.transform.position - gameController.playerObject.transform.position;
                away.y = 0;

                Vector3 target = cop.transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : cop.transform.forward) * 500;

                cop.aiTarget = () => target;

                leaving.Add(cop);
            }

            cops.Clear();

            UpdateStars();
        }

        void CleanUp()
        {
            cops.RemoveAll(cop => cop == null || cop.health <= 0);

            Camera view = Camera.main;
            Vector3 center = gameController && gameController.playerObject ? gameController.playerObject.transform.position : Vector3.zero;

            for (int index = leaving.Count - 1; index >= 0; index--)
            {
                ECCCar cop = leaving[index];

                if (cop == null)
                {
                    leaving.RemoveAt(index);
                    continue;
                }

                bool far = ECCRuntimeVisuals.FlatDistance(cop.transform.position, center) > 70;

                if (far || (view && ECCRuntimeVisuals.IsInView(view, cop.transform.position) == false && ECCRuntimeVisuals.FlatDistance(cop.transform.position, center) > 35))
                {
                    leaving.RemoveAt(index);
                    Destroy(cop.gameObject);
                }
            }
        }

        public int ChasingCops
        {
            get { return cops.Count; }
        }
    }
}
