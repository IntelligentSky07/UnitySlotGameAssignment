using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// Result shown after the reels stop: whether the middle payline won, which symbol paid, and how many credits.
    /// </summary>
    public readonly struct WinResult
    {
        public bool IsWin { get; }
        public SymbolData WinningSymbol { get; }
        public int Payout { get; }

        public WinResult(SymbolData winningSymbol, int payout)
        {
            IsWin = true;
            WinningSymbol = winningSymbol;
            Payout = payout;
        }

        public static WinResult None => default;
    }

    /// <summary>
    /// Checks the payline after a spin.
    /// A win needs every reel on the line to show the same symbol; wilds fill in for whatever that symbol is.
    /// Payout is that symbol's multiplier times the bet that was locked in for the spin.
    /// </summary>
    public class PayoutEvaluator
    {
        public WinResult Evaluate(IReadOnlyList<SymbolData> payline, int bet)
        {
            if (payline == null || payline.Count == 0)
                return WinResult.None;

            // Ignore wilds while finding the symbol the line has to match.
            SymbolData target = null;

            foreach (SymbolData symbol in payline)
            {
                if (symbol.IsWild)
                    continue;

                if (target == null)
                    target = symbol;
                else if (target != symbol)
                    return WinResult.None;
            }

            // All wilds still counts as a win and pays using the wild's own multiplier.
            if (target == null)
                target = payline[0];

            return new WinResult(target, target.PayoutMultiplier * bet);
        }
    }
}
