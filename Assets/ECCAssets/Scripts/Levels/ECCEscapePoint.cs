using UnityEngine;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// The goal of escape levels: a glowing beam of light the player must drive into
    /// </summary>
    public class ECCEscapePoint : MonoBehaviour
    {
        [Tooltip("How close the player must get to the center of the escape point")]
        public float reachRadius = 4;

        Transform ring;

        // The bag of money of heist levels, bobbing over the ring
        Transform loot;

        public static readonly Color EscapeColor = new Color(0.2f, 1, 0.4f, 1);
        public static readonly Color LootColor = new Color(1, 0.78f, 0.15f, 1);
        public static readonly Color CheckpointColor = new Color(0.25f, 0.75f, 1, 1);

        public static ECCEscapePoint Create(Vector3 position)
        {
            return Create(position, EscapeColor, "EscapePoint");
        }

        /// <summary>
        /// The heist's bag of money: a gold beam with a bag over the ring
        /// </summary>
        public static ECCEscapePoint CreateLoot(Vector3 position)
        {
            ECCEscapePoint point = Create(position, LootColor, "Loot");

            Material bagMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.45f, 0.3f, 0.12f), 0.3f);
            Material moneyMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.3f, 0.75f, 0.3f), 0.6f);

            point.loot = new GameObject("Bag").transform;
            point.loot.SetParent(point.transform, false);
            point.loot.localPosition = Vector3.up * 2.2f;

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, point.loot, Vector3.zero, new Vector3(2.2f, 1.9f, 1.6f), bagMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, point.loot, new Vector3(0, 1.05f, 0), new Vector3(0.7f, 0.25f, 0.7f), bagMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, point.loot, new Vector3(0, 0, -0.81f), new Vector3(0.9f, 0.5f, 0.05f), moneyMaterial);

            return point;
        }

        /// <summary>
        /// A checkpoint: a blue beam and ring
        /// </summary>
        public static ECCEscapePoint CreateCheckpoint(Vector3 position)
        {
            ECCEscapePoint point = Create(position, CheckpointColor, "Checkpoint");
            point.reachRadius = 5;

            return point;
        }

        public static ECCEscapePoint Create(Vector3 position, Color green, string name)
        {
            GameObject escapeObject = new GameObject(name);
            escapeObject.transform.position = position;

            ECCEscapePoint escapePoint = escapeObject.AddComponent<ECCEscapePoint>();

            Material beamMaterial = ECCRuntimeVisuals.CreateMaterial(green, 1.5f);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, escapeObject.transform, Vector3.up * 15, new Vector3(1.2f, 15, 1.2f), beamMaterial);

            escapePoint.ring = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, escapeObject.transform, Vector3.up * 0.05f, new Vector3(escapePoint.reachRadius * 2, 0.05f, escapePoint.reachRadius * 2), ECCRuntimeVisuals.CreateMaterial(green, 0.8f));

            return escapePoint;
        }

        void Update()
        {
            // Pulse the ring on the ground
            if (ring)
            {
                float size = reachRadius * 2 * (1 + Mathf.Sin(Time.time * 4) * 0.1f);

                ring.localScale = new Vector3(size, 0.05f, size);
            }

            if (loot)
            {
                loot.localPosition = Vector3.up * (2.2f + Mathf.Sin(Time.time * 3) * 0.4f);
                loot.Rotate(Vector3.up, 90 * Time.deltaTime, Space.World);
            }
        }

        public bool IsReachedBy(Transform target)
        {
            return target && ECCRuntimeVisuals.FlatDistance(target.position, transform.position) <= reachRadius;
        }
    }
}
