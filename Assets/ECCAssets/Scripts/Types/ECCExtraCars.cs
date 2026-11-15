using UnityEngine;

namespace EndlessCarChase.Types
{
    /// <summary>
    /// Cars added to the garage after the ones set on the shop in the scene ( the Kenney Car Kit cars ).
    /// Loaded from Resources/ECCExtraCars and appended by ECCShop.AddExtraItems, so the scene doesn't need to change.
    /// Built by Tools > Endless Car Chase > Build Kenney Cars ( ECCKenneyCarBuilder ).
    /// </summary>
    [CreateAssetMenu(fileName = "ECCExtraCars", menuName = "Endless Car Chase/Extra Garage Cars")]
    public class ECCExtraCars : ScriptableObject
    {
        public const string ResourcePath = "ECCExtraCars";

        [Tooltip("Cars shown after the shop's own cars. lockState 0 = must be bought")]
        public ShopItem[] items = new ShopItem[0];
    }
}
