using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCStyleCategory
    {
        Paint = 0,
        Rims = 1,
        Neon = 2,
        Trail = 3,
    }

    public class ECCStyleOption
    {
        public ECCStyleCategory category;
        public string id;          // Empty is the car's own look ( free )
        public string name;
        public Color color;
        public int price;
        public bool rainbow;       // Cycles through all the colors

        public string Key
        {
            get { return (int)category + ":" + id; }
        }
    }

    /// <summary>
    /// Car customization: paint, rims, a neon underglow and a colored tire trail, bought once with coins and then free to
    /// use on any owned car. Each car keeps its own choice ( saved per car ). The look is applied to the player's car when
    /// a chase starts and to the garage's preview car.
    /// Paint recolors the body materials ( glass, lights, trim and wheels keep their colors ); textured bodies are tinted.
    /// Rims are colored discs on both sides of every wheel ( they spin with it ). The neon and the trail are soft glows.
    /// </summary>
    public static class ECCCarStyle
    {
        static ECCStyleOption O(ECCStyleCategory category, string id, string name, int rgb, int price, bool rainbow = false)
        {
            return new ECCStyleOption { category = category, id = id, name = name, color = ECCCarTheme.Hex(rgb), price = price, rainbow = rainbow };
        }

        public static readonly ECCStyleOption[] All =
        {
            O(ECCStyleCategory.Paint, "", "رنگ کارخانه", 0xFFFFFF, 0),
            O(ECCStyleCategory.Paint, "red", "قرمز آتشین", 0xE53935, 300),
            O(ECCStyleCategory.Paint, "blue", "آبی دریایی", 0x1E6FE5, 300),
            O(ECCStyleCategory.Paint, "green", "سبز چمنی", 0x43A047, 300),
            O(ECCStyleCategory.Paint, "yellow", "زرد تاکسی", 0xFDD835, 400),
            O(ECCStyleCategory.Paint, "orange", "نارنجی", 0xFB8C00, 400),
            O(ECCStyleCategory.Paint, "purple", "بنفش", 0x8E24AA, 500),
            O(ECCStyleCategory.Paint, "pink", "صورتی", 0xEC407A, 500),
            O(ECCStyleCategory.Paint, "black", "مشکی براق", 0x262626, 600),
            O(ECCStyleCategory.Paint, "white", "سفید مرواریدی", 0xF2F2F2, 600),
            O(ECCStyleCategory.Paint, "gold", "طلایی", 0xD4AF37, 1500),

            O(ECCStyleCategory.Rims, "", "رینگ معمولی", 0x9E9E9E, 0),
            O(ECCStyleCategory.Rims, "chrome", "کروم", 0xE0E6EE, 400),
            O(ECCStyleCategory.Rims, "black", "مشکی اسپرت", 0x2A2A2A, 400),
            O(ECCStyleCategory.Rims, "red", "قرمز", 0xE53935, 500),
            O(ECCStyleCategory.Rims, "neon", "سبز نئونی", 0x39FF14, 700),
            O(ECCStyleCategory.Rims, "gold", "طلایی", 0xFFC107, 1200),

            O(ECCStyleCategory.Neon, "", "بدون نئون", 0x888888, 0),
            O(ECCStyleCategory.Neon, "blue", "نئون آبی", 0x00B0FF, 800),
            O(ECCStyleCategory.Neon, "pink", "نئون صورتی", 0xFF2D95, 800),
            O(ECCStyleCategory.Neon, "green", "نئون سبز", 0x39FF14, 800),
            O(ECCStyleCategory.Neon, "purple", "نئون بنفش", 0xB026FF, 900),
            O(ECCStyleCategory.Neon, "red", "نئون قرمز", 0xFF1744, 900),
            O(ECCStyleCategory.Neon, "rainbow", "رنگین‌کمان", 0xFFFFFF, 2000, true),

            O(ECCStyleCategory.Trail, "", "بدون رد", 0x888888, 0),
            O(ECCStyleCategory.Trail, "smoke", "دود سفید", 0xECEFF1, 600),
            O(ECCStyleCategory.Trail, "fire", "آتش", 0xFF6D00, 900),
            O(ECCStyleCategory.Trail, "ice", "یخ", 0x40C4FF, 900),
            O(ECCStyleCategory.Trail, "toxic", "سبز سمی", 0x76FF03, 900),
            O(ECCStyleCategory.Trail, "rainbow", "رنگین‌کمان", 0xFFFFFF, 2000, true),
        };

        public static string CategoryName(ECCStyleCategory category)
        {
            switch (category)
            {
                case ECCStyleCategory.Rims: return "رینگ";
                case ECCStyleCategory.Neon: return "نئون زیر ماشین";
                case ECCStyleCategory.Trail: return "رد لاستیک";
                default: return "رنگ";
            }
        }

        public static List<ECCStyleOption> InCategory(ECCStyleCategory category)
        {
            List<ECCStyleOption> options = new List<ECCStyleOption>();

            foreach (ECCStyleOption option in All)
            {
                if (option.category == category) options.Add(option);
            }

            return options;
        }

        public static ECCStyleOption Find(ECCStyleCategory category, string id)
        {
            foreach (ECCStyleOption option in All)
            {
                if (option.category == category && option.id == (id ?? string.Empty)) return option;
            }

            return null;
        }

        // ----- Ownership and choice -----

        public static bool IsOwned(ECCStyleOption option)
        {
            return option.price <= 0 || string.IsNullOrEmpty(option.id) || ECCSave.Data.styleOwned.Contains(option.Key);
        }

        /// <summary>
        /// Buys an option with coins. Returns false ( and spends nothing ) without enough coins
        /// </summary>
        public static bool Buy(ECCStyleOption option)
        {
            if (IsOwned(option)) return true;

            if (ECCWallet.TrySpend(option.price) == false) return false;

            ECCSave.Data.styleOwned.Add(option.Key);
            ECCSave.Save();

            return true;
        }

        static ECCCarStyleRecord GetRecord(string car, bool create)
        {
            foreach (ECCCarStyleRecord record in ECCSave.Data.carStyles)
            {
                if (record.car == car) return record;
            }

            if (create == false) return null;

            ECCCarStyleRecord newRecord = new ECCCarStyleRecord { car = car };
            ECCSave.Data.carStyles.Add(newRecord);

            return newRecord;
        }

        public static string GetChoice(string car, ECCStyleCategory category)
        {
            ECCCarStyleRecord record = GetRecord(CleanName(car), false);

            if (record == null) return string.Empty;

            switch (category)
            {
                case ECCStyleCategory.Rims: return record.rims ?? string.Empty;
                case ECCStyleCategory.Neon: return record.neon ?? string.Empty;
                case ECCStyleCategory.Trail: return record.trail ?? string.Empty;
                default: return record.paint ?? string.Empty;
            }
        }

        /// <summary>
        /// Puts an owned option on a car. Returns false for an option that isn't owned
        /// </summary>
        public static bool Equip(string car, ECCStyleOption option)
        {
            if (option == null || IsOwned(option) == false) return false;

            ECCCarStyleRecord record = GetRecord(CleanName(car), true);

            switch (option.category)
            {
                case ECCStyleCategory.Rims: record.rims = option.id; break;
                case ECCStyleCategory.Neon: record.neon = option.id; break;
                case ECCStyleCategory.Trail: record.trail = option.id; break;
                default: record.paint = option.id; break;
            }

            ECCSave.Save();

            return true;
        }

        static string CleanName(string car)
        {
            return string.IsNullOrEmpty(car) ? string.Empty : car.Replace("(Clone)", string.Empty).Trim();
        }

        // ----- Look -----

        /// <summary>
        /// Applies a car's saved look to a car object in the scene
        /// </summary>
        public static void ApplyTo(Component car, string carName)
        {
            if (car == null) return;

            ApplyTo(car.transform, Find(ECCStyleCategory.Paint, GetChoice(carName, ECCStyleCategory.Paint)), Find(ECCStyleCategory.Rims, GetChoice(carName, ECCStyleCategory.Rims)), Find(ECCStyleCategory.Neon, GetChoice(carName, ECCStyleCategory.Neon)), Find(ECCStyleCategory.Trail, GetChoice(carName, ECCStyleCategory.Trail)));
        }

        /// <summary>
        /// Applies a look ( null options are the car's own look ). Can be called again on the same car to change it
        /// </summary>
        public static void ApplyTo(Transform car, ECCStyleOption paint, ECCStyleOption rims, ECCStyleOption neon, ECCStyleOption trail)
        {
            if (car == null) return;

            ECCCarStyleState state = car.GetComponent<ECCCarStyleState>();
            if (state == null) state = car.gameObject.AddComponent<ECCCarStyleState>();

            state.ApplyPaint(paint != null && string.IsNullOrEmpty(paint.id) == false ? paint : null);
            state.ApplyRims(rims != null && string.IsNullOrEmpty(rims.id) == false ? rims : null);
            state.ApplyNeon(neon != null && string.IsNullOrEmpty(neon.id) == false ? neon : null);
            state.ApplyTrail(trail != null && string.IsNullOrEmpty(trail.id) == false ? trail : null);
        }

        /// <summary>
        /// A color that moves through the rainbow over time
        /// </summary>
        public static Color Rainbow(float offset)
        {
            float hue = Mathf.Repeat(Time.time * 0.25f + offset, 1);

            return Color.HSVToRGB(hue, 0.85f, 1);
        }
    }

    /// <summary>
    /// Keeps a car's original paint colors and the objects added for its look, so the look can be changed again
    /// </summary>
    public class ECCCarStyleState : MonoBehaviour
    {
        static readonly string[] KeepColorParts = { "glass", "light", "wheel", "trim", "tire", "tyre", "dust", "window", "burn", "lamp", "siren" };

        readonly Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
        readonly List<Material> paintable = new List<Material>();
        bool collected;

        readonly List<GameObject> rimObjects = new List<GameObject>();
        GameObject neonObject;
        readonly List<TrailRenderer> trails = new List<TrailRenderer>();

        ECCStyleOption neonOption;
        ECCStyleOption trailOption;
        Renderer neonRenderer;

        Transform Chassis
        {
            get
            {
                Transform baseTransform = transform.Find("Base");
                Transform chassis = baseTransform ? baseTransform.Find("Chasis") : null;

                return chassis;
            }
        }

        /// <summary>
        /// The body's size in meters along the car's axes ( from the chassis mesh )
        /// </summary>
        Vector3 BodySize
        {
            get
            {
                Transform chassis = Chassis;
                MeshFilter filter = chassis ? chassis.GetComponent<MeshFilter>() : null;

                if (filter && filter.sharedMesh)
                {
                    Vector3 size = filter.sharedMesh.bounds.size;
                    Vector3 scale = chassis.lossyScale;

                    return new Vector3(Mathf.Abs(size.x * scale.x), Mathf.Abs(size.y * scale.y), Mathf.Abs(size.z * scale.z));
                }

                return new Vector3(2.2f, 1.5f, 4.4f);
            }
        }

        bool IsWheelPart(Transform part)
        {
            ECCCar car = GetComponent<ECCCar>();

            for (Transform current = part; current && current != transform; current = current.parent)
            {
                if (current.name.StartsWith("Wheel") || current.name.StartsWith("Style_")) return true;

                if (car && car.wheels != null)
                {
                    foreach (Transform wheel in car.wheels)
                    {
                        if (wheel == current) return true;
                    }
                }
            }

            return false;
        }

        void CollectPaintable()
        {
            if (collected) return;

            collected = true;

            foreach (MeshRenderer part in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (IsWheelPart(part.transform)) continue;

                foreach (Material material in part.materials)
                {
                    if (material == null || material.HasProperty("_Color") == false) continue;

                    string name = material.name.ToLowerInvariant();
                    bool keep = false;

                    foreach (string word in KeepColorParts)
                    {
                        if (name.Contains(word)) keep = true;
                    }

                    if (keep) continue;

                    originalColors[material] = material.color;
                    paintable.Add(material);
                }
            }
        }

        public void ApplyPaint(ECCStyleOption paint)
        {
            CollectPaintable();

            for (int index = 0; index < paintable.Count; index++)
            {
                Material material = paintable[index];

                if (material == null) continue;

                if (paint == null)
                {
                    material.color = originalColors[material];
                    continue;
                }

                // Two-tone bodies keep a lighter second color; a textured body is tinted lightly so its details stay
                Color color = index == 0 ? paint.color : Color.Lerp(paint.color, Color.white, 0.45f);

                if (material.mainTexture != null) color = Color.Lerp(color, Color.white, 0.2f);

                material.color = new Color(color.r, color.g, color.b, originalColors[material].a);
            }
        }

        public void ApplyRims(ECCStyleOption rims)
        {
            foreach (GameObject rim in rimObjects)
            {
                if (rim) Destroy(rim);
            }

            rimObjects.Clear();

            if (rims == null) return;

            ECCCar car = GetComponent<ECCCar>();

            if (car == null || car.wheels == null) return;

            Material rimMaterial = ECCRuntimeVisuals.CreateMaterial(rims.color, 0.35f);
            Material hubMaterial = ECCRuntimeVisuals.CreateMaterial(Color.Lerp(rims.color, Color.black, 0.45f), 0.1f);

            foreach (Transform wheel in car.wheels)
            {
                if (wheel == null) continue;

                Transform wheelObject = wheel.Find("WheelObject");
                if (wheelObject == null) wheelObject = wheel;

                MeshFilter filter = wheelObject.GetComponentInChildren<MeshFilter>();

                if (filter == null || filter.sharedMesh == null) continue;

                Bounds bounds = filter.sharedMesh.bounds;
                Transform meshTransform = filter.transform;

                GameObject holder = new GameObject("Style_Rims");
                holder.transform.SetParent(meshTransform, false);
                holder.transform.localPosition = bounds.center;

                float diameter = Mathf.Min(bounds.size.y, bounds.size.z);
                float thickness = Mathf.Max(0.01f, bounds.size.x * 0.06f);

                for (int side = -1; side <= 1; side += 2)
                {
                    // A cylinder's axis is Y; turned 90 degrees on Z it lies along the axle ( X )
                    Transform disc = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, holder.transform, new Vector3(side * (bounds.size.x * 0.5f + thickness * 0.5f), 0, 0), new Vector3(diameter * 0.66f, thickness, diameter * 0.66f), rimMaterial);
                    disc.localRotation = Quaternion.Euler(0, 0, 90);

                    Transform hub = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, holder.transform, new Vector3(side * (bounds.size.x * 0.5f + thickness * 1.5f), 0, 0), new Vector3(diameter * 0.22f, thickness, diameter * 0.22f), hubMaterial);
                    hub.localRotation = Quaternion.Euler(0, 0, 90);

                    // Spokes across the disc, so the spinning shows
                    Transform spoke = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, holder.transform, new Vector3(side * (bounds.size.x * 0.5f + thickness * 1.2f), 0, 0), new Vector3(thickness, diameter * 0.6f, diameter * 0.1f), hubMaterial);
                    spoke.localRotation = Quaternion.identity;
                }

                rimObjects.Add(holder);
            }
        }

        public void ApplyNeon(ECCStyleOption neon)
        {
            neonOption = neon;

            if (neonObject) Destroy(neonObject);

            neonObject = null;
            neonRenderer = null;

            if (neon == null) return;

            Vector3 size = BodySize;

            neonObject = new GameObject("Style_Neon");
            neonObject.transform.SetParent(transform, false);

            Vector3 scale = transform.lossyScale;
            neonObject.transform.localScale = new Vector3(1 / Mathf.Max(0.01f, scale.x), 1 / Mathf.Max(0.01f, scale.y), 1 / Mathf.Max(0.01f, scale.z));

            Color color = neon.rainbow ? Color.white : new Color(neon.color.r, neon.color.g, neon.color.b, 0.85f);

            Transform glow = ECCGlow.CreateFlat("Glow", neonObject.transform, new Vector3(0, 0.06f, 0), new Vector2(size.x * 1.9f, size.z * 1.45f), neon.rainbow ? NewRainbowMaterial() : ECCGlow.RadialMaterial(color));

            if (glow) neonRenderer = glow.GetComponent<Renderer>();
        }

        // The rainbow neon changes color, so it has its own material
        static Material NewRainbowMaterial()
        {
            Material shared = ECCGlow.RadialMaterial(Color.white);

            return shared ? new Material(shared) : null;
        }

        public void ApplyTrail(ECCStyleOption trail)
        {
            trailOption = trail;

            foreach (TrailRenderer old in trails)
            {
                if (old) Destroy(old.gameObject);
            }

            trails.Clear();

            if (trail == null) return;

            ECCCar car = GetComponent<ECCCar>();

            Material material = ECCGlow.RadialMaterial(Color.white);

            if (material == null) return;

            Vector3 size = BodySize;

            // Behind both rear wheels ( the back of the body if the wheels are unknown )
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject holder = new GameObject("Style_Trail");
                holder.transform.SetParent(transform, false);

                Vector3 scale = transform.lossyScale;
                holder.transform.localPosition = new Vector3(side * size.x * 0.35f / Mathf.Max(0.01f, scale.x), 0.15f / Mathf.Max(0.01f, scale.y), -size.z * 0.38f / Mathf.Max(0.01f, scale.z));

                TrailRenderer renderer = holder.AddComponent<TrailRenderer>();
                renderer.sharedMaterial = material;
                renderer.time = 0.7f;
                renderer.startWidth = size.x * 0.28f;
                renderer.endWidth = 0;
                renderer.minVertexDistance = 0.3f;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                SetTrailColor(renderer, trail.rainbow ? ECCCarStyle.Rainbow(0) : trail.color);

                trails.Add(renderer);
            }
        }

        static void SetTrailColor(TrailRenderer renderer, Color color)
        {
            renderer.startColor = new Color(color.r, color.g, color.b, 0.85f);
            renderer.endColor = new Color(color.r, color.g, color.b, 0);
        }

        void Update()
        {
            if (neonOption != null && neonRenderer)
            {
                // A soft breathing glow; the rainbow one also changes color
                float pulse = 0.75f + Mathf.Sin(Time.time * 3) * 0.2f;
                Color color = neonOption.rainbow ? ECCCarStyle.Rainbow(0) : neonOption.color;

                if (neonOption.rainbow && neonRenderer.sharedMaterial && neonRenderer.sharedMaterial.HasProperty("_TintColor"))
                {
                    neonRenderer.sharedMaterial.SetColor("_TintColor", new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.42f * pulse));
                }

                neonRenderer.transform.localScale = new Vector3(neonRenderer.transform.localScale.x, neonRenderer.transform.localScale.y, 1);
            }

            if (trailOption != null && trailOption.rainbow)
            {
                Color color = ECCCarStyle.Rainbow(0);

                foreach (TrailRenderer renderer in trails)
                {
                    if (renderer) SetTrailColor(renderer, color);
                }
            }
        }
    }
}
