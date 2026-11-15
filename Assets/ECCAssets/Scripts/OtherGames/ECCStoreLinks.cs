using System;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Opens Cafe Bazaar listings. On Android it first tries the Cafe Bazaar app ( bazaar://details?id=<package>, the
    /// documented Bazaar deep link, restricted to the Bazaar app package com.farsitel.bazaar ), and falls back to the
    /// HTTPS listing page in the browser when the app is missing or the intent fails. Elsewhere it opens the HTTPS page
    /// </summary>
    public static class ECCStoreLinks
    {
        public const string BazaarAppPackage = "com.farsitel.bazaar";

        public static string BazaarListingUrl(string packageName)
        {
            return "https://cafebazaar.ir/app/" + packageName;
        }

        public static void OpenBazaarListing(string packageName)
        {
            if (ECCGameCatalog.IsValidPackageName(packageName) == false) return;

            if (TryStartIntent("android.intent.action.VIEW", "bazaar://details?id=" + packageName, BazaarAppPackage)) return;

            Application.OpenURL(BazaarListingUrl(packageName));
        }

        public static void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url) == false) Application.OpenURL(url);
        }

        /// <summary>
        /// Starts an Android activity with an intent restricted to one app package. Returns false if it isn't possible
        /// ( not Android, the app isn't installed, or any other launch error )
        /// </summary>
        public static bool TryStartIntent(string action, string data, string targetPackage)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri"))
                using (AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", data))
                using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", action, uri))
                {
                    if (string.IsNullOrEmpty(targetPackage) == false) intent.Call<AndroidJavaObject>("setPackage", targetPackage);

                    // FLAG_ACTIVITY_NEW_TASK
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000);

                    activity.Call("startActivity", intent);
                }

                return true;
            }
            catch (Exception exception)
            {
                // ActivityNotFoundException when the store app isn't installed
                Debug.LogWarning("Couldn't open " + data + " in " + targetPackage + ": " + exception.Message);

                return false;
            }
#else
            return false;
#endif
        }
    }
}
