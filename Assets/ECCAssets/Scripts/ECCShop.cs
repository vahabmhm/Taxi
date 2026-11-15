using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EndlessCarChase.Types;

namespace EndlessCarChase
{
	/// <summary>
	/// This script displays items that can be unlocked with in-game money. Each item has a 3D icon that spins while showing the price of the item if it is still locked. Locked items are also darkened.
	/// </summary>
	public class ECCShop : MonoBehaviour 
	{
        // Hold the gamecontroller for quicker access
        internal ECCGameController gameController;

        //How much money we have left in the shop
        internal int moneyLeft = 0;

        [Tooltip("The text object that displays the money we have")]
        public Transform moneyText;

        [Tooltip("The text object that displays the price of the current item")]
        public Text priceText;
    
        [Tooltip("The sign that appears next to the money text")]
        public string moneySign = "$";

        [Tooltip("The player prefs record of the money we have")]
        public string moneyPlayerprefs = "Money";

        [Tooltip("Hide the money text and use the shared coin balance button instead")]
        public bool useSharedBalanceButton = true;

        //The current item we are on. The first item is 0, the second 1, etc
        internal int currentItem = 0;

        [Tooltip("The player prefs record of the current item we selected")]
        public string currentPlayerprefs = "CurrentCar";

        [Tooltip("A list of items in the game, their names, the scene they link to, the price to unlock it, and the state of the item ( -1 - locked and can't be played, 0 - unlocked no stars, 1-X the star rating we got on the item")]
        public ShopItem[] items;
        
        // The current item icon, 2D or 3D
        internal Transform currentIcon;
        
        [Tooltip("Paint locked cars black ( the template's old look ). Off: locked cars show their colors and the garage shows a lock label")]
        public bool darkenLockedItems = false;

        [Tooltip("The effect that appears when we unlock this item")]
        public Transform unlockEffect;

        [Tooltip("Buttons for going to the next/previous items, and selecting items")]
        public Button buttonNextItem;
        public Button buttonPrevItem;
        public Button buttonSelectItem;

        [Tooltip("Keyboard/Gamepad buttons for changing and selecting items")]
        public string changeItemButton = "Horizontal";
        public string selectItemButton = "Submit";
        internal bool buttonPressed = false;

        [Tooltip("The camera object that is turned on when this shop is activated")]
        public GameObject shopCamera;

        [Tooltip("The position of the currently selected item in 3D space")]
        public Vector3 itemIconPosition;

        [Tooltip("The rotation speed of the currently selected item in 3D space")]
        public float itemSpinSpeed = 100;
        internal float itemRotation = 0;

        // Maximum values of stats for Speed, Rotation, Health, and Damage
        internal float speedMax = 0;
        internal float rotateSpeedMax = 0;
        internal float healthMax = 0;
        internal float damageMax = 0;

        internal int index;

        bool extraItemsAdded = false;

        // The six Kenney cars that were cars 13 to 18 of the garage ( removed ), with the prices they were sold for
        static readonly string[] RemovedCars = { "Kenney_HatchbackSports", "Kenney_Taxi", "Kenney_SedanSports", "Kenney_SuvLuxury", "Kenney_Race", "Kenney_RaceFuture" };
        static readonly int[] RemovedCarPrices = { 1500, 2500, 4000, 6000, 8000, 12000 };

        /// <summary>
        /// A player who bought one of the removed cars gets its price and the upgrades bought for it back, once
        /// ( idempotent wallet rewards ). The lock state was saved under the shop's instance name, "( Clone )"
        /// </summary>
        static void RefundRemovedCars()
        {
            for (int index = 0; index < RemovedCars.Length; index++)
            {
                string car = RemovedCars[index];

                if (PlayerPrefs.GetInt(car + "(Clone)", 0) != 1) continue;

                ECCWallet.TryReward("refund-removed-car-" + car, RemovedCarPrices[index] + ECCUpgrades.TotalSpent(car));
            }
        }

        /// <summary>
        /// Appends the extra garage cars ( Resources/ECCExtraCars ) after the cars set in the scene, once.
        /// Called before the list is used ( the game controller reads the selected car in its Awake )
        /// </summary>
        public void AddExtraItems()
        {
            if (extraItemsAdded) return;

            extraItemsAdded = true;

            RefundRemovedCars();

            ECCExtraCars extras = Resources.Load<ECCExtraCars>(ECCExtraCars.ResourcePath);

            if (extras != null && extras.items != null)
            {
                System.Collections.Generic.List<ShopItem> list = new System.Collections.Generic.List<ShopItem>(items);

                foreach (ShopItem extra in extras.items)
                {
                    if (extra == null || extra.itemIcon == null || list.Exists(item => item.itemIcon == extra.itemIcon)) continue;

                    // A copy, so the lock state read at runtime never changes the asset
                    list.Add(new ShopItem { itemIcon = extra.itemIcon, lockState = extra.lockState, price = extra.price });
                }

                items = list.ToArray();
            }

            // The scene's locked cars have no price ( 0 ), so they were free and there was nothing to buy. They get
            // rising prices from the economy config. Cars a player already took stay theirs ( saved per car name )
            int order = 0;

            foreach (ShopItem item in items)
            {
                if (item == null || item.lockState == 1 || item.price > 0) continue;

                item.price = ECCEconomyConfig.Instance.GetDefaultCarPrice(order++);
            }
        }

