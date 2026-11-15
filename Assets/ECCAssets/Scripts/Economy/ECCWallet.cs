using System;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The single coin wallet of the game. It wraps the existing "Money" PlayerPrefs balance ( so old balances are kept ),
    /// raises an event when the balance changes, and pays rewards through idempotent transactions:
    /// a reward id is paid at most once, so double callbacks, double taps and restarts can't pay twice.
    /// </summary>
    public static class ECCWallet
    {
        // How many paid reward ids are remembered. Daily ids contain the day, so old ones are safe to forget
        const int MaxRememberedTransactions = 400;

        /// <summary>
        /// Called with the new balance whenever it changes, from anywhere in the game
        /// </summary>
        public static event Action<int> BalanceChanged;

        public static int Balance
        {
            get { return ECCSave.Money; }
        }

        public static bool CanAfford(int amount)
        {
            return amount >= 0 && Balance >= amount;
        }

        /// <summary>
        /// Adds coins without a transaction id. Use TryReward for anything that could be triggered twice
        /// </summary>
        public static void Add(int amount)
        {
            if (amount <= 0) return;

            ECCSave.Money = SafeSum(Balance, amount);
        }

        /// <summary>
        /// Spends coins if the balance is enough. Returns false and changes nothing otherwise
        /// </summary>
        public static bool TrySpend(int amount)
        {
            if (amount < 0 || Balance < amount) return false;

            if (amount > 0) ECCSave.Money = Balance - amount;

            return true;
        }

        /// <summary>
        /// True if the reward with this id was already paid
        /// </summary>
        public static bool IsPaid(string transactionId)
        {
            return string.IsNullOrEmpty(transactionId) == false && ECCSave.Data.transactions.Contains(transactionId);
        }

        /// <summary>
        /// Pays a reward once. The id and the new balance are written together. Returns false if it was already paid
        /// </summary>
        public static bool TryReward(string transactionId, int amount)
        {
            if (string.IsNullOrEmpty(transactionId) || amount <= 0) return false;

            ECCSaveData data = ECCSave.Data;

            if (data.transactions.Contains(transactionId)) return false;

            data.transactions.Add(transactionId);

            if (data.transactions.Count > MaxRememberedTransactions) data.transactions.RemoveRange(0, data.transactions.Count - MaxRememberedTransactions);

            // Stage the transaction id, then the money setter flushes both in the same PlayerPrefs.Save
            ECCSave.Save(false);

            ECCSave.Money = SafeSum(Balance, amount);

            return true;
        }

        /// <summary>
        /// Records a non-coin reward ( like a timed power-up ) under a transaction id. Returns false if it was already
        /// recorded, so the reward must not be given again
        /// </summary>
        public static bool TryRecord(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return false;

            ECCSaveData data = ECCSave.Data;

            if (data.transactions.Contains(transactionId)) return false;

            data.transactions.Add(transactionId);

            if (data.transactions.Count > MaxRememberedTransactions) data.transactions.RemoveRange(0, data.transactions.Count - MaxRememberedTransactions);

            ECCSave.Save();

            return true;
        }

        /// <summary>
        /// Formats a coin amount with thousands separators, like 12,500
        /// </summary>
        public static string Format(int amount)
        {
            return amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static void NotifyChanged(int newBalance)
        {
            if (BalanceChanged != null) BalanceChanged(newBalance);
        }

        static int SafeSum(int a, int b)
        {
            long sum = (long)a + b;

            return sum > int.MaxValue ? int.MaxValue : (int)sum;
        }
    }
}
