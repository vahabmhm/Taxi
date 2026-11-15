using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A strip of spikes across the road. Any car that drives over it is hurt once
    /// </summary>
    public class ECCSpikeStrip : MonoBehaviour
    {
        public float damage = 1;
        public Vector2 size = new Vector2(8, 1.2f);

        readonly HashSet<ECCCar> hitCars = new HashSet<ECCCar>();

        public static ECCSpikeStrip Create(Vector3 position, Quaternion rotation, LayerMask groundLayer)
        {
            GameObject stripObject = new GameObject("SpikeStrip");
            stripObject.transform.position = ECCRuntimeVisuals.SnapToGround(position, groundLayer);
            stripObject.transform.rotation = rotation;

            ECCSpikeStrip strip = stripObject.AddComponent<ECCSpikeStrip>();

            Material baseMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.15f, 0.15f, 0.15f), 0);
            Material spikeMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.85f, 0.85f, 0.9f), 0.3f);
            Material stripeMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(1, 0.8f, 0.1f), 0.6f);

            // The strip lies across the road ( along the local x axis )
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, stripObject.transform, Vector3.up * 0.05f, new Vector3(strip.size.x, 0.1f, 0.6f), baseMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, stripObject.transform, new Vector3(0, 0.11f, 0.32f), new Vector3(strip.size.x, 0.02f, 0.08f), stripeMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, stripObject.transform, new Vector3(0, 0.11f, -0.32f), new Vector3(strip.size.x, 0.02f, 0.08f), stripeMaterial);

            for (float x = -strip.size.x * 0.5f + 0.3f; x < strip.size.x * 0.5f; x += 0.6f)
            {
                Transform spike = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, stripObject.transform, new Vector3(x, 0.2f, 0), new Vector3(0.12f, 0.25f, 0.12f), spikeMaterial);
                spike.localEulerAngles = new Vector3(45, 0, 45);
            }

            stripObject.AddComponent<ECCDespawnWhenFar>();

            return strip;
        }

        void Update()
        {
            for (int index = ECCCar.ActiveCars.Count - 1; index >= 0; index--)
            {
                ECCCar car = ECCCar.ActiveCars[index];

                if (car == null || car.health <= 0 || hitCars.Contains(car)) continue;

                Vector3 local = transform.InverseTransformPoint(car.transform.position);

                if (Mathf.Abs(local.x) < size.x * 0.5f && Mathf.Abs(local.z) < size.y)
                {
                    hitCars.Add(car);

                    if (car.hitEffect) Instantiate(car.hitEffect, car.transform.position, car.transform.rotation);

                    car.ChangeHealth(-damage);

                    if (car.IsPlayer) ECCRunMessages.FlatTire();
                }
            }
        }
    }
}
