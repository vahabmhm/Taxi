using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using EndlessCarChase.Types;

namespace EndlessCarChase.EditorTools
{
    /// <summary>
    /// Builds garage cars from the Kenney Car Kit models ( CC0, Assets/ThirdParty/KenneyCarKit ).
    /// Each car is a copy of the Sedan prefab ( so it has the same ECCCar setup, dust and effects ) with:
    /// - Base/Chasis: the body and its extra parts baked into one mesh ( ECCShop darkens Base/Chasis for locked cars,
    ///   and ECCCar leans it, so it must have no rotation of its own )
    /// - WheelFR / WheelFL / WheelBR / WheelBL: at the Kenney wheel centers; their WheelObject has the wheel mesh centered,
    ///   so it spins around its axle
    /// - a box collider and stats that fit the car
    /// The cars and their prices are listed in Resources/ECCExtraCars, which ECCShop appends after the scene's cars.
    /// It runs once by itself when the cars are missing; Tools > Endless Car Chase > Build Kenney Cars rebuilds them.
    /// </summary>
    [InitializeOnLoad]
    public static class ECCKenneyCarBuilder
    {
        const string ModelFolder = "Assets/ThirdParty/KenneyCarKit/Models";
        const string TexturePath = ModelFolder + "/Textures/colormap.png";
        const string MaterialPath = "Assets/ThirdParty/KenneyCarKit/KenneyColormap.mat";
        const string OutputFolder = "Assets/ThirdParty/KenneyCarKit/Cars";
        const string TemplatePath = "Assets/ECCAssets/Prefabs/Cars/Sedan.prefab";
        const string ExtraCarsPath = "Assets/ECCAssets/Resources/ECCExtraCars.asset";

        // The length of the cars in the game ( the Sedan is 2 units long )
        const float TargetLength = 2.1f;

        struct CarSpec
        {
            public string model;
            public string prefab;
            public int price;
            public float health;
            public float speed;
            public float rotateSpeed;
            public int damage;

            public CarSpec(string model, string prefab, int price, float health, float speed, float rotateSpeed, int damage)
            {
                this.model = model;
                this.prefab = prefab;
                this.price = price;
                this.health = health;
                this.speed = speed;
                this.rotateSpeed = rotateSpeed;
                this.damage = damage;
            }
        }

        // Cheapest first; stats next to the garage's other cars ( health 4-10, speed 14-20, damage 3-10 )
        static readonly CarSpec[] Cars =
        {
            new CarSpec("hatchback-sports", "Kenney_HatchbackSports", 1500, 5, 17, 210, 4),
            new CarSpec("taxi", "Kenney_Taxi", 2500, 7, 16, 190, 6),
            new CarSpec("sedan-sports", "Kenney_SedanSports", 4000, 7, 18, 200, 6),
            new CarSpec("suv-luxury", "Kenney_SuvLuxury", 6000, 11, 16, 170, 9),
            new CarSpec("race", "Kenney_Race", 8000, 6, 21, 220, 5),
            new CarSpec("race-future", "Kenney_RaceFuture", 12000, 9, 22, 230, 8),
        };

