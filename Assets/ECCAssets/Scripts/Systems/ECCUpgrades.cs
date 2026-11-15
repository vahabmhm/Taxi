using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCUpgradeType
    {
        Speed = 0,
        Handling = 1,
        Health = 2,
    }

    /// <summary>
    /// Car upgrades bought in the garage. Each car has its own upgrade levels, applied when the chase starts
    /// </summary>
    public static class ECCUpgrades
    {
        public const int MaxLevel = 5;

        // The prices before the economy rebalance ( now in ECCEconomyConfig.upgradeCosts ). Only used to refund the
        // upgrades of the removed cars, which were bought at these prices
        static readonly int[] legacyCosts = { 150, 300, 600, 1000, 1600 };

        // What each upgrade level adds to the car
        public const float SpeedPerLevel = 0.6f;
        public const float HandlingPerLevel = 15;
        public const float HealthPerLevel = 1;

        public static string GetName(ECCUpgradeType type)
        {
            switch (type)
            {
                case ECCUpgradeType.Speed: return "سرعت";
                case ECCUpgradeType.Handling: return "فرمان‌پذیری";
                default: return "استحکام";
            }
        }

        public static int GetLevel(string carName, ECCUpgradeType type)
        {
            ECCUpgradeRecord record = ECCSave.GetUpgrades(carName);

            switch (type)
            {
                case ECCUpgradeType.Speed: return record.speed;
                case ECCUpgradeType.Handling: return record.handling;
                default: return record.health;
            }
        }

        /// <summary>
        /// The price of the next level, or -1 when the upgrade is maxed
        /// </summary>
        public static int GetNextCost(string carName, ECCUpgradeType type)
        {
            int level = GetLevel(carName, type);

            return level >= MaxLevel ? -1 : ECCEconomyConfig.Instance.GetUpgradeCost(level);
        }

        /// <summary>
        /// All the money spent on a car's upgrades, at the old prices ( the refund of the removed cars )
        /// </summary>
        public static int TotalSpent(string carName)
        {
            int total = 0;

            foreach (ECCUpgradeType type in new[] { ECCUpgradeType.Speed, ECCUpgradeType.Handling, ECCUpgradeType.Health })
            {
                int level = GetLevel(carName, type);

                for (int index = 0; index < level && index < legacyCosts.Length; index++) total += legacyCosts[index];
            }

            return total;
        }

        public static bool TryBuy(string carName, ECCUpgradeType type)
        {
            int cost = GetNextCost(carName, type);

            if (cost < 0 || ECCWallet.TrySpend(cost) == false) return false;

            ECCUpgradeRecord record = ECCSave.GetUpgrades(carName);

            switch (type)
            {
                case ECCUpgradeType.Speed: record.speed++; break;
                case ECCUpgradeType.Handling: record.handling++; break;
                default: record.health++; break;
            }

            ECCSave.Save();

            return true;
        }

        /// <summary>
        /// Adds the upgrades to a newly created player car, before its Start runs
        /// </summary>
        public static void ApplyTo(ECCCar car, string carName)
        {
            car.speed += GetLevel(carName, ECCUpgradeType.Speed) * SpeedPerLevel;
            car.rotateSpeed += GetLevel(carName, ECCUpgradeType.Handling) * HandlingPerLevel;
            car.health += GetLevel(carName, ECCUpgradeType.Health) * HealthPerLevel;
        }
    }
}
