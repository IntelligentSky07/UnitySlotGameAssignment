using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>Outcome of evaluating the payline.</summary>
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
    /// Pure logic (no Unity dependencies) that decides if a payline wins and how much.
    /// Rule: every symbol on the line must match. Wild symbols match anything.
    /// </summary>
    public class PayoutEvaluator
    {
        public WinResult Evaluate(IReadOnlyList<SymbolData> payline, int bet)
        {
            if (payline == null || payline.Count == 0)
                return WinResult.None;

            // Find the first non-wild symbol; every other non-wild must equal it.
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

            // A line made entirely of wilds pays as the wild symbol itself.
            if (target == null)
                target = payline[0];

            return new WinResult(target, target.PayoutMultiplier * bet);
        }
    }
}