        public void OnEnable()
        {
            AddExtraItems();

            // Activate the shop camera
            if (shopCamera) shopCamera.SetActive(true);

            //Get the number of money we have ( the shared coin wallet )
            moneyLeft = ECCWallet.Balance;

            //Update the text of the money we have
            moneyText.GetComponent<Text>().text = moneyLeft.ToString() + moneySign;

            // The balance is shown by the shared coin balance button ( ECCMenuHub ), so the old money text is hidden
            if (useSharedBalanceButton) moneyText.gameObject.SetActive(false);

            ECCWallet.BalanceChanged += OnBalanceChanged;

            // If the max stats for speed rotation and health have not been set yet, set them based on the highest stats in all items
            if (speedMax == 0 && rotateSpeedMax == 0 && healthMax == 0 && damageMax == 0)
            {
                for (index = 0; index < items.Length; index++)
                {
                    if (speedMax < items[index].itemIcon.GetComponent<ECCCar>().speed) speedMax = items[index].itemIcon.GetComponent<ECCCar>().speed;
                    if (rotateSpeedMax < items[index].itemIcon.GetComponent<ECCCar>().rotateSpeed) rotateSpeedMax = items[index].itemIcon.GetComponent<ECCCar>().rotateSpeed;
                    if (healthMax < items[index].itemIcon.GetComponent<ECCCar>().health) healthMax = items[index].itemIcon.GetComponent<ECCCar>().health;
                    if (damageMax < items[index].itemIcon.GetComponent<ECCCar>().damage) damageMax = items[index].itemIcon.GetComponent<ECCCar>().damage;
                }
            }

            //Get the number of the current item
            currentItem = PlayerPrefs.GetInt(currentPlayerprefs, currentItem);

            // A removed car may have been the selected one: back to the first car
            if (currentItem < 0 || currentItem >= items.Length) currentItem = 0;

            // Set the item
            ChangeItem(0);
        }

        void OnBalanceChanged(int balance)
        {
            moneyLeft = balance;

            if (moneyText && moneyText.GetComponent<Text>()) moneyText.GetComponent<Text>().text = moneyLeft.ToString() + moneySign;
        }

        public void OnDisable()
        {
            ECCWallet.BalanceChanged -= OnBalanceChanged;

            // Deactivate the shop camera
            if (shopCamera) shopCamera.SetActive(false);

            // Remove the icon of the previous item
            if (currentIcon)
            {
                // Reset the item rotation
                itemRotation = currentIcon.eulerAngles.y;

                Destroy(currentIcon.gameObject);
            }
        }

        void Start()
        {
            //PlayerPrefs.DeleteAll();

            // If there are no items in the shop, we can't use it
            if (items.Length <= 0) return;

            // Get the gamecontroller from the scene so we can update the selected item
            if (gameController == null) gameController = GameObject.FindObjectOfType<ECCGameController>();

            // Listen for a click to change to the next item
            buttonNextItem.onClick.AddListener(delegate { ChangeItem(1); });

            // Listen for a click to change to the next item
            buttonPrevItem.onClick.AddListener(delegate { ChangeItem(-1); });

            // Listen for a click to start the game in the current item
            buttonSelectItem.onClick.AddListener(delegate { StartCoroutine("SelectItem"); });
        }

        void Update()
        {
            // If there are no items in the shop, we can't use it
            if (items.Length <= 0) return;

            // Rotate the current item
            if (currentIcon) currentIcon.Rotate(Vector3.up * itemSpinSpeed * Time.deltaTime, Space.World);

            // Keyboard and gamepad controls
            if ( buttonPressed == false )
            {
                if (Input.GetAxisRaw(changeItemButton) > 0) buttonNextItem.onClick.Invoke();// ChangeItem(1);
                if (Input.GetAxisRaw(changeItemButton) < 0) buttonPrevItem.onClick.Invoke();// ChangeItem(-1);
                if (Input.GetButtonDown(selectItemButton)) buttonSelectItem.onClick.Invoke();// SelectItem();

                if (Input.GetAxisRaw(changeItemButton) != 0 || Input.GetButton(selectItemButton)) buttonPressed = true;

            }
            else if (Input.GetAxisRaw(changeItemButton) == 0 || Input.GetButtonUp(selectItemButton)) buttonPressed = false;
        }

