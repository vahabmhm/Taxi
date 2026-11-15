using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The store where players are asked to leave a review
    /// </summary>
    public enum ECCReviewStore
    {
        Bazaar = 0, // Cafe Bazaar
        Myket = 1,
    }

    /// <summary>
    /// Opens the review page of this game in the selected Android store with an Intent.
    ///
    /// NOTE: no BazaarReviewManager.cs existed in this project ( and the older connectballs project isn't available here ),
    /// so this is a new, compatible manager. The store is chosen with the "store" field below ( Inspector, or the default
    /// value here ); the review prompt text follows the same field.
    /// - Bazaar: ACTION_EDIT, bazaar://details?id=<package>, app com.farsitel.bazaar ( Cafe Bazaar's rating intent )
    /// - Myket:  ACTION_VIEW, myket://comment?id=<package>, app ir.mservices.market ( Myket's comment intent )
    /// If the store app isn't installed or the intent fails, the same store's HTTPS page of the game is opened instead
    /// ( never the other store ). Outside Android ( Editor ) only the HTTPS page can be opened.
    /// The intents are written from the stores' public developer documentation and must be checked on an Android device.
    /// </summary>
    public class BazaarReviewManager : MonoBehaviour
    {
        [Tooltip("The store the review prompt sends players to")]
        public ECCReviewStore store = ECCReviewStore.Bazaar;

        [Tooltip("Leave empty to use the app's package name ( Application.identifier )")]
        public string packageNameOverride = string.Empty;

        // This game's Android package, used when Application.identifier is empty ( Editor on another platform )
        const string DefaultPackage = "com.car.chase.vp";

        public const string BazaarPackage = "com.farsitel.bazaar";
        public const string MyketPackage = "ir.mservices.market";

        static BazaarReviewManager instance;

        /// <summary>
        /// The manager in the scene, or one created with the default settings
        /// </summary>
        public static BazaarReviewManager Instance
        {
            get
            {
                if (instance == null) instance = FindObjectOfType<BazaarReviewManager>();

                if (instance == null) instance = new GameObject("BazaarReviewManager").AddComponent<BazaarReviewManager>();

                return instance;
            }
        }

        public string PackageName
        {
            get
            {
                if (string.IsNullOrEmpty(packageNameOverride) == false) return packageNameOverride.Trim();

                return string.IsNullOrEmpty(Application.identifier) ? DefaultPackage : Application.identifier;
            }
        }

        public string IntentAction
        {
            get { return store == ECCReviewStore.Myket ? "android.intent.action.VIEW" : "android.intent.action.EDIT"; }
        }

        public string IntentData
        {
            get { return store == ECCReviewStore.Myket ? "myket://comment?id=" + PackageName : "bazaar://details?id=" + PackageName; }
        }

        public string StorePackage
        {
            get { return store == ECCReviewStore.Myket ? MyketPackage : BazaarPackage; }
        }

        /// <summary>
        /// The same store's web page of the game, used when its app can't be opened
        /// </summary>
        public string WebFallbackUrl
        {
            get { return store == ECCReviewStore.Myket ? "https://myket.ir/app/" + PackageName : "https://cafebazaar.ir/app/" + PackageName; }
        }

        /// <summary>
        /// Opens the review page. Returns true if the store app was opened, false if the web page was used instead
        /// </summary>
        public bool OpenReview()
        {
            if (ECCStoreLinks.TryStartIntent(IntentAction, IntentData, StorePackage)) return true;

            Application.OpenURL(WebFallbackUrl);

            return false;
        }
    }
}