        static ECCKenneyCarBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            // Once: when the models are in the project but the car list isn't built yet
            if (AssetDatabase.LoadAssetAtPath<ECCExtraCars>(ExtraCarsPath) != null) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelFolder + "/" + Cars[0].model + ".fbx") == null) return;
            if (SessionState.GetBool("ECCKenneyCarBuilder.Tried", false)) return;

            SessionState.SetBool("ECCKenneyCarBuilder.Tried", true);

            Build();
        }

        [MenuItem("Tools/Endless Car Chase/Build Kenney Cars")]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePath) == null)
            {
                Debug.LogError("ECCKenneyCarBuilder: the template car is missing: " + TemplatePath);
                return;
            }

            if (AssetDatabase.IsValidFolder(OutputFolder) == false) AssetDatabase.CreateFolder("Assets/ThirdParty/KenneyCarKit", "Cars");

            Material material = GetMaterial();

            List<ShopItem> items = new List<ShopItem>();

            foreach (CarSpec spec in Cars)
            {
                GameObject car = BuildCar(spec, material);

                if (car) items.Add(new ShopItem { itemIcon = car.transform, lockState = 0, price = spec.price });
            }

            ECCExtraCars extras = AssetDatabase.LoadAssetAtPath<ECCExtraCars>(ExtraCarsPath);

            if (extras == null)
            {
                extras = ScriptableObject.CreateInstance<ECCExtraCars>();
                AssetDatabase.CreateAsset(extras, ExtraCarsPath);
            }

            extras.items = items.ToArray();
            EditorUtility.SetDirty(extras);

            AssetDatabase.SaveAssets();

            Debug.Log("ECCKenneyCarBuilder: built " + items.Count + " garage cars in " + OutputFolder + " and listed them in " + ExtraCarsPath);
        }

        static Material GetMaterial()
        {
            TextureImporter textureImporter = AssetImporter.GetAtPath(TexturePath) as TextureImporter;

            // The color map is made of flat color cells: no mipmaps, so far cars don't bleed colors
            if (textureImporter && textureImporter.mipmapEnabled)
            {
                textureImporter.mipmapEnabled = false;
                textureImporter.SaveAndReimport();
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            if (material == null)
            {
                // The built-in pipeline's Standard shader, like the game's own car materials ( ECCShop sets _Color on it )
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            material.SetFloat("_Glossiness", 0.3f);
            EditorUtility.SetDirty(material);

            return material;
        }

        static GameObject BuildCar(CarSpec spec, Material material)
        {
            string modelPath = ModelFolder + "/" + spec.model + ".fbx";

            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

            if (importer == null)
            {
                Debug.LogWarning("ECCKenneyCarBuilder: model not found: " + modelPath);
                return null;
            }

            // The meshes are baked from code, and the shared color map material is used instead of the embedded ones
            if (importer.isReadable == false || importer.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                importer.isReadable = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.SaveAndReimport();
            }

            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath));
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            try
            {
                return BuildFromModel(spec, model, material);
            }
            finally
            {
                Object.DestroyImmediate(model);
            }
        }

        static GameObject BuildFromModel(CarSpec spec, GameObject model, Material material)
        {
            List<MeshFilter> bodyParts = new List<MeshFilter>();
            List<MeshFilter> wheels = new List<MeshFilter>();

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;

                if (filter.name.ToLowerInvariant().Contains("wheel")) wheels.Add(filter);
                else bodyParts.Add(filter);
            }

            if (bodyParts.Count == 0 || wheels.Count < 4)
            {
                Debug.LogWarning("ECCKenneyCarBuilder: " + spec.model + " needs a body and four wheels, skipped");
                return null;
            }

            // Turn the car to face +Z ( the game's forward ) if its front wheels are on the -Z side
            float front = 0, back = 0;

            foreach (MeshFilter wheel in wheels)
            {
                float z = WorldBounds(wheel, Matrix4x4.identity).center.z;

                if (wheel.name.ToLowerInvariant().Contains("front")) front += z;
                else back += z;
            }

            Matrix4x4 turn = Matrix4x4.Rotate(Quaternion.Euler(0, front < back ? 180 : 0, 0));

            // Scale to the game's car length, stand on the ground and center the body
            Bounds raw = WorldBounds(bodyParts, turn);

            foreach (MeshFilter wheel in wheels) raw.Encapsulate(WorldBounds(wheel, turn));

            float scale = TargetLength / Mathf.Max(0.01f, raw.size.z);

            Matrix4x4 fit = Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(new Vector3(-raw.center.x, -raw.min.y, -raw.center.z)) * turn;

            Bounds body = WorldBounds(bodyParts, fit);

            string meshPath = OutputFolder + "/" + spec.prefab + "_Meshes.asset";

            AssetDatabase.DeleteAsset(meshPath);

            Mesh bodyMesh = Combine(spec.prefab + "_Body", bodyParts, fit, Vector3.zero);
            AssetDatabase.CreateAsset(bodyMesh, meshPath);

            // The template's wheels: front first ( ECCCar steers the first frontWheels wheels )
            string[] slots = { "WheelFR", "WheelFL", "WheelBR", "WheelBL" };
            MeshFilter[] slotWheels = new MeshFilter[4];

            foreach (MeshFilter wheel in wheels)
            {
                Vector3 center = WorldBounds(wheel, fit).center;

                int slot = (center.z > body.center.z ? 0 : 2) + (center.x > 0 ? 0 : 1);

                if (slotWheels[slot] == null) slotWheels[slot] = wheel;
            }

            GameObject car = PrefabUtility.LoadPrefabContents(TemplatePath);

            try
            {
                Transform baseTransform = car.transform.Find("Base");
                Transform chassis = baseTransform ? baseTransform.Find("Chasis") : null;

                if (chassis == null)
                {
                    Debug.LogWarning("ECCKenneyCarBuilder: the template has no Base/Chasis, " + spec.model + " skipped");
                    return null;
                }

                chassis.localPosition = Vector3.zero;
                chassis.localRotation = Quaternion.identity;
                chassis.localScale = Vector3.one;
                chassis.GetComponent<MeshFilter>().sharedMesh = bodyMesh;
                chassis.GetComponent<MeshRenderer>().sharedMaterials = new[] { material };

                float rearZ = 0;

                for (int slot = 0; slot < slots.Length; slot++)
                {
                    Transform holder = baseTransform.Find(slots[slot]);
                    MeshFilter source = slotWheels[slot];

                    if (holder == null) continue;

                    if (source == null)
                    {
                        holder.gameObject.SetActive(false);
                        continue;
                    }

                    Vector3 center = WorldBounds(source, fit).center;

                    holder.localPosition = center;
                    holder.localRotation = Quaternion.identity;
                    holder.localScale = Vector3.one;

                    Mesh wheelMesh = Combine(spec.prefab + "_" + slots[slot], new List<MeshFilter> { source }, fit, center);
                    AssetDatabase.AddObjectToAsset(wheelMesh, meshPath);

                    Transform wheelObject = holder.Find("WheelObject");

                    if (wheelObject)
                    {
                        wheelObject.localPosition = Vector3.zero;
                        wheelObject.localRotation = Quaternion.identity;
                        wheelObject.localScale = Vector3.one;
                        wheelObject.GetComponent<MeshFilter>().sharedMesh = wheelMesh;
                        wheelObject.GetComponent<MeshRenderer>().sharedMaterials = new[] { material };
                    }

                    if (slot >= 2) rearZ = center.z;
                }

                // The dust behind the rear wheels
                Transform dust = baseTransform.Find("Dust");
                if (dust) dust.localPosition = new Vector3(0, 0, rearZ - 0.2f);

                // A low box, like the game's other cars
                BoxCollider collider = car.GetComponent<BoxCollider>();

                if (collider)
                {
                    collider.size = new Vector3(body.size.x * 0.95f, body.size.y * 0.5f, body.size.z * 0.95f);
                    collider.center = new Vector3(0, body.size.y * 0.25f, body.center.z);
                }

                ECCCar stats = car.GetComponent<ECCCar>();

                if (stats)
                {
                    stats.health = spec.health;
                    stats.speed = spec.speed;
                    stats.rotateSpeed = spec.rotateSpeed;
                    stats.damage = spec.damage;
                }

                return PrefabUtility.SaveAsPrefabAsset(car, OutputFolder + "/" + spec.prefab + ".prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(car);
            }
        }

        /// <summary>
        /// Bakes parts into one mesh in the car's space ( transform ), moved so "origin" becomes the mesh's zero
        /// </summary>
        static Mesh Combine(string name, List<MeshFilter> parts, Matrix4x4 transform, Vector3 origin)
        {
            List<CombineInstance> instances = new List<CombineInstance>();

            Matrix4x4 shift = Matrix4x4.Translate(-origin);

            foreach (MeshFilter part in parts)
            {
                for (int subMesh = 0; subMesh < part.sharedMesh.subMeshCount; subMesh++)
                {
                    instances.Add(new CombineInstance
                    {
                        mesh = part.sharedMesh,
                        subMeshIndex = subMesh,
                        transform = shift * transform * part.transform.localToWorldMatrix,
                    });
                }
            }

            Mesh mesh = new Mesh { name = name };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(instances.ToArray(), true, true);
            mesh.RecalculateBounds();

            return mesh;
        }

        static Bounds WorldBounds(List<MeshFilter> parts, Matrix4x4 transform)
        {
            Bounds bounds = WorldBounds(parts[0], transform);

            for (int index = 1; index < parts.Count; index++) bounds.Encapsulate(WorldBounds(parts[index], transform));

            return bounds;
        }

        static Bounds WorldBounds(MeshFilter part, Matrix4x4 transform)
        {
            Matrix4x4 matrix = transform * part.transform.localToWorldMatrix;

            Vector3[] vertices = part.sharedMesh.vertices;

            Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(vertices.Length > 0 ? vertices[0] : Vector3.zero), Vector3.zero);

            foreach (Vector3 vertex in vertices) bounds.Encapsulate(matrix.MultiplyPoint3x4(vertex));

            return bounds;
        }
    }
}