        /// <summary>
        /// Changes to the next or previous item and updates the new item based on lockstate
        /// </summary>
        /// <param name="changeValue"></param>
        public void ChangeItem( int changeValue )
        {
            // If there are no items in the shop, we can't use it
            if (items.Length <= 0) return;

            // Change the index of the item
            currentItem += changeValue;

            // Make sure we don't go out of the list of available items
            if (currentItem > items.Length - 1) currentItem = 0;
            else if (currentItem < 0) currentItem = items.Length - 1;

            // Remove the icon of the previous item
            if (currentIcon)
            {
                // Reset the item rotation
                itemRotation = currentIcon.eulerAngles.y;

                Destroy(currentIcon.gameObject);
            }

            // Display the icon of the current item
            if ( items[currentItem].itemIcon )
            {
                // Create the current item
                currentIcon = Instantiate(items[currentItem].itemIcon.transform, itemIconPosition, Quaternion.identity) as Transform;

                // Set the rotation of the item
                currentIcon.eulerAngles = Vector3.up * itemRotation;

                // If the item has an animation, play it
                if (currentIcon.GetComponent<Animation>()) currentIcon.GetComponent<Animation>().Play();

                // Set the stats of the current item, move speed, health, rotation speed, and damage
                if ( currentIcon.GetComponent<ECCCar>())
                {
                    // Fill values for the icons
                    if (transform.Find("Base/Stats/Speed/Full"))    transform.Find("Base/Stats/Speed/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<ECCCar>().speed/speedMax;
                    if (transform.Find("Base/Stats/RotateSpeed/Full")) transform.Find("Base/Stats/RotateSpeed/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<ECCCar>().rotateSpeed/rotateSpeedMax;
                    if (transform.Find("Base/Stats/Health/Full")) transform.Find("Base/Stats/Health/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<ECCCar>().health/healthMax;
                    if (transform.Find("Base/Stats/Damage/Full")) transform.Find("Base/Stats/Damage/Full").GetComponent<Image>().fillAmount = currentIcon.GetComponent<ECCCar>().damage / damageMax;

                    // Text values inside the icons
                    if (transform.Find("Base/Stats/Speed/Text")) transform.Find("Base/Stats/Speed/Text").GetComponent<Text>().text = currentIcon.GetComponent<ECCCar>().speed.ToString();
                    if (transform.Find("Base/Stats/RotateSpeed/Text")) transform.Find("Base/Stats/RotateSpeed/Text").GetComponent<Text>().text = currentIcon.GetComponent<ECCCar>().rotateSpeed.ToString();
                    if (transform.Find("Base/Stats/Health/Text")) transform.Find("Base/Stats/Health/Text").GetComponent<Text>().text = currentIcon.GetComponent<ECCCar>().health.ToString();
                    if (transform.Find("Base/Stats/Damage/Text")) transform.Find("Base/Stats/Damage/Text").GetComponent<Text>().text = currentIcon.GetComponent<ECCCar>().damage.ToString();
                }
            }

            // Get the lock state of the current item
            items[currentItem].lockState = PlayerPrefs.GetInt(currentIcon.name, items[currentItem].lockState);

            // If the item is unlocked, show the "GO!" button
            if ( items[currentItem].lockState == 1 )
            {
                if (priceText) priceText.text = "ﻉﻭﺮﺷ!";

                // Set the currently selected item in the shop
                PlayerPrefs.SetInt(currentPlayerprefs, currentItem);
            }
            else // Otherwise, show the unlock price ( and darken the item if set )
            {
                // Get the renderer of the chasis ( main body ) of the car object
                Transform chassis = darkenLockedItems ? currentIcon.transform.Find("Base/Chasis") : null;
                MeshRenderer meshRenderer = chassis ? chassis.GetComponent<MeshRenderer>() : null;

                // Go through all the materials of the renderer and darken them
                for (index = 0; meshRenderer && index < meshRenderer.materials.Length; index++)
                {
                    meshRenderer.materials[index].SetColor("_Color", Color.black);
                    meshRenderer.materials[index].SetColor("_EmissionColor", Color.black);
                }

                // Display the price of the current item
                if (priceText) priceText.text = items[currentItem].price.ToString() + moneySign.ToString();
            }
        }
        
        /// <summary>
        /// Select the current item. If it's locked and we have enough money we unlock it.
        /// If it is already unlocked we play with the selected item.
        /// </summary>
        public void SelectItem()
        {
            // If the item is unlocked, select it and start the game
            if (items[currentItem].lockState == 1)
            {
                // Set this item as the player object
                gameController.playerObject = items[currentItem].itemIcon.GetComponent<ECCCar>();

                // Start the game
                gameController.StartGame();

                // Destroy the shop icon
                Destroy(currentIcon.gameObject);

                // Hide the shop camera
                if (shopCamera) shopCamera.SetActive(false);

                // Deactivate the shop object
                gameObject.SetActive(false);
            }
            else if ( ECCWallet.TrySpend(items[currentItem].price) )
            {
                // Unlock the item
                items[currentItem].lockState = 1;

                // Set the lock state of the current item
                PlayerPrefs.SetInt(currentIcon.name, items[currentItem].lockState);
                PlayerPrefs.Save();

                // The price was already spent through the shared wallet
                moneyLeft = ECCWallet.Balance;

                //Update the text of the money we have
                moneyText.GetComponent<Text>().text = moneyLeft.ToString() + moneySign;

                // Create the unlock effect on the item
                if (unlockEffect) Instantiate(unlockEffect, currentIcon.position, currentIcon.rotation);

                // Set the item
                ChangeItem(0);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawCube(itemIconPosition, new Vector3(1,0.1f,1));
        }
    }
}