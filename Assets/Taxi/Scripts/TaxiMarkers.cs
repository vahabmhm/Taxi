using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The 3D markers of the taxi game, built from primitives: the pickup ring on the street next to a waiting
    /// passenger, the icon above the passenger's head, the destination beacon, and the roof sign of the player's taxi
    /// </summary>
    public static class TaxiMarkers
    {
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();

        public static Material Material(Color color, float emission = 0.9f)
        {
            Material material;

            if (materials.TryGetValue(color, out material) && material) return material;

            material = ECCRuntimeVisuals.CreateMaterial(color, emission);
            materials[color] = material;

            return material;
        }

        /// <summary>
        /// A flat ring on the street: an outer disc in the color with a darker disc inside ( reads as a ring from above )
        /// </summary>
        public static Transform CreateZone(Vector3 position, float radius, Color color)
        {
            GameObject zone = new GameObject("PickupZone");
            zone.transform.position = new Vector3(position.x, 0, position.z);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, zone.transform, Vector3.up * 0.04f, new Vector3(radius * 2, 0.02f, radius * 2), Material(color, 1.1f));
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, zone.transform, Vector3.up * 0.05f, new Vector3(radius * 1.6f, 0.02f, radius * 1.6f), Material(Color.Lerp(color, Color.black, 0.55f), 0.2f));

            // The perfect stop: a small bright dot in the middle
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, zone.transform, Vector3.up * 0.06f, new Vector3(1.2f, 0.02f, 1.2f), Material(Color.white, 1.2f));

            return zone.transform;
        }

        /// <summary>
        /// The icon above a passenger: a turning diamond in the color of the kind of passenger
        /// </summary>
        public static Transform CreateIcon(Transform parent, float height, Color color)
        {
            Transform icon = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, parent, Vector3.up * height, Vector3.one * 0.9f, Material(color, 1.3f));
            icon.localRotation = Quaternion.Euler(45, 0, 45);
            icon.name = "Icon";

            return icon;
        }

        /// <summary>
        /// The destination: a tall beam and a ring, in the passenger's color
        /// </summary>
        public static Transform CreateBeacon(Vector3 position, float radius, Color color)
        {
            Transform beacon = CreateZone(position, radius, color);
            beacon.name = "Destination";

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, beacon, Vector3.up * 12, new Vector3(0.9f, 12, 0.9f), Material(new Color(color.r, color.g, color.b, 1), 1.6f));

            // A flag at the top
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, beacon, new Vector3(1.2f, 22.5f, 0), new Vector3(2.2f, 1.4f, 0.15f), Material(Color.white, 1));

            return beacon;
        }

        /// <summary>
        /// A yellow TAXI sign on the roof of a car that doesn't have one ( any car the player drives is a taxi )
        /// </summary>
        public static void AddRoofSign(ECCCar car, Color color)
        {
            if (car == null || car.transform.Find("TaxiSign")) return;

            // The Kenney taxi has its own sign
            if (car.name.IndexOf("Taxi", System.StringComparison.OrdinalIgnoreCase) >= 0 && color == TaxiColors.PlayerYellow) return;

            Transform parent = car.baseTransform ? car.baseTransform : car.transform;

            GameObject sign = new GameObject("TaxiSign");
            sign.transform.SetParent(parent, false);

            float top = car.TopHeight;
            Vector3 scale = parent.lossyScale;
            float inverse = 1 / Mathf.Max(0.01f, scale.y);

            sign.transform.localPosition = Vector3.up * (top * inverse);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, sign.transform, Vector3.up * 0.25f * inverse, new Vector3(1.1f, 0.5f, 0.55f) * inverse, Material(color, 0.9f));
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, sign.transform, Vector3.up * 0.25f * inverse, new Vector3(0.8f, 0.3f, 0.6f) * inverse, Material(Color.black, 0));
        }

        /// <summary>
        /// Tints every renderer of a car ( the rival taxis get their own colors )
        /// </summary>
        public static void Tint(GameObject target, Color color)
        {
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer) continue;

                Material[] parts = renderer.materials;

                foreach (Material part in parts)
                {
                    if (part.HasProperty("_Color")) part.color = Color.Lerp(part.color, color, 0.55f) * 1.15f;
                }

                renderer.materials = parts;
            }
        }
    }

    /// <summary>
    /// The colors of the taxi game
    /// </summary>
    public static class TaxiColors
    {
        public static readonly Color PlayerYellow = new Color(1, 0.82f, 0.1f);
        public static readonly Color Normal = new Color(0.25f, 0.85f, 1);
        public static readonly Color Hurry = new Color(1, 0.35f, 0.25f);
        public static readonly Color Vip = new Color(1, 0.75f, 0.05f);
        public static readonly Color Tourist = new Color(0.55f, 0.45f, 1);
        public static readonly Color Family = new Color(0.35f, 1, 0.45f);
        public static readonly Color Grumpy = new Color(0.65f, 0.65f, 0.7f);

        public static readonly Color[] Rivals =
        {
            new Color(0.2f, 0.85f, 0.35f),
            new Color(0.25f, 0.55f, 1),
            new Color(1, 0.3f, 0.35f),
            new Color(0.85f, 0.35f, 1),
            new Color(1, 0.55f, 0.15f),
        };
    }
}
