using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace EndlessCarChase
{
    /// <summary>
    /// The icons of the other games. Order: an icon imported at Resources/ECCOtherGames/Icons/<packageName>.png,
    /// then a copy cached on the device, then a one-time download from the entry's Cafe Bazaar icon address ( only
    /// https://s.cafebazaar.ir/ ), saved to the cache. Without a network the placeholder simply stays
    /// </summary>
    public class ECCGameIcons : MonoBehaviour
    {
        const string ResourceFolder = "ECCOtherGames/Icons/";
        const string AllowedHost = "https://s.cafebazaar.ir/";

        static ECCGameIcons instance;

        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, List<Action<Sprite>>> pending = new Dictionary<string, List<Action<Sprite>>>();

        static ECCGameIcons Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject host = new GameObject("ECCGameIcons");
                    DontDestroyOnLoad(host);

                    instance = host.AddComponent<ECCGameIcons>();
                }

                return instance;
            }
        }

        static string CachePath(string packageName)
        {
            return Path.Combine(Path.Combine(Application.persistentDataPath, "game_icons"), packageName + ".png");
        }

        /// <summary>
        /// The icon if it's already available without a download ( imported or cached on the device ), otherwise null
        /// </summary>
        public static Sprite GetCached(ECCGameEntry game)
        {
            if (game == null || string.IsNullOrEmpty(game.packageName)) return null;

            Sprite sprite;

            if (sprites.TryGetValue(game.packageName, out sprite) && sprite) return sprite;

            Texture2D texture = Resources.Load<Texture2D>(ResourceFolder + game.packageName);

            if (texture == null) texture = LoadFromCache(game.packageName);

            if (texture == null) return null;

            sprite = ToSprite(texture);
            sprites[game.packageName] = sprite;

            return sprite;
        }

        /// <summary>
        /// Gets the icon, downloading it once if needed. done is called with the sprite, or not at all when it can't be loaded
        /// </summary>
        public static void Load(ECCGameEntry game, Action<Sprite> done)
        {
            Sprite cached = GetCached(game);

            if (cached)
            {
                if (done != null) done(cached);
                return;
            }

            if (game == null || IsAllowedUrl(game.iconUrl) == false) return;

            List<Action<Sprite>> waiting;

            if (pending.TryGetValue(game.packageName, out waiting))
            {
                waiting.Add(done);
                return;
            }

            pending[game.packageName] = new List<Action<Sprite>> { done };

            Instance.StartCoroutine(Download(game.packageName, game.iconUrl));
        }

        public static bool IsAllowedUrl(string url)
        {
            return string.IsNullOrEmpty(url) == false && url.StartsWith(AllowedHost, StringComparison.Ordinal);
        }

        static IEnumerator Download(string packageName, string url)
        {
            Sprite sprite = null;

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                request.timeout = 15;

                yield return request.SendWebRequest();

                if (request.isNetworkError == false && request.isHttpError == false)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);

                    if (texture)
                    {
                        sprite = ToSprite(texture);
                        sprites[packageName] = sprite;

                        SaveToCache(packageName, request.downloadHandler.data);
                    }
                }
            }

            List<Action<Sprite>> waiting;

            if (pending.TryGetValue(packageName, out waiting))
            {
                pending.Remove(packageName);

                if (sprite)
                {
                    foreach (Action<Sprite> callback in waiting)
                    {
                        if (callback != null) callback(sprite);
                    }
                }
            }
        }

        static Texture2D LoadFromCache(string packageName)
        {
            try
            {
                string path = CachePath(packageName);

                if (File.Exists(path) == false) return null;

                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                return texture.LoadImage(File.ReadAllBytes(path)) ? texture : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Couldn't read the cached icon of " + packageName + ": " + exception.Message);

                return null;
            }
        }

        static void SaveToCache(string packageName, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;

            try
            {
                string path = CachePath(packageName);

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Couldn't cache the icon of " + packageName + ": " + exception.Message);
            }
        }

        static Sprite ToSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100);
        }
    }
}
