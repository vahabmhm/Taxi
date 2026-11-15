using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A power-up lying on the ground. It floats and spins; the ECCPowerUpManager checks when the player reaches it
    /// </summary>
    public class ECCPowerUpPickup : MonoBehaviour
    {
        public ECCPowerUpType type;

        Transform model;
        float bobOffset;

        public static ECCPowerUpPickup Create(ECCPowerUpType type, Vector3 position)
        {
            GameObject pickupObject = new GameObject("PowerUp" + type);
            pickupObject.transform.position = position;

            ECCPowerUpPickup pickup = pickupObject.AddComponent<ECCPowerUpPickup>();
            pickup.type = type;
            pickup.bobOffset = Random.value * 10;

            Color color = ECCPowerUpManager.GetColor(type);
            Material material = ECCRuntimeVisuals.CreateMaterial(color, 1);

            // A glowing ring on the ground makes the power-up easy to spot
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, pickupObject.transform, Vector3.up * 0.03f, new Vector3(2.4f, 0.02f, 2.4f), ECCRuntimeVisuals.CreateMaterial(color, 0.6f));

            pickup.model = new GameObject("Model").transform;
            pickup.model.SetParent(pickupObject.transform, false);
            pickup.model.localPosition = Vector3.up * 1.2f;

            // Each power-up has its own shape
            switch (type)
            {
                case ECCPowerUpType.Nitro:
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Capsule, pickup.model, Vector3.zero, new Vector3(0.5f, 0.6f, 0.5f), material).localEulerAngles = Vector3.right * 90;
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, pickup.model, new Vector3(0, 0, -0.65f), new Vector3(0.6f, 0.1f, 0.6f), material).localEulerAngles = Vector3.right * 90;
                    break;

                case ECCPowerUpType.Shield:
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, pickup.model, Vector3.zero, Vector3.one * 0.9f, material);
                    break;

                case ECCPowerUpType.EMP:
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, pickup.model, Vector3.zero, Vector3.one * 0.7f, material).localEulerAngles = new Vector3(45, 0, 45);
                    break;

                default:
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, pickup.model, new Vector3(-0.3f, 0, 0), new Vector3(0.22f, 0.45f, 0.22f), material);
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, pickup.model, new Vector3(0.3f, 0, 0), new Vector3(0.22f, 0.45f, 0.22f), material);
                    ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, pickup.model, new Vector3(0, -0.45f, 0), new Vector3(0.82f, 0.2f, 0.22f), material);
                    break;
            }

            pickupObject.AddComponent<ECCDespawnWhenFar>().distance = 70;

            return pickup;
        }

        void Update()
        {
            if (model == null) return;

            model.localPosition = Vector3.up * (1.2f + Mathf.Sin(Time.time * 3 + bobOffset) * 0.25f);
            model.Rotate(Vector3.up * 120 * Time.deltaTime, Space.World);
        }
    }
}
