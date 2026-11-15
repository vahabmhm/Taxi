using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The highway theme: an endless straight highway along the world's Z axis ( up the portrait screen ), streamed in
    /// 20 meter stretches around the player:
    /// - two lanes each way ( right-hand traffic ), painted on one textured strip per stretch: dashed lane lines, solid
    ///   edge lines and a double yellow center line
    /// - concrete barriers with a metal rail on both sides. They're solid: cars are pushed back onto the road, and a
    ///   car that drives head-on into one is hurt ( like the city's buildings )
    /// - street lights, overhead sign gantries every 200 meters, trees and bushes on the grass beyond the barriers
    /// Spawns go onto the road ( SnapToRoad ), police come from ahead or behind ( ECCRuntimeVisuals.OutOfView ), and the
    /// traffic drives in the lanes ( ECCTraffic ). Random obstacles aren't used on the highway.
    /// Started by ECCThemes for the highway theme.
    /// The road and the barriers are one long strip ( no seams between stretches ) that jumps a whole stretch at a time,
    /// and the grass is drawn a little lower than the road so the two never flicker against each other.
    /// Runs before ECCGameController ( execution order ), so the barrier push-back happens before the camera and the
    /// ground follow the player.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class ECCHighwayWorld : MonoBehaviour
    {
        [Tooltip("Lanes in each direction")]
        public int lanesPerSide = 2;

        public float laneWidth = 4.4f;

        [Tooltip("Paved shoulder between the outer lane and the barrier")]
        public float shoulder = 1.8f;

        [Tooltip("How close a car's center may get to the barrier ( about half an enlarged car's width )")]
        public float carHalfWidth = 1.6f;

        public float segmentLength = 20;

        [Tooltip("Stretches built ahead of and behind the player")]
        public int segmentsAround = 9;

        [Tooltip("Health lost by a car that drives head-on into a barrier")]
        public float crashDamage = 1;

        [Tooltip("Seconds between two crash damages of the same car")]
        public float crashCooldown = 1.5f;

        /// <summary>
        /// How much lower than the cars' ground the grass is drawn, so the road clearly covers it
        /// </summary>
        const float GrassDrop = 0.25f;

        static ECCHighwayWorld active;

        Transform roadStrip;

        ECCGameController gameController;
        readonly Dictionary<int, GameObject> segments = new Dictionary<int, GameObject>();
        readonly List<int> removeList = new List<int>();
        readonly Dictionary<ECCCar, float> lastCrash = new Dictionary<ECCCar, float>();

        int centerSegment = int.MinValue;

        Material asphalt, concrete, metal, pole, lamp, sign, signStripe, trunk, leaves;

        /// <summary>
        /// True while a highway is in use ( other scripts move their spawns onto the road )
        /// </summary>
        public static bool IsActive
        {
            get { return active != null && active.enabled; }
        }

        public static ECCHighwayWorld Active
        {
            get { return IsActive ? active : null; }
        }

        /// <summary>
        /// Half the width of the paved road, from the center line to the barrier
        /// </summary>
        public float RoadHalfWidth
        {
            get { return lanesPerSide * laneWidth + shoulder; }
        }

        float DriveHalfWidth
        {
            get { return RoadHalfWidth - carHalfWidth; }
        }

        /// <summary>
        /// Starts the highway for this game. Safe to call more than once
        /// </summary>
        public static ECCHighwayWorld Enable(ECCGameController gameController)
        {
            if (active) return active;

            active = new GameObject("HighwayWorld").AddComponent<ECCHighwayWorld>();
            active.gameController = gameController;

            return active;
        }

        /// <summary>
        /// Moves a point onto the highway ( inside the barriers, a little away from them ). Unchanged without a highway
        /// </summary>
        public static Vector3 SnapToRoad(Vector3 position)
        {
            if (IsActive == false) return position;

            float limit = active.DriveHalfWidth - 1;

            position.x = Mathf.Clamp(position.x, -limit, limit);

            return position;
        }

        /// <summary>
        /// The center of a lane: lane 0 is next to the center line. Right-hand traffic: the lanes going to +Z are on the
        /// +X side, the ones going to -Z on the -X side
        /// </summary>
        public float LaneCenter(int lane, bool towardsPlusZ)
        {
            float x = (Mathf.Clamp(lane, 0, lanesPerSide - 1) + 0.5f) * laneWidth;

            return towardsPlusZ ? x : -x;
        }

        void Start()
        {
            CreateMaterials();

            BuildRoadStrip();

            LowerGround();

            ECCSpawnAroundObject.ObjectSpawned += OnObjectSpawned;

            MoveCameraBack();
        }

        void OnDestroy()
        {
            ECCSpawnAroundObject.ObjectSpawned -= OnObjectSpawned;

            if (active == this) active = null;
        }

        void MoveCameraBack()
        {
            // Every chapter uses the same camera distance as chapter 1 ( the city )
            ECCCameraFraming.Apply();
        }

        // ----- Streaming -----

        void Update()
        {
            ECCCar player = gameController ? gameController.playerObject : null;

            if (player == null) return;

            int segment = Mathf.RoundToInt(player.transform.position.z / segmentLength);

            if (segment == centerSegment) return;

            centerSegment = segment;

            // The road texture repeats every stretch, so a jump of whole stretches looks seamless
            if (roadStrip) roadStrip.position = new Vector3(0, 0, centerSegment * segmentLength);

            removeList.Clear();

            foreach (KeyValuePair<int, GameObject> pair in segments)
            {
                if (Mathf.Abs(pair.Key - centerSegment) > segmentsAround + 1) removeList.Add(pair.Key);
            }

            foreach (int key in removeList)
            {
                Destroy(segments[key]);
                segments.Remove(key);
            }

            for (int index = centerSegment - segmentsAround; index <= centerSegment + segmentsAround; index++)
            {
                if (segments.ContainsKey(index) == false) segments[index] = BuildSegment(index);
            }
        }

        void LateUpdate()
        {
            // The barriers are solid: push the cars back onto the road
            float limit = DriveHalfWidth;

            for (int index = ECCCar.ActiveCars.Count - 1; index >= 0; index--)
            {
                ECCCar car = ECCCar.ActiveCars[index];

                if (car == null || car.health <= 0) continue;

                Vector3 position = car.transform.position;

                if (Mathf.Abs(position.x) <= limit) continue;

                // The normal points from the barrier back onto the road
                Vector3 normal = new Vector3(-Mathf.Sign(position.x), 0, 0);

                position.x = Mathf.Sign(position.x) * limit;
                car.transform.position = position;

                // A head-on crash ( not sliding along the barrier ) hurts
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

        // ----- Building -----

        /// <summary>
        /// The road ( markings painted on ) and both barriers as one strip as long as all the stretches around the
        /// player. Separate pieces per stretch left thin seams across the road
        /// </summary>
        void BuildRoadStrip()
        {
            roadStrip = new GameObject("HighwayRoad").transform;
            roadStrip.SetParent(transform, false);

            int count = segmentsAround * 2 + 1;
            float half = RoadHalfWidth;
            float length = count * segmentLength;

            // One copy of the road texture per stretch
            asphalt.mainTextureScale = new Vector2(1, count);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, roadStrip, new Vector3(0, 0, 0), new Vector3(half * 2, 0.08f, length), asphalt);

            // Concrete barriers with a metal rail, reaching down to the lowered grass
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (half + 0.4f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, roadStrip, new Vector3(x, (1.1f - GrassDrop) * 0.5f, 0), new Vector3(0.8f, 1.1f + GrassDrop, length), concrete);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, roadStrip, new Vector3(x - side * 0.1f, 1.3f, 0), new Vector3(0.22f, 0.32f, length), metal);
            }
        }

        /// <summary>
        /// The scene's ground plane sits at the cars' height, only 4 cm under the road, and flickered through it.
        /// It keeps its collider and its place ( the cars and spawns still use it ), but is drawn by a copy a little
        /// lower, with the same material ( its texture still scrolls with the player )
        /// </summary>
        void LowerGround()
        {
            Transform ground = gameController ? gameController.groundObject : null;

            if (ground == null) return;

            Renderer groundRenderer = ground.GetComponent<Renderer>();
            MeshFilter groundMesh = ground.GetComponent<MeshFilter>();

            if (groundRenderer == null || groundMesh == null || groundMesh.sharedMesh == null) return;

            GameObject copy = new GameObject("HighwayGrass");
            copy.transform.SetParent(ground, false);
            copy.transform.position = ground.position + Vector3.down * GrassDrop;

            copy.AddComponent<MeshFilter>().sharedMesh = groundMesh.sharedMesh;

            MeshRenderer copyRenderer = copy.AddComponent<MeshRenderer>();
            copyRenderer.sharedMaterial = groundRenderer.material;
            copyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            copyRenderer.receiveShadows = groundRenderer.receiveShadows;

            groundRenderer.enabled = false;
        }

        GameObject BuildSegment(int index)
        {
            GameObject segment = new GameObject("Highway_" + index);
            segment.transform.position = new Vector3(0, 0, index * segmentLength);
            segment.transform.SetParent(transform, true);

            Transform root = segment.transform;

            float half = RoadHalfWidth;
            float length = segmentLength;

            int roll = Hash(index, 11);

            // A street light every other stretch, on alternating sides, its arm over the road
            if (index % 2 == 0)
            {
                float side = Hash(index, 5) % 2 == 0 ? 1 : -1;
                float x = side * (half + 1.3f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, root, new Vector3(x, (9 - GrassDrop) * 0.5f, 0), new Vector3(0.35f, (9 + GrassDrop) * 0.5f, 0.35f), pole);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(x - side * 2.2f, 8.9f, 0), new Vector3(4.4f, 0.25f, 0.3f), pole);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(x - side * 4.1f, 8.7f, 0), new Vector3(1.2f, 0.25f, 0.6f), lamp);
            }

            // An overhead sign gantry every ten stretches
            if (index % 10 == 0) BuildGantry(root, half);

            // Trees and bushes on the grass beyond the barriers
            int count = 1 + roll % 3;

            for (int item = 0; item < count; item++)
            {
                int seed = Hash(index, 100 + item);

                float side = seed % 2 == 0 ? 1 : -1;
                float x = side * (half + 4 + (seed / 2) % 14);
                float z = ((seed / 32) % 100) / 100f * length - length * 0.5f;

                if (seed % 5 == 0) CreateBush(root, new Vector3(x, -GrassDrop, z), 1 + (seed % 7) * 0.15f);
                else CreateTree(root, new Vector3(x, -GrassDrop, z), 1 + (seed % 9) * 0.12f);
            }

            return segment;
        }

        void BuildGantry(Transform root, float half)
        {
            float span = half + 1.4f;

            for (int side = -1; side <= 1; side += 2)
            {
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(side * span, (9.2f - GrassDrop) * 0.5f, 0), new Vector3(0.5f, 9.2f + GrassDrop, 0.5f), pole);
            }

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, 9.3f, 0), new Vector3(span * 2 + 0.5f, 0.6f, 0.6f), pole);

            // One green sign over each direction, with a white border stripe
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 center = new Vector3(side * half * 0.5f, 10.8f, -0.35f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center, new Vector3(half * 0.8f, 2.4f, 0.15f), sign);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(0, 0.75f, -0.08f), new Vector3(half * 0.6f, 0.18f, 0.02f), signStripe);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(0, -0.2f, -0.08f), new Vector3(half * 0.4f, 0.18f, 0.02f), signStripe);
            }
        }

        void CreateTree(Transform root, Vector3 position, float size)
        {
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, root, position + new Vector3(0, 1.2f * size, 0), new Vector3(0.5f, 1.2f, 0.5f) * size, trunk);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, root, position + new Vector3(0, 3.4f * size, 0), new Vector3(3, 3.4f, 3) * size, leaves);
        }

        void CreateBush(Transform root, Vector3 position, float size)
        {
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, root, position + new Vector3(0, 0.6f * size, 0), new Vector3(2.4f, 1.4f, 2.2f) * size, leaves);
        }

        static int Hash(int a, int b)
        {
            unchecked
            {
                int hash = a * 73856093 ^ b * 19349663;
                hash ^= hash >> 13;
                hash *= 1274126177;

                return hash & 0x7fffffff;
            }
        }

        // ----- Materials -----

        void CreateMaterials()
        {
            asphalt = ECCRuntimeVisuals.CreateMaterial(Color.white, 0);
            asphalt.mainTexture = CreateRoadTexture();
            if (asphalt.HasProperty("_Glossiness")) asphalt.SetFloat("_Glossiness", 0.15f);

            concrete = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xC9C6BF), 0);
            metal = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xB8C2CC), 0);
            if (metal.HasProperty("_Metallic")) metal.SetFloat("_Metallic", 0.6f);

            pole = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x5B6470), 0);
            lamp = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xFFF2C4), 0.8f);
            sign = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x1E7A46), 0.15f);
            signStripe = ECCRuntimeVisuals.CreateMaterial(Color.white, 0.4f);
            trunk = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x7A5230), 0);
            leaves = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x4CAF50), 0.05f);
        }

        /// <summary>
        /// One stretch of road seen from above: X across the road ( barrier to barrier ), Y along it. Repeats exactly
        /// from stretch to stretch
        /// </summary>
        Texture2D CreateRoadTexture()
        {
            const int width = 512, height = 256;

            float half = RoadHalfWidth;
            float pixelsPerMeterX = width / (half * 2);
            float pixelsPerMeterY = height / segmentLength;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
            // Clamped across the road, repeated along it ( one copy per stretch on the long road strip )
            texture.wrapModeU = TextureWrapMode.Clamp;
            texture.wrapModeV = TextureWrapMode.Repeat;
            texture.anisoLevel = 8;

            Color road = ECCCarTheme.Hex(0x3B3E45);
            Color shoulderColor = ECCCarTheme.Hex(0x4A4D54);
            Color white = ECCCarTheme.Hex(0xF2F2EE);
            Color yellow = ECCCarTheme.Hex(0xF4C430);

            Color[] pixels = new Color[width * height];

            System.Random random = new System.Random(5);

            float lanesEdge = lanesPerSide * laneWidth;

            for (int y = 0; y < height; y++)
            {
                float along = y / pixelsPerMeterY;

                for (int x = 0; x < width; x++)
                {
                    float across = x / pixelsPerMeterX - half;
                    float distance = Mathf.Abs(across);

                    // Asphalt with a little grain
                    Color color = distance > lanesEdge ? shoulderColor : road;
                    float grain = (float)random.NextDouble() * 0.05f - 0.025f;
                    color = new Color(color.r + grain, color.g + grain, color.b + grain, 1);

                    // Double yellow center line
                    if (Mathf.Abs(distance - 0.25f) < 0.09f) color = yellow;

                    // Solid white edge lines
                    else if (Mathf.Abs(distance - lanesEdge) < 0.13f) color = white;

                    // Dashed lane lines: 4 meters of line every 10 meters
                    else
                    {
                        for (int lane = 1; lane < lanesPerSide; lane++)
                        {
                            if (Mathf.Abs(distance - lane * laneWidth) < 0.1f && Mathf.Repeat(along, 10) < 4) color = white;
                        }
                    }

                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);

            return texture;
        }

        // ----- Spawns -----

        /// <summary>
        /// No random obstacles on the highway; money and pickups go onto the road
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
