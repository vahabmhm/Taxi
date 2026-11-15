using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The daily lucky wheel «گردونه شانس»: one reward-bearing spin per calendar day ( ECCDaily ).
    /// The prize is chosen from the weighted table in ECCEconomyConfig BEFORE the wheel turns, saved right away
    /// ( day, segment, coins ), and the wheel is animated to stop exactly on that segment. The coins are paid with the
    /// transaction id "wheel-<day>", so restarting during the spin pays once ( on the next menu visit ) and never twice.
    /// </summary>
    public static class ECCLuckyWheel
    {
        public static ECCWheelPrize[] Prizes
        {
            get
            {
                ECCWheelPrize[] prizes = ECCEconomyConfig.Instance.wheelPrizes;

                return prizes != null && prizes.Length > 0 ? prizes : new[] { new ECCWheelPrize { coins = 50, weight = 1 } };
            }
        }

        public static bool IsAvailableToday
        {
            get { return ECCDaily.IsToday(ECCSave.Data.wheelDay) == false; }
        }

        /// <summary>
        /// Picks and saves today's prize. Returns the segment index, or -1 if today's spin was already used
        /// </summary>
        public static int Spin()
        {
            if (IsAvailableToday == false) return -1;

            ECCWheelPrize[] prizes = Prizes;

            int index = PickWeighted(prizes, Random.value);

            ECCSaveData data = ECCSave.Data;
            data.wheelDay = ECCDaily.Today;
            data.wheelResult = index;
            data.wheelPrize = Mathf.Max(0, prizes[index].coins);
            ECCSave.Save();

            ECCMenuHub.RefreshIndicators();

            return index;
        }

        /// <summary>
        /// The segment for a random value in [0, 1), proportional to the weights ( negative weights count as 0 )
        /// </summary>
        public static int PickWeighted(ECCWheelPrize[] prizes, float randomValue)
        {
            float total = 0;

            foreach (ECCWheelPrize prize in prizes) total += Mathf.Max(0, prize.weight);

            if (total <= 0) return 0;

            float roll = Mathf.Clamp01(randomValue) * total;

            for (int index = 0; index < prizes.Length; index++)
            {
                float weight = Mathf.Max(0, prizes[index].weight);

                if (roll < weight) return index;

                roll -= weight;
            }

            // randomValue == 1: the last segment with a weight
            for (int index = prizes.Length - 1; index >= 0; index--)
            {
                if (prizes[index].weight > 0) return index;
            }

            return 0;
        }

        /// <summary>
        /// Pays the saved prize if it wasn't paid yet. Returns the coins paid now
        /// </summary>
        public static int PayPending()
        {
            ECCSaveData data = ECCSave.Data;

            if (string.IsNullOrEmpty(data.wheelDay) || data.wheelResult < 0 || data.wheelPrize <= 0) return 0;

            return ECCWallet.TryReward("wheel-" + data.wheelDay, data.wheelPrize) ? data.wheelPrize : 0;
        }

        // ----- Geometry shared by the UI and the tests -----

        /// <summary>
        /// The wheel rotation ( z degrees, counter-clockwise positive ) that puts the middle of a segment under the top pointer
        /// </summary>
        public static float AngleForSegment(int index, int count)
        {
            float segment = 360f / count;

            return (index + 0.5f) * segment;
        }

        /// <summary>
        /// The segment under the top pointer for a wheel rotation. Segment i covers [i, i+1) * 360/count degrees,
        /// clockwise from the top of the wheel
        /// </summary>
        public static int SegmentAtPointer(float wheelAngle, int count)
        {
            float angle = Mathf.Repeat(wheelAngle, 360);

            return Mathf.Clamp(Mathf.FloorToInt(angle / (360f / count)), 0, count - 1);
        }
    }
}
