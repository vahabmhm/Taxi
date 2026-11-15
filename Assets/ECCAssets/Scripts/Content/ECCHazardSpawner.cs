using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Places hazards on the road ahead of the player: spike strips ( which also hurt the police ) and police roadblocks.
    /// The game modes turn each hazard on or off.
    /// </summary>
    public class ECCHazardSpawner : MonoBehaviour
    {
        public bool spikeStrips = false;
        public bool roadblocks = false;

        [Tooltip("Seconds between spike strips")]
        public float spikeInterval = 14;

        [Tooltip("Seconds between roadblocks")]
        public float roadblockInterval = 22;

        [Tooltip("How far ahead of the player hazards are placed")]
        public float distanceAhead = 30;

        ECCGameController gameController;
        List<ECCCar> policePrefabs;

        float spikeTimer;
        float roadblockTimer;

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            // The first hazards come a bit later than the interval, so the chase can get going
            spikeTimer = spikeInterval * 0.6f;
            roadblockTimer = roadblockInterval * 0.6f;
        }

        void Update()
        {
            if (gameController == null || gameController.gameStarted == false || gameController.isGameOver || gameController.startDelay > 0 || gameController.playerObject == null) return;

            if (spikeStrips)
            {
                spikeTimer -= Time.deltaTime;

                if (spikeTimer <= 0)
                {
                    spikeTimer = spikeInterval * Random.Range(0.8f, 1.2f);

                    Transform player = gameController.playerObject.transform;

                    ECCSpikeStrip.Create(AheadOfPlayer(player), Quaternion.LookRotation(FlatForward(player)), gameController.groundLayer);
                }
            }

            if (roadblocks)
            {
                roadblockTimer -= Time.deltaTime;

                if (roadblockTimer <= 0)
                {
                    roadblockTimer = roadblockInterval * Random.Range(0.8f, 1.2f);

                    SpawnRoadblock();
                }
            }
        }

        static Vector3 FlatForward(Transform player)
        {
            Vector3 forward = player.forward;
            forward.y = 0;

            return forward.normalized;
        }

        Vector3 AheadOfPlayer(Transform player)
        {
            return player.position + FlatForward(player) * distanceAhead;
        }

        void SpawnRoadblock()
        {
            if (policePrefabs == null)
            {
                ECCSpawnAroundObject spawner = GetComponent<ECCSpawnAroundObject>();

                policePrefabs = spawner ? new ECCSpawnTuning(spawner).GetPolicePrefabs() : new List<ECCCar>();
            }

            if (policePrefabs.Count == 0) return;

            Transform player = gameController.playerObject.transform;

            Vector3 forward = FlatForward(player);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            // Ahead of the player, out of the camera's view ( all three cars ), so the roadblock doesn't pop up on screen
            Vector3 center = ECCRuntimeVisuals.OutOfView(player.position, forward, distanceAhead, gameController.groundLayer);
            Camera view = Camera.main;
            float extra = 0;

            while (view && extra < 60 && (ECCRuntimeVisuals.IsInView(view, center + right * 4.2f) || ECCRuntimeVisuals.IsInView(view, center - right * 4.2f)))
            {
                extra += 3;
                center = ECCRuntimeVisuals.OutOfView(player.position, forward, distanceAhead + extra, gameController.groundLayer);
            }

            // Three cars parked side by side, across the road
            for (int index = -1; index <= 1; index++)
            {
                Vector3 position = ECCRuntimeVisuals.SnapToGround(center + right * index * 4.2f, gameController.groundLayer);

                ECCCar prefab = policePrefabs[Random.Range(0, policePrefabs.Count)];

                ECCCar car = Instantiate(prefab, position, Quaternion.LookRotation(right));
                car.isStationary = true;
                car.avoidObstacles = false;

                car.gameObject.AddComponent<ECCDespawnWhenFar>();
            }

            ECCRunMessages.Roadblock();
        }
    }
}
