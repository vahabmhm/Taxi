using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// An endless city instead of the open ground ( Kenney City Kit Roads / Commercial / Suburban, CC0 ).
    /// The world is a grid of square cells: every ( blockCells + 1 )th row and column is a street, the cells between them
    /// are city blocks. Only the cells around the player exist; they're built from pooled models as the player drives.
    /// - streets: straight road tiles and crossroads, street lights and traffic lights
    /// - blocks: «downtown» ( shops and towers ) or «suburb» ( houses and trees ), chosen per block
    /// - the blocks are solid: cars ( the player and the police ) are pushed back onto the street, and a head-on crash
    ///   costs health. Random obstacles aren't spawned; money, pickups and hazards are moved onto the streets
    /// - buildings between the camera and the player fade see-through ( keeping their size ), so the car is never hidden
    /// Used by the endless mode and the city chapter of the levels ( ECCEndlessMode, ECCThemes ).
    /// </summary>
    public class ECCCityWorld : MonoBehaviour
    {
        [Tooltip("Size of one grid cell. A street is one cell wide")]
        public float cellSize = 14;

        [Tooltip("Cells per city block side ( between two streets )")]
        public int blockCells = 2;

        [Tooltip("Cells built around the player in each direction")]
        public int viewCells = 6;

        [Tooltip("How far from a street's center line a car's center may go ( half the street plus the sidewalk, minus the car )")]
        public float streetHalfWidth = 5.8f;

        [Tooltip("Health lost by a car that drives head-on into a building")]
        public float crashDamage = 1;

        [Tooltip("Seconds between two crash damages of the same car")]
        public float crashCooldown = 1.5f;

        [Tooltip("Scale of street lights, traffic lights and other props")]
        public float propScale = 10;

        [Tooltip("The road tile model runs along X ( its sidewalks are on its Z sides ). Turn off if the streets look crossed")]
        public bool roadModelRunsAlongX = true;

        const string Root = "KenneyCity/";

        static readonly string[] Downtown =
        {
            "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g",
            "building-h", "building-i", "building-j", "building-k", "building-l", "building-m", "building-n",
            "low-detail-building-wide-a", "low-detail-building-wide-b",
        };

        static readonly string[] Towers = { "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e" };

        static readonly string[] Houses =
        {
            "building-type-a", "building-type-b", "building-type-c", "building-type-d", "building-type-e", "building-type-f",
            "building-type-g", "building-type-h", "building-type-i", "building-type-j", "building-type-k", "building-type-l",
            "building-type-m", "building-type-n", "building-type-o", "building-type-p", "building-type-q", "building-type-r",
            "building-type-s", "building-type-t", "building-type-u",
        };

        class Cell
        {
            public readonly List<GameObject> objects = new List<GameObject>();
        }

        class Building
        {
            public Transform transform;
            public float scale;
            public float halfWidth;
            public float height;
            public string kit;
            public Renderer[] renderers;
            public float alpha = 1;
            public bool faded;
        }

        [Tooltip("How see-through a building between the camera and the player becomes ( 0 invisible, 1 solid )")]
        [Range(0, 1)]
        public float fadedAlpha = 0.3f;

        const string FadeMaterialPath = "KenneyCity/KenneyFade";

        readonly Dictionary<string, Material> fadeMaterials = new Dictionary<string, Material>();
        MaterialPropertyBlock fadeBlock;

        static ECCCityWorld active;

        /// <summary>
        /// The color the buildings ( and, lighter, the streets ) are multiplied by. White is the normal city; the ruins
        /// theme makes it burnt and dusty. Set before the city builds; reset by the game controller every game
        /// </summary>
        public static Color buildingTint = Color.white;

        ECCGameController gameController;
        int seed;
        Vector2Int centerCell = new Vector2Int(int.MinValue, int.MinValue);

        readonly Dictionary<Vector2Int, Cell> cells = new Dictionary<Vector2Int, Cell>();
        readonly Dictionary<GameObject, Building> buildings = new Dictionary<GameObject, Building>();
        readonly Dictionary<string, Stack<GameObject>> pool = new Dictionary<string, Stack<GameObject>>();
        readonly Dictionary<string, GameObject> models = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Bounds> modelBounds = new Dictionary<string, Bounds>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly Dictionary<ECCCar, float> lastCrash = new Dictionary<ECCCar, float>();
        readonly List<Vector2Int> removeList = new List<Vector2Int>();

        Transform poolRoot;

        /// <summary>
        /// True while a city is in use ( other scripts move their spawns onto the streets )
        /// </summary>
        public static bool IsActive
        {
            get { return active != null && active.enabled; }
        }

        /// <summary>
        /// Starts the city for this game. Safe to call more than once
        /// </summary>
        public static ECCCityWorld Enable(ECCGameController gameController)
        {
            if (active) return active;

            // The city needs the Kenney models in Resources
            if (Resources.Load<GameObject>(Root + "Roads/road-straight") == null)
            {
                Debug.LogWarning("ECCCityWorld: the Kenney city models are missing from Resources/" + Root + ", the open ground is used");
                return null;
            }

            active = new GameObject("CityWorld").AddComponent<ECCCityWorld>();
            active.gameController = gameController;

            // The old ground follows the car and would cover the street tiles: only its drawing is turned off
            // ( its collider stays, for the ground raycasts )
            HideGround(gameController);

            return active;
        }

        static void HideGround(ECCGameController gameController)
        {
            if (gameController == null || gameController.groundObject == null) return;

            foreach (Renderer renderer in gameController.groundObject.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }

        void Start()
        {
            seed = Random.Range(1, 100000);

            poolRoot = new GameObject("Pool").transform;
            poolRoot.SetParent(transform, false);
            poolRoot.gameObject.SetActive(false);

            ECCSpawnAroundObject.ObjectSpawned += OnObjectSpawned;

            MoveCameraBack();
        }

        /// <summary>
        /// The gameplay camera moves farther away along its own angle, so more of the city is visible.
        /// The scene reloads between runs, so it's only done once per run
        /// </summary>
        void MoveCameraBack()
        {
            // Every chapter uses the same camera distance as chapter 1 ( the city )
            ECCCameraFraming.Apply();
        }

        void OnDestroy()
        {
            ECCSpawnAroundObject.ObjectSpawned -= OnObjectSpawned;

            if (active == this) active = null;
        }

        // ----- The grid -----

        int Period
        {
            get { return blockCells + 1; }
        }

        float StreetSpacing
        {
            get { return Period * cellSize; }
        }

        static int Mod(int value, int period)
        {
            int result = value % period;

            return result < 0 ? result + period : result;
        }

        static int FloorDiv(int value, int period)
        {
            return (value - Mod(value, period)) / period;
        }

        int Hash(int x, int y, int salt)
        {
            unchecked
            {
                int hash = x * 73856093 ^ y * 19349663 ^ salt * 83492791 ^ seed * 2654435;
                hash ^= hash >> 13;
                hash *= 0x5bd1e995;
                hash ^= hash >> 15;

                return hash & 0x7fffffff;
            }
        }

        Vector3 CellCenter(Vector2Int cell)
        {
            return new Vector3(cell.x * cellSize, 0, cell.y * cellSize);
        }

        /// <summary>
        /// Moves a point that is inside a block onto the nearest street ( points on a street don't move )
        /// </summary>
        /// <summary>
        /// The distance between two parallel streets ( their center lines are at multiples of it ), when the city is used
        /// </summary>
        public static bool GetStreetSpacing(out float spacing)
        {
            spacing = IsActive ? active.StreetSpacing : 0;

            return IsActive;
        }

        public static Vector3 SnapToRoad(Vector3 position)
        {
            if (IsActive == false) return position;

            Vector3 normal;

            active.PushOut(ref position, active.streetHalfWidth - 1, out normal);

            return position;
        }

        /// <summary>
        /// Pushes a point out of a block onto the street, up to "free" from the street's center line.
        /// Returns true if it was inside a block; normal points from the block onto the street
        /// </summary>
        bool PushOut(ref Vector3 position, float free, out Vector3 normal)
        {
            normal = Vector3.zero;

            float spacing = StreetSpacing;

            float localX = position.x - Mathf.Round(position.x / spacing) * spacing;
            float localZ = position.z - Mathf.Round(position.z / spacing) * spacing;

            float insideX = Mathf.Abs(localX) - free;
            float insideZ = Mathf.Abs(localZ) - free;

            // On a street when close enough to one of the two nearest center lines
            if (insideX <= 0 || insideZ <= 0) return false;

            if (insideX < insideZ)
            {
                position.x -= Mathf.Sign(localX) * insideX;
                normal = new Vector3(-Mathf.Sign(localX), 0, 0);
            }
            else
            {
                position.z -= Mathf.Sign(localZ) * insideZ;
                normal = new Vector3(0, 0, -Mathf.Sign(localZ));
            }

            return true;
        }

        // ----- Streaming -----

        void Update()
        {
            ECCCar player = gameController ? gameController.playerObject : null;

            if (player == null) return;

            Vector3 position = player.transform.position;

            Vector2Int cell = new Vector2Int(Mathf.RoundToInt(position.x / cellSize), Mathf.RoundToInt(position.z / cellSize));

            if (cell != centerCell)
            {
                centerCell = cell;
                Stream();
            }

            FadeBuildings(position);
        }

        void LateUpdate()
        {
            // The blocks are solid: push the cars back onto the streets
            for (int index = ECCCar.ActiveCars.Count - 1; index >= 0; index--)
            {
                ECCCar car = ECCCar.ActiveCars[index];

                // ( every car is enlarged by ECCCar itself, see ECCGameController.carSize )
                if (car == null || car.health <= 0) continue;

                Vector3 position = car.transform.position;
                Vector3 normal;

                if (PushOut(ref position, streetHalfWidth, out normal) == false) continue;

                car.transform.position = position;

                // A head-on crash ( not sliding along the wall ) hurts
                Vector3 forward = car.transform.forward;
                forward.y = 0;

                if (crashDamage > 0 && Vector3.Dot(forward.normalized, normal) < -0.7f)
                {
                    float last;

                    if (lastCrash.TryGetValue(car, out last) == false || Time.time - last > crashCooldown)
                    {
                        lastCrash[car] = Time.time;

                        if (car.hitEffect && car.health - crashDamage > 0) Instantiate(car.hitEffect, position, car.transform.rotation);

                        car.ChangeHealth(-crashDamage);
                    }
                }
            }
        }

        void Stream()
        {
            // Remove the far cells
            removeList.Clear();

            foreach (KeyValuePair<Vector2Int, Cell> pair in cells)
            {
                if (Mathf.Abs(pair.Key.x - centerCell.x) > viewCells + 1 || Mathf.Abs(pair.Key.y - centerCell.y) > viewCells + 1) removeList.Add(pair.Key);
            }

            foreach (Vector2Int key in removeList)
            {
                foreach (GameObject item in cells[key].objects) Release(item);

                cells.Remove(key);
            }

            // Add the near ones
            for (int x = centerCell.x - viewCells; x <= centerCell.x + viewCells; x++)
            {
                for (int y = centerCell.y - viewCells; y <= centerCell.y + viewCells; y++)
                {
                    Vector2Int key = new Vector2Int(x, y);

                    if (cells.ContainsKey(key) == false) cells[key] = BuildCell(key);
                }
            }
        }

        Cell BuildCell(Vector2Int key)
        {
            Cell cell = new Cell();

            bool streetX = Mod(key.x, Period) == 0;   // a street running along Z ( constant x )
            bool streetZ = Mod(key.y, Period) == 0;   // a street running along X ( constant z )

            Vector3 center = CellCenter(key);

            if (streetX && streetZ) BuildCrossroad(cell, key, center);
            else if (streetX || streetZ) BuildStreet(cell, key, center, streetX);
            else BuildBlockCell(cell, key, center);

            return cell;
        }

        void BuildCrossroad(Cell cell, Vector2Int key, Vector3 center)
        {
            cell.objects.Add(PlaceTile("Roads/road-crossroad", center, 0));

            // A traffic light on one corner
            int corner = Hash(key.x, key.y, 7) % 4;

            Vector3 offset = Quaternion.Euler(0, corner * 90, 0) * new Vector3(1, 0, 1) * (cellSize * 0.5f - 0.8f);

            cell.objects.Add(PlaceProp("Roads/traffic-light", center + offset, corner * 90 + 225));
        }

        void BuildStreet(Cell cell, Vector2Int key, Vector3 center, bool alongZ)
        {
            // The tile runs along X; a street along Z turns it a quarter
            bool turn = alongZ == roadModelRunsAlongX;

            cell.objects.Add(PlaceTile("Roads/road-straight", center, turn ? 90 : 0));

            int roll = Hash(key.x, key.y, 3);

            // Street lights on every other cell, on alternating sides, with the arm over the street
            if (roll % 2 == 0)
            {
                float side = (roll / 2) % 2 == 0 ? 1 : -1;
                float edge = cellSize * 0.5f - 0.8f;

                Vector3 position = alongZ ? center + new Vector3(side * edge, 0, 0) : center + new Vector3(0, 0, side * edge);

                // The model's arm points to -Z
                float angle = alongZ ? (side > 0 ? 90 : -90) : (side > 0 ? 0 : 180);

                cell.objects.Add(PlaceProp("Roads/light-square", position, angle));
            }
            else if (roll % 7 == 1)
            {
                // Now and then a dumpster on the sidewalk
                float side = (roll / 7) % 2 == 0 ? 1 : -1;
                float edge = cellSize * 0.5f - 0.7f;

                Vector3 position = alongZ ? center + new Vector3(side * edge, 0, 0) : center + new Vector3(0, 0, side * edge);

                cell.objects.Add(PlaceProp("Roads/dumpster", position, alongZ ? 90 : 0));
            }
        }

        void BuildBlockCell(Cell cell, Vector2Int key, Vector3 center)
        {
            cell.objects.Add(PlaceTile("Roads/tile-low", center, 0));

            // The whole block is either downtown or a suburb
            int blockX = FloorDiv(key.x, Period);
            int blockY = FloorDiv(key.y, Period);

            bool downtown = Hash(blockX, blockY, 11) % 100 < 55;

            int roll = Hash(key.x, key.y, 5);

            string model;

            if (downtown) model = roll % 100 < 15 ? "Commercial/" + Towers[roll % Towers.Length] : "Commercial/" + Downtown[roll % Downtown.Length];
            else model = "Suburban/" + Houses[roll % Houses.Length];

            // Face one of the streets next to this cell
            int localX = Mod(key.x, Period);
            int localY = Mod(key.y, Period);

            float faceX = localX == 1 ? 270 : 90;   // the street on -X or +X
            float faceZ = localY == 1 ? 180 : 0;    // the street on -Z or +Z

            float angle = (roll / 100) % 2 == 0 ? faceX : faceZ;

            GameObject building = PlaceBuilding(model, center, angle);
            cell.objects.Add(building);

            // Suburbs get trees on the corners of their lots
            if (downtown == false)
            {
                float edge = cellSize * 0.5f - 1.2f;

                for (int corner = 0; corner < 2; corner++)
                {
                    if (Hash(key.x, key.y, 20 + corner) % 3 == 0) continue;

                    Vector3 offset = new Vector3(corner == 0 ? edge : -edge, 0, (roll % 2 == 0 ? edge : -edge));

                    cell.objects.Add(PlaceProp(Hash(key.x, key.y, 30 + corner) % 2 == 0 ? "Suburban/tree-large" : "Suburban/tree-small", center + offset, roll % 360));
                }
            }
        }

        // ----- Placing models -----

        /// <summary>
        /// A ground tile, scaled to one cell, its top just above the old ground
        /// </summary>
        GameObject PlaceTile(string model, Vector3 center, float angle)
        {
            GameObject tile = Take(model);

            if (tile == null) return null;

            Bounds bounds = modelBounds[model];

            tile.transform.localScale = Vector3.one * cellSize;
            tile.transform.rotation = Quaternion.Euler(0, angle, 0);
            tile.transform.position = new Vector3(center.x, 0.01f - bounds.max.y * cellSize, center.z);

            return tile;
        }

        GameObject PlaceProp(string model, Vector3 position, float angle)
        {
            GameObject prop = Take(model);

            if (prop == null) return null;

            Bounds bounds = modelBounds[model];

            prop.transform.localScale = Vector3.one * propScale;
            prop.transform.rotation = Quaternion.Euler(0, angle, 0);
            prop.transform.position = new Vector3(position.x, TileTop - bounds.min.y * propScale, position.z);

            return prop;
        }

        /// <summary>
        /// A building, as big as fits the cell ( leaving a sidewalk ), standing on the block's tile
        /// </summary>
        GameObject PlaceBuilding(string model, Vector3 center, float angle)
        {
            GameObject building = Take(model);

            if (building == null) return null;

            Bounds bounds = modelBounds[model];

            float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
            float scale = (cellSize - 2.5f) / Mathf.Max(0.01f, footprint);

            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            Vector3 offset = rotation * new Vector3(bounds.center.x, 0, bounds.center.z) * scale;

            building.transform.localScale = Vector3.one * scale;
            building.transform.rotation = rotation;
            building.transform.position = new Vector3(center.x - offset.x, TileTop - bounds.min.y * scale, center.z - offset.z);

            Building info;

            if (buildings.TryGetValue(building, out info) == false)
            {
                info = new Building { transform = building.transform };
                buildings[building] = info;
            }

            info.scale = scale;
            info.halfWidth = footprint * scale * 0.5f;
            info.height = bounds.size.y * scale;
            info.kit = model.Substring(0, model.IndexOf('/'));

            if (info.renderers == null) info.renderers = building.GetComponentsInChildren<Renderer>(true);

            // A pooled building may come back faded
            SetFade(info, 1);

            return building;
        }

        float TileTop
        {
            get { return 0.01f; }
        }

        GameObject Take(string model)
        {
            Stack<GameObject> stack;

            if (pool.TryGetValue(model, out stack) && stack.Count > 0)
            {
                GameObject pooled = stack.Pop();
                pooled.transform.SetParent(transform, false);

                return pooled;
            }

            GameObject source = LoadModel(model);

            if (source == null) return null;

            GameObject instance = Instantiate(source, transform);
            instance.name = model;

            return instance;
        }

        void Release(GameObject item)
        {
            if (item == null) return;

            Stack<GameObject> stack;

            if (pool.TryGetValue(item.name, out stack) == false)
            {
                stack = new Stack<GameObject>();
                pool[item.name] = stack;
            }

            item.transform.SetParent(poolRoot, false);
            stack.Push(item);
        }

        /// <summary>
        /// Loads a model once: gives it its kit's material, and measures it at scale 1
        /// </summary>
        GameObject LoadModel(string model)
        {
            GameObject source;

            if (models.TryGetValue(model, out source)) return source;

            source = Resources.Load<GameObject>(Root + model);

            if (source == null)
            {
                Debug.LogWarning("ECCCityWorld: missing model " + Root + model);
                models[model] = null;
                return null;
            }

            // A prepared copy ( kit material, no colliders ) kept inactive in the pool, used as the template.
            // The model keeps its own import rotation and scale inside a plain holder, which is what gets placed
            GameObject template = new GameObject(model);
            template.transform.SetParent(poolRoot, false);

            Instantiate(source, template.transform, false);

            Material material = GetMaterial(model.Substring(0, model.IndexOf('/')));

            Bounds bounds = new Bounds();
            bool first = true;

            foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
            {
                Material[] shared = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];

                for (int index = 0; index < shared.Length; index++) shared[index] = material;

                renderer.sharedMaterials = shared;

                // Bounds relative to the template's root ( the pool root is at the origin of the city object )
                Bounds local = TransformBounds(renderer, template.transform);

                if (first) bounds = local;
                else bounds.Encapsulate(local);

                first = false;
            }

            foreach (Collider collider in template.GetComponentsInChildren<Collider>(true)) Destroy(collider);

            models[model] = template;
            modelBounds[model] = bounds;

            return template;
        }

        static Bounds TransformBounds(Renderer renderer, Transform root)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();

            Bounds meshBounds = filter && filter.sharedMesh ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.zero);

            Matrix4x4 matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

            Vector3 min = meshBounds.min;
            Vector3 max = meshBounds.max;

            Bounds result = new Bounds(matrix.MultiplyPoint3x4(min), Vector3.zero);

            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = new Vector3((corner & 1) == 0 ? min.x : max.x, (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z);

                result.Encapsulate(matrix.MultiplyPoint3x4(point));
            }

            return result;
        }

        Material GetMaterial(string kit)
        {
            Material material;

            if (materials.TryGetValue(kit, out material)) return material;

            // The built-in pipeline's Standard shader, like the game's own materials
            material = new Material(Shader.Find("Standard"));
            material.mainTexture = Resources.Load<Texture2D>(Root + kit + "/Textures/colormap");
            material.SetFloat("_Glossiness", 0.15f);

            // Streets get half the tint of the buildings
            material.color = kit == "Roads" ? Color.Lerp(Color.white, buildingTint, 0.5f) : buildingTint;
            material.enableInstancing = true;

            materials[kit] = material;

            return material;
        }

        // ----- Camera view -----

        /// <summary>
        /// Buildings that stand between the camera and the player become see-through ( they keep their size ), so the
        /// player's car stays visible. Only the ones tall enough to cut the camera's line of sight fade
        /// </summary>
        void FadeBuildings(Vector3 player)
        {
            Camera view = Camera.main;

            if (view == null) return;

            Vector3 toCamera = view.transform.position - player;
            float cameraHeight = toCamera.y;
            toCamera.y = 0;

            float cameraDistance = toCamera.magnitude;

            if (cameraDistance < 0.01f) toCamera = Vector3.back;

            toCamera.Normalize();

            // How fast the line of sight rises when going from the car towards the camera
            float rise = cameraDistance > 0.01f ? cameraHeight / cameraDistance : 10;

            Vector3 side = Vector3.Cross(Vector3.up, toCamera);

            foreach (Building building in buildings.Values)
            {
                if (building.transform.parent != transform) continue;

                Vector3 offset = building.transform.position - player;
                offset.y = 0;

                float along = Vector3.Dot(offset, toCamera);
                float across = Mathf.Abs(Vector3.Dot(offset, side));

                // On the camera's side of the car, beside the line of sight, and taller than it at its near edge
                float nearEdge = Mathf.Max(0, along - building.halfWidth);

                bool hiding = along > -building.halfWidth && across < building.halfWidth + 4 && building.height > nearEdge * rise;

                float target = hiding ? fadedAlpha : 1;

                if (Mathf.Approximately(building.alpha, target)) continue;

                SetFade(building, Mathf.MoveTowards(building.alpha, target, Time.deltaTime * 2.5f));
            }
        }

        /// <summary>
        /// Shows a building with an opacity: solid with the kit's normal material, see-through with its fade material
        /// </summary>
        void SetFade(Building building, float alpha)
        {
            building.alpha = alpha;

            if (building.renderers == null) return;

            bool fade = alpha < 0.999f;

            if (fade != building.faded)
            {
                building.faded = fade;

                Material material = fade ? GetFadeMaterial(building.kit) : GetMaterial(building.kit);

                foreach (Renderer renderer in building.renderers)
                {
                    if (renderer == null) continue;

                    Material[] shared = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];

                    for (int index = 0; index < shared.Length; index++) shared[index] = material;

                    renderer.sharedMaterials = shared;

                    if (fade == false) renderer.SetPropertyBlock(null);
                }
            }

            if (fade == false) return;

            if (fadeBlock == null) fadeBlock = new MaterialPropertyBlock();

            fadeBlock.SetColor("_Color", new Color(buildingTint.r, buildingTint.g, buildingTint.b, alpha));

            foreach (Renderer renderer in building.renderers)
            {
                if (renderer) renderer.SetPropertyBlock(fadeBlock);
            }
        }

        /// <summary>
        /// The see-through version of a kit's material ( from the Fade material asset in Resources, so its shader variant
        /// is in the build )
        /// </summary>
        Material GetFadeMaterial(string kit)
        {
            Material material;

            if (fadeMaterials.TryGetValue(kit, out material)) return material;

            Material template = Resources.Load<Material>(FadeMaterialPath);

            material = template ? new Material(template) : new Material(GetMaterial(kit));
            material.mainTexture = GetMaterial(kit).mainTexture;
            material.enableInstancing = true;

            fadeMaterials[kit] = material;

            return material;
        }

        // ----- Spawns -----

        /// <summary>
        /// No random obstacles in the city ( its buildings are the obstacles ); money and pickups go onto the streets.
        /// Police cars are pushed onto the streets every frame anyway
        /// </summary>
        void OnObjectSpawned(Transform spawned)
        {
            if (spawned == null || spawned.GetComponent<ECCCar>()) return;

            if (spawned.GetComponent<ECCObstacle>())
            {
                Destroy(spawned.gameObject);
                return;
            }

            spawned.position = SnapToRoad(spawned.position);
        }
    }

}
