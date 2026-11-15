using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// Gives each chapter its own look using the assets already in the scene:
    /// city = buildings, desert = rocks on dirt ground, forest = dense trees, plus ground, light and fog colors.
    /// rain = the city streets at night in heavy rain ( ECCRainWeather ), where every car slides ( ECCCar.grip ).
    /// highway = an endless highway with barriers and fast traffic ( ECCHighwayWorld ), on green grass.
    /// ruins = the city in ruins ( ECCRuinsWorld ): burnt buildings, wrecks on the streets, smoke and dust.
    /// </summary>
    public static class ECCThemes
    {
        /// <summary>
        /// How well the cars hold the wet road of the rain theme ( 1 is dry )
        /// </summary>
        public const float WetGrip = 0.45f;

        /// <summary>
        /// The forest spawns its trees this much more often than the other places' obstacles ( 1.7 = 70% more trees )
        /// </summary>
        public const float ForestTreeDensity = 1.7f;

        /// <summary>
        /// The desert's dirt color: the green ground texture is turned into this color, keeping its pattern
        /// </summary>
        static readonly Color DirtColor = new Color(0.8f, 0.6f, 0.38f);

        static Texture2D dirtTexture;

        struct ThemeSettings
        {
            public string[] obstacleKeywords;
            public Color groundTint;
            public Color lightColor;
            public Color fogColor;
        }

        static ThemeSettings GetSettings(ECCLevelTheme theme)
        {
            switch (theme)
            {
                case ECCLevelTheme.City:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Building", "Rock" },
                        groundTint = new Color(0.78f, 0.8f, 0.86f),
                        lightColor = new Color(0.95f, 0.97f, 1),
                        fogColor = new Color(0.7f, 0.75f, 0.85f),
                    };

                case ECCLevelTheme.Desert:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Rock" },
                        // The ground texture is already turned to dirt ( DirtGround ), this only warms it a little
                        groundTint = new Color(1, 0.96f, 0.9f),
                        lightColor = new Color(1, 0.88f, 0.7f),
                        fogColor = new Color(0.95f, 0.8f, 0.6f),
                    };

                case ECCLevelTheme.Ruins:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Building", "Rock" },
                        groundTint = new Color(0.7f, 0.6f, 0.5f),
                        lightColor = new Color(1, 0.74f, 0.5f),
                        fogColor = new Color(0.56f, 0.43f, 0.32f),
                    };

                case ECCLevelTheme.Highway:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Rock" },
                        groundTint = new Color(0.62f, 0.86f, 0.5f),
                        lightColor = new Color(1, 0.97f, 0.9f),
                        fogColor = new Color(0.75f, 0.82f, 0.9f),
                    };

                case ECCLevelTheme.Rain:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Building", "Rock" },
                        groundTint = new Color(0.45f, 0.5f, 0.6f),
                        lightColor = new Color(0.55f, 0.63f, 0.9f),
                        fogColor = new Color(0.1f, 0.12f, 0.18f),
                    };

                case ECCLevelTheme.Forest:
                    return new ThemeSettings
                    {
                        obstacleKeywords = new[] { "Tree", "Rock" },
                        groundTint = new Color(0.62f, 0.88f, 0.58f),
                        lightColor = new Color(0.92f, 1, 0.9f),
                        fogColor = new Color(0.6f, 0.75f, 0.6f),
                    };

                default:
                    return new ThemeSettings { obstacleKeywords = null };
            }
        }

        public static void Apply(ECCLevelTheme theme, ECCGameController gameController, ECCSpawnTuning spawnTuning)
        {
            // Every chapter is seen from the same distance as chapter 1 ( the city ), also the open-ground ones
            ECCCameraFraming.Apply();

            ThemeSettings settings = GetSettings(theme);

            // The default theme keeps the scene as it is
            if (settings.obstacleKeywords == null) return;

            // The city chapter drives through the endless city ( streets and blocks instead of the open ground )
            // The ruins are the same city, burnt and dusty, with wrecks, rubble, fires and smoke on top
            if (theme == ECCLevelTheme.Ruins) ECCCityWorld.buildingTint = ECCRuinsWorld.BuildingTint;

            if (theme == ECCLevelTheme.City || theme == ECCLevelTheme.Rain || theme == ECCLevelTheme.Ruins) ECCCityWorld.Enable(gameController);

            if (theme == ECCLevelTheme.Ruins && gameController.GetComponent<ECCRuinsWorld>() == null) gameController.gameObject.AddComponent<ECCRuinsWorld>();

            // The highway chapter drives on an endless highway between barriers
            if (theme == ECCLevelTheme.Highway) ECCHighwayWorld.Enable(gameController);

            // The rainy road: night, rain and lightning, and a wet road where the cars slide
            if (theme == ECCLevelTheme.Rain)
            {
                ECCCar.grip = WetGrip;

                if (gameController.GetComponent<ECCRainWeather>() == null) gameController.gameObject.AddComponent<ECCRainWeather>();
            }

            if (spawnTuning != null)
            {
                spawnTuning.FilterObstacles(settings.obstacleKeywords);

                // A dense forest: trees more often ( a shorter time between obstacle spawns )
                if (theme == ECCLevelTheme.Forest) spawnTuning.SetSpawnRateMultiplier(ECCSpawnTuning.GroupKind.Obstacles, 1 / ForestTreeDensity);
            }

            // The desert drives on dirt instead of the green ground
            if (theme == ECCLevelTheme.Desert && gameController.groundRenderer)
            {
                Texture2D dirt = DirtGround(gameController.groundRenderer.material.mainTexture);

                if (dirt) gameController.groundRenderer.material.mainTexture = dirt;
            }

            if (gameController.groundRenderer && gameController.groundRenderer.material.HasProperty("_Color"))
            {
                gameController.groundRenderer.material.color = gameController.groundRenderer.material.color * settings.groundTint;
            }

            foreach (Light sceneLight in Object.FindObjectsOfType<Light>())
            {
                if (sceneLight.type == LightType.Directional) sceneLight.color = settings.lightColor;
            }

            RenderSettings.fogColor = settings.fogColor;
        }

        /// <summary>
        /// A dirt-colored copy of the ground texture, with the same pattern ( its light and dark patches become light
        /// and dark dirt ). The texture isn't readable, so it is copied through the GPU once and kept
        /// </summary>
        static Texture2D DirtGround(Texture source)
        {
            if (dirtTexture) return dirtTexture;

            if (source == null) return null;

            const int size = 512;

            // The average brightness of the green ground; brighter patches give lighter dirt
            const float baseBrightness = 0.37f;

            RenderTexture previous = RenderTexture.active;
            RenderTexture copy = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            try
            {
                Graphics.Blit(source, copy);
                RenderTexture.active = copy;

                Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, true);
                texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);

                Color[] pixels = texture.GetPixels();

                for (int index = 0; index < pixels.Length; index++)
                {
                    float shade = Mathf.Clamp(pixels[index].grayscale / baseBrightness, 0.45f, 1.25f);

                    pixels[index] = new Color(Mathf.Clamp01(DirtColor.r * shade), Mathf.Clamp01(DirtColor.g * shade), Mathf.Clamp01(DirtColor.b * shade));
                }

                texture.SetPixels(pixels);
                texture.wrapMode = TextureWrapMode.Repeat;
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = source.anisoLevel;
                texture.name = "DesertDirt";
                texture.Apply(true, true);

                dirtTexture = texture;
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Desert ground: could not make the dirt texture. " + exception.Message);
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(copy);
            }

            return dirtTexture;
        }
    }
}
