using System;

namespace SlotGame.Core
{
    /// <summary>Holds the player's credits and notifies listeners when they change.</summary>
    public class Wallet
    {
        public event Action<int> BalanceChanged;

        public int Balance { get; private set; }

        public Wallet(int startingBalance)
        {
            Balance = startingBalance;
        }

        /// <summary>Deducts credits if the player can afford it.</summary>
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
