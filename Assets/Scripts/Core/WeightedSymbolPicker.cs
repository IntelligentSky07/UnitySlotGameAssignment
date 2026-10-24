using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// Rolls which symbol lands on a reel cell.
    /// Each SymbolData.Weight is a slice of the total: a symbol with weight 2 in a pool of 22
    /// shows up about 2/22 of the time, so rares stay rare and still pay more.
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
            // Walk the symbol list like a weighted wheel: subtract each weight until the roll lands in a slice.
            int roll = random.Range(0, totalWeight);

            foreach (SymbolData symbol in symbols)
            {
                if (roll < symbol.Weight)
                    return symbol;
                roll -= symbol.Weight;
            }

            return symbols[symbols.Count - 1];
        }
    }
}
