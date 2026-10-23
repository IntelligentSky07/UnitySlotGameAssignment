using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// Picks symbols randomly, respecting each symbol's weight.
    /// Example: weights 10/10/2 => the third symbol shows up 2 out of 22 times.
    /// </summary>
    public class WeightedSymbolPicker
    {
        private readonly IReadOnlyList<SymbolData> symbols;
        private readonly IRandomService random;
        private readonly int totalWeight;

        public WeightedSymbolPicker(IReadOnlyList<SymbolData> symbols, IRandomService random)
        {
            this.symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
            this.random = random ?? throw new ArgumentNullException(nameof(random));

            foreach (SymbolData symbol in symbols)
                totalWeight += symbol.Weight;

            if (totalWeight <= 0)
                throw new ArgumentException("Symbols must have a total weight greater than zero.");
        }

        public SymbolData Pick()
        {
            // Roll a number across the total weight, then walk the list subtracting
            // weights until we find the bucket the roll fell into.
            int roll = random.Range(0, totalWeight);

            foreach (SymbolData symbol in symbols)
            {
                if (roll < symbol.Weight)
                    return symbol;
                roll -= symbol.Weight;
            }

            return symbols[symbols.Count - 1]; // Unreachable, keeps the compiler happy.
        }
    }
}
