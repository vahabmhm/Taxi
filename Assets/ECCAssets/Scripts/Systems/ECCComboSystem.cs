using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Rewards daring driving: passing very close to a police car or a traffic car ( near miss, «از بیخ گوش» ) and wrecking
    /// police cars give a fixed bonus, which flies to the balance like the road's dollars. The near-miss message slides down from the top ( ECCRunMessages ); a wreck only shows its flying dollars.
    /// ( There used to be a combo multiplier here; it was removed because it didn't suit the game )
    /// </summary>
    public class ECCComboSystem : MonoBehaviour
    {
        [Tooltip("A police car closer than this counts as a close pass")]
        public float nearMissDistance = 3;

        [Tooltip("The close pass counts once the police car is this far again, without having hit the player")]
        public float nearMissExitDistance = 6;

        public int nearMissBonus = 5;
        public int copDestroyedBonus = 5;

        /// <summary>
        /// Called on every near miss
        /// </summary>
        public static event Action NearMiss;

        ECCGameController gameController;

        // Police cars that are passing close to the player, with the player's health when they got close
        readonly Dictionary<ECCCar, float> closeCars = new Dictionary<ECCCar, float>();
        readonly List<ECCCar> finishedCars = new List<ECCCar>();

        // The same for traffic cars ( they are plain objects, not ECCCar )
        readonly Dictionary<Transform, float> closeTraffic = new Dictionary<Transform, float>();
        readonly List<Transform> finishedTraffic = new List<Transform>();


        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            ECCCar.CarDestroyed += OnCarDestroyed;
        }

        void OnDestroy()
        {
            ECCCar.CarDestroyed -= OnCarDestroyed;
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0 && gameController.playerObject; }
        }

        void Update()
        {
            if (IsPlaying) DetectNearMisses();
        }

        void DetectNearMisses()
        {
            ECCCar player = gameController.playerObject;

            foreach (ECCCar car in ECCCar.ActiveCars)
            {
                if (car == null || car == player || car.health <= 0 || car.isStationary) continue;

                float distance = ECCRuntimeVisuals.FlatDistance(car.transform.position, player.transform.position);

                if (distance < nearMissDistance && closeCars.ContainsKey(car) == false) closeCars.Add(car, player.health);
            }

            finishedCars.Clear();

            foreach (KeyValuePair<ECCCar, float> closeCar in closeCars)
            {
                ECCCar car = closeCar.Key;

                if (car == null || car.health <= 0)
                {
                    finishedCars.Add(car);
                    continue;
                }

                if (ECCRuntimeVisuals.FlatDistance(car.transform.position, player.transform.position) > nearMissExitDistance)
                {
                    finishedCars.Add(car);

                    // Only a clean pass counts: the player wasn't hurt while the car was close
                    if (player.health >= closeCar.Value) CountNearMiss(car.transform.position);
                }
            }

            foreach (ECCCar car in finishedCars) closeCars.Remove(car);

            DetectTrafficNearMisses(player);
        }

        /// <summary>
        /// Close passes by the ordinary traffic count the same way. A traffic car that crashes is removed from the road,
        /// so a crash never counts as a close pass
        /// </summary>
        void DetectTrafficNearMisses(ECCCar player)
        {
            ECCTraffic traffic = ECCTraffic.Instance;

            if (traffic == null) return;

            for (int index = 0; index < traffic.CarCount; index++)
            {
                Transform car = traffic.GetCar(index);

                if (car == null || closeTraffic.ContainsKey(car)) continue;

                if (ECCRuntimeVisuals.FlatDistance(car.position, player.transform.position) < nearMissDistance) closeTraffic.Add(car, player.health);
            }

            finishedTraffic.Clear();

            foreach (KeyValuePair<Transform, float> close in closeTraffic)
            {
                Transform car = close.Key;

                if (car == null)
                {
                    finishedTraffic.Add(car);
                    continue;
                }

                if (ECCRuntimeVisuals.FlatDistance(car.position, player.transform.position) > nearMissExitDistance)
                {
                    finishedTraffic.Add(car);

                    if (player.health >= close.Value) CountNearMiss(car.position);
                }
            }

            foreach (Transform car in finishedTraffic) closeTraffic.Remove(car);
        }

        void CountNearMiss(Vector3 carPosition)
        {
            int bonus = AddBonus(nearMissBonus);

            ECCRunMessages.NearMiss(bonus);

            ECCHaptics.Vibrate(ECCHaptics.Kind.Light);

            // The bonus dollars fly from between the two cars to the balance, like the road's dollars
            ECCCar player = gameController.playerObject;

            if (player) ECCPickupEffects.MoneyToBalance(gameController, Vector3.Lerp(player.transform.position, carPosition, 0.5f) + Vector3.up, bonus);

            if (NearMiss != null) NearMiss();
        }

        void OnCarDestroyed(ECCCar car)
        {
            if (IsPlaying == false || car.IsPlayer) return;

            // No message ( it happens all the time ): the bonus dollars fly from the wreck to the balance with a «+5»
            ECCPickupEffects.MoneyToBalance(gameController, car.transform.position + Vector3.up, AddBonus(copDestroyedBonus));
        }

        int AddBonus(int bonus)
        {
            gameController.ChangeScore(bonus);

            return bonus;
        }
    }
}
