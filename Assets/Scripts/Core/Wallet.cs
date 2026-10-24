using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Player credit meter. Spinning spends from here; a winning payline adds credits back.
    /// </summary>
    public class Wallet
    {
        public event Action<int> BalanceChanged;

        public int Balance { get; private set; }

        public Wallet(int startingBalance)
        {
            Balance = startingBalance;
        }

        /// <summary>
        /// Takes the bet off the balance. Returns false if the player does not have enough credits.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || amount > Balance)
                return false;

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }
    }
}
