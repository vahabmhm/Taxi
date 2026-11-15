using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The power-ups the player can trigger on demand during a chase ( nitro, shield, EMP shock, magnet, repair kit ).
    /// Each type starts with one free use, given once per save ( existing players get it once too, on their first launch
    /// of this version ). Free uses don't regenerate. Coin prices are in ECCEconomyConfig.
    /// The day 10 login reward is a timed "free shield pass": one free shield per chase until it expires.
    /// </summary>
    public static class ECCPowerUpInventory
    {
        public static readonly ECCPowerUpType[] Types = { ECCPowerUpType.Nitro, ECCPowerUpType.Shield, ECCPowerUpType.EMP, ECCPowerUpType.Magnet, ECCPowerUpType.Repair };

        /// <summary>
        /// Called when a free count or the pass changes
        /// </summary>
        public static event System.Action Changed;

        // The pass gives one free shield per chase; the scene reloads between chases, so a static flag is enough
        static bool passUsedThisRun = false;

        static void EnsureGranted()
        {
            ECCSaveData data = ECCSave.Data;

            if (data.powerUpsGranted && data.repairGranted) return;

            int gift = Mathf.Max(0, ECCEconomyConfig.Instance.initialFreeUses);

            foreach (ECCPowerUpType type in Types)
            {
                // The repair kit came in a later version: players who already got the first gift get only its free use
                bool repair = type == ECCPowerUpType.Repair;

                if (repair ? data.repairGranted == false : data.powerUpsGranted == false) GetStock(type).free += gift;
            }

            data.powerUpsGranted = true;
            data.repairGranted = true;

            ECCSave.Save();
        }

        static ECCPowerUpStock GetStock(ECCPowerUpType type)
        {
            foreach (ECCPowerUpStock stock in ECCSave.Data.powerUpStock)
            {
                if (stock.type == (int)type) return stock;
            }

            ECCPowerUpStock newStock = new ECCPowerUpStock { type = (int)type };

            ECCSave.Data.powerUpStock.Add(newStock);

            return newStock;
        }

        public static int GetFreeUses(ECCPowerUpType type)
        {
            EnsureGranted();

            return Mathf.Max(0, GetStock(type).free);
        }

        /// <summary>
        /// Adds free uses ( a watched video whose power-up couldn't be applied any more is kept for later )
        /// </summary>
        public static void AddFreeUses(ECCPowerUpType type, int amount)
        {
            if (amount <= 0) return;

            EnsureGranted();

            GetStock(type).free += amount;

            ECCSave.Save();

            if (Changed != null) Changed();
        }

        /// <summary>
        /// How many times a power-up's explanation popup is shown. After that a tap pays the coins right away
        /// ( a video still asks first, so a stray tap never starts one )
        /// </summary>
        public const int HelpPopupLimit = 2;

        public static bool ShouldShowHelp(ECCPowerUpType type)
        {
            return GetStock(type).helpShown < HelpPopupLimit;
        }

        public static void MarkHelpShown(ECCPowerUpType type)
        {
            ECCPowerUpStock stock = GetStock(type);

            if (stock.helpShown >= HelpPopupLimit) return;

            stock.helpShown++;

            ECCSave.Save();
        }

        public static int GetPrice(ECCPowerUpType type)
        {
            return Mathf.Max(0, ECCEconomyConfig.Instance.GetPowerUpPrice(type));
        }

        // ----- Day 10 free shield pass -----

        public static bool IsShieldPassActive
        {
            get { return ECCSave.Data.freeShieldPassUntil > ECCDaily.UnixNow; }
        }

        /// <summary>
        /// Seconds left on the free shield pass, 0 when it's not active
        /// </summary>
        public static long ShieldPassSecondsLeft
        {
            get { return System.Math.Max(0, ECCSave.Data.freeShieldPassUntil - ECCDaily.UnixNow); }
        }

        /// <summary>
        /// Starts or extends the free shield pass ( never permanent )
        /// </summary>
        public static void GrantShieldPass(float hours, bool save = true)
        {
            long from = System.Math.Max(ECCDaily.UnixNow, ECCSave.Data.freeShieldPassUntil);

            ECCSave.Data.freeShieldPassUntil = from + (long)(Mathf.Max(0, hours) * 3600);

            if (save) ECCSave.Save();

            if (Changed != null) Changed();
        }

        public static bool HasPassUse(ECCPowerUpType type)
        {
            return type == ECCPowerUpType.Shield && IsShieldPassActive && passUsedThisRun == false;
        }

        public static void ResetRun()
        {
            passUsedThisRun = false;
        }

        // ----- Activation -----

        public enum PayMethod
        {
            Free,
            Coins,
            Video,
        }

        /// <summary>
        /// How the next use of this power-up would be paid
        /// </summary>
        public static PayMethod GetPayMethod(ECCPowerUpType type)
        {
            if (HasPassUse(type) || GetFreeUses(type) > 0) return PayMethod.Free;

            if (ECCWallet.CanAfford(GetPrice(type))) return PayMethod.Coins;

            return PayMethod.Video;
        }

        /// <summary>
        /// Uses a free use or coins and applies the effect. The free use or the coins are only taken after the effect was
        /// applied; a failed effect costs nothing. Returns false if it couldn't be used this way ( then a video is needed )
        /// </summary>
        public static bool TryUseFreeOrCoins(ECCPowerUpType type, System.Func<bool> applyEffect)
        {
            PayMethod method = GetPayMethod(type);

            if (method == PayMethod.Video) return false;

            int price = GetPrice(type);

            // Check the balance again right before applying, then charge only on success
            if (method == PayMethod.Coins && ECCWallet.CanAfford(price) == false) return false;

            if (applyEffect() == false) return false;

            if (method == PayMethod.Free)
            {
                if (HasPassUse(type))
                {
                    passUsedThisRun = true;
                }
                else
                {
                    GetStock(type).free--;
                    ECCSave.Save();
                }
            }
            else
            {
                ECCWallet.TrySpend(price);
            }

            if (Changed != null) Changed();

            return true;
        }
    }
}
