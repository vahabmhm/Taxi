using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EndlessCarChase
{
    [Serializable]
    public class ECCGameEntry
    {
        [Tooltip("The real Android package name, used as the stable id and for the Cafe Bazaar listing ( cafebazaar.ir/app/<package> )")]
        public string packageName;

        [Tooltip("The game's name as shown on Cafe Bazaar")]
        public string title;

        [Tooltip("A short Persian description ( optional )")]
        public string description;

        [Tooltip("The Cafe Bazaar icon, imported into the project ( optional: otherwise loaded from iconUrl and cached )")]
        public Sprite icon;

        [Tooltip("The Cafe Bazaar icon address ( s.cafebazaar.ir ), downloaded once and cached when no icon sprite is set")]
        public string iconUrl;

        [Tooltip("Tick only after checking that the package name opens this exact game on Cafe Bazaar")]
        public bool verified;
    }

    /// <summary>
    /// The developer's other games, shared by the «سایر بازی‌ها» screen and the daily recommendation popup.
    ///
    /// Source of the games, in order:
    /// 1. Resources/ECCGameCatalog ( this ScriptableObject, if one was created in the Editor )
    /// 2. Resources/ECCOtherGames.json: the verified list supplied by the developer ( package names from the sibling projects,
    ///    titles, descriptions and icon addresses from the public Cafe Bazaar listings ). An entry is only used when its
    ///    listing link and deep link match its package name.
    /// Icons: Resources/ECCOtherGames/Icons/<packageName>.png if imported, otherwise downloaded once from iconUrl and cached
    /// ( ECCGameIcons ), with a placeholder meanwhile or when offline.
    /// </summary>
    [CreateAssetMenu(fileName = "ECCGameCatalog", menuName = "Endless Car Chase/Game Catalog")]
    public class ECCGameCatalog : ScriptableObject
    {
        const string ResourcePath = "ECCGameCatalog";

        /// <summary>
        /// The developer page set on the menu's "More games" button in the ECCGame scene
        /// </summary>
        public const string DefaultDeveloperPage = "https://cafebazaar.ir/developer/774612137738";

        // This game, excluded from its own list
        const string ThisGamePackage = "com.car.chase.vp";

        public string developerPageUrl = DefaultDeveloperPage;

        const string JsonResourcePath = "ECCOtherGames";

        [Serializable]
        class JsonGame
        {
            public string packageName;
            public string title;
            public string description;
            public string bazaarUrl;
            public string bazaarDeepLink;
            public string iconUrl;
        }

        [Serializable]
        class JsonCatalog
        {
            public string verifiedOn;
            public JsonGame[] games;
        }

        /// <summary>
        /// Reads the developer's verified game list. An entry is marked verified only if the file says when it was verified
        /// and its Bazaar links point to its own package name
        /// </summary>
        public static List<ECCGameEntry> ParseJson(string text)
        {
            List<ECCGameEntry> entries = new List<ECCGameEntry>();

            JsonCatalog catalog = null;

            try
            {
                catalog = JsonUtility.FromJson<JsonCatalog>(text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Couldn't read the other games list: " + exception.Message);
            }

            if (catalog == null || catalog.games == null) return entries;

            bool fileVerified = string.IsNullOrEmpty(catalog.verifiedOn) == false;

            foreach (JsonGame game in catalog.games)
            {
                if (game == null || string.IsNullOrEmpty(game.packageName)) continue;

                string package = game.packageName.Trim();

                bool linksMatch = (string.IsNullOrEmpty(game.bazaarUrl) || game.bazaarUrl.Trim() == ECCStoreLinks.BazaarListingUrl(package))
                    && (string.IsNullOrEmpty(game.bazaarDeepLink) || game.bazaarDeepLink.Trim() == "bazaar://details?id=" + package);

                entries.Add(new ECCGameEntry
                {
                    packageName = package,
                    title = game.title,
                    description = game.description,
                    iconUrl = game.iconUrl,
                    verified = fileVerified && linksMatch,
                });
            }

            return entries;
        }

        public List<ECCGameEntry> games = new List<ECCGameEntry>();

        static ECCGameCatalog instance;

        public static ECCGameCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCGameCatalog>(ResourcePath);

                    if (instance == null || instance.games == null || instance.games.Count == 0)
                    {
                        instance = CreateInstance<ECCGameCatalog>();

                        TextAsset json = Resources.Load<TextAsset>(JsonResourcePath);

                        if (json) instance.games = ParseJson(json.text);
                    }
                }

                return instance;
            }
        }

        static readonly Regex PackagePattern = new Regex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$");

        public static bool IsValidPackageName(string packageName)
        {
            return string.IsNullOrEmpty(packageName) == false && PackagePattern.IsMatch(packageName);
        }

        static string CurrentPackage
        {
            get { return string.IsNullOrEmpty(Application.identifier) ? ThisGamePackage : Application.identifier; }
        }

        /// <summary>
        /// The games that can be shown: verified, with a valid package name and a title, not this game, no duplicates
        /// </summary>
        public List<ECCGameEntry> EligibleGames
        {
            get
            {
                List<ECCGameEntry> eligible = new List<ECCGameEntry>();
                HashSet<string> seen = new HashSet<string>();

                foreach (ECCGameEntry game in games)
                {
                    if (game == null || game.verified == false || IsValidPackageName(game.packageName) == false || string.IsNullOrEmpty(game.title)) continue;

                    string package = game.packageName.Trim();

                    if (package == CurrentPackage || package == ThisGamePackage || seen.Add(package) == false) continue;

                    eligible.Add(game);
                }

                return eligible;
            }
        }

        public ECCGameEntry Find(string packageName)
        {
            foreach (ECCGameEntry game in EligibleGames)
            {
                if (game.packageName == packageName) return game;
            }

            return null;
        }
    }
}
