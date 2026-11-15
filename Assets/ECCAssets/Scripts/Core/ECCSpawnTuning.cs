using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Adjusts the spawn groups of ECCSpawnAroundObject at runtime, without touching the values set in the scene:
    /// police spawn rate ( difficulty ) and which obstacles are spawned ( environment theme ).
    /// </summary>
    public class ECCSpawnTuning
    {
        public enum GroupKind { Police, Items, Obstacles }

        readonly ECCSpawnAroundObject spawner;
        readonly float[] baseSpawnRates;
        readonly Transform[][] baseSpawnObjects;
        readonly GroupKind[] kinds;

        public ECCSpawnAroundObject Spawner
        {
            get { return spawner; }
        }

        public ECCSpawnTuning(ECCSpawnAroundObject spawner)
        {
            this.spawner = spawner;

            int count = spawner && spawner.spawnGroups != null ? spawner.spawnGroups.Length : 0;

            baseSpawnRates = new float[count];
            baseSpawnObjects = new Transform[count][];
            kinds = new GroupKind[count];

            for (int index = 0; index < count; index++)
            {
                ECCSpawnAroundObject.SpawnGroup group = spawner.spawnGroups[index];

                baseSpawnRates[index] = group.spawnRate;
                baseSpawnObjects[index] = group.spawnObjects;
                kinds[index] = GetKind(group.spawnObjects);
            }
        }

        static GroupKind GetKind(Transform[] spawnObjects)
        {
            foreach (Transform spawnObject in spawnObjects)
            {
                if (spawnObject == null) continue;

                if (spawnObject.GetComponent<ECCCar>()) return GroupKind.Police;
                if (spawnObject.GetComponent<ECCItem>()) return GroupKind.Items;
            }

            return GroupKind.Obstacles;
        }

        /// <summary>
        /// Multiplies the time between spawns of a kind of group. Lower values spawn more often
        /// </summary>
        public void SetSpawnRateMultiplier(GroupKind kind, float multiplier)
        {
            for (int index = 0; index < kinds.Length; index++)
            {
                if (kinds[index] == kind) spawner.spawnGroups[index].spawnRate = baseSpawnRates[index] * Mathf.Max(0.05f, multiplier);
            }
        }

        /// <summary>
        /// Keeps only the obstacles whose name contains one of the keywords. If nothing matches, all the obstacles are kept
        /// </summary>
        public void FilterObstacles(string[] keywords)
        {
            for (int index = 0; index < kinds.Length; index++)
            {
                if (kinds[index] != GroupKind.Obstacles) continue;

                List<Transform> matches = new List<Transform>();

                foreach (Transform obstacle in baseSpawnObjects[index])
                {
                    if (obstacle == null) continue;

                    foreach (string keyword in keywords)
                    {
                        if (obstacle.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            matches.Add(obstacle);

                            break;
                        }
                    }
                }

                spawner.spawnGroups[index].spawnObjects = matches.Count > 0 ? matches.ToArray() : baseSpawnObjects[index];
                spawner.spawnGroups[index].spawnIndex = 0;
            }
        }

        /// <summary>
        /// Taxi: stops a kind of group from spawning at all ( the police only come after a violation, see TaxiPolice )
        /// </summary>
        public void ClearGroups(GroupKind kind)
        {
            for (int index = 0; index < kinds.Length; index++)
            {
                if (kinds[index] == kind)
                {
                    spawner.spawnGroups[index].spawnObjects = new Transform[0];
                    spawner.spawnGroups[index].spawnIndex = 0;
                }
            }
        }

        /// <summary>
        /// The police car prefabs the spawner uses. Prefers the ones named "Cop"
        /// </summary>
        public List<ECCCar> GetPolicePrefabs()
        {
            List<ECCCar> police = new List<ECCCar>();
            List<ECCCar> otherCars = new List<ECCCar>();

            for (int index = 0; index < kinds.Length; index++)
            {
                if (kinds[index] != GroupKind.Police) continue;

                foreach (Transform spawnObject in baseSpawnObjects[index])
                {
                    ECCCar car = spawnObject ? spawnObject.GetComponent<ECCCar>() : null;

                    if (car == null) continue;

                    if (car.name.Contains("Cop")) { if (police.Contains(car) == false) police.Add(car); }
                    else if (otherCars.Contains(car) == false) otherCars.Add(car);
                }
            }

            return police.Count > 0 ? police : otherCars;
        }
    }
}
