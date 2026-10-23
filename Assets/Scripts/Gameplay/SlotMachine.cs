using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Reels;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Game controller. Flow of one spin:
    ///   1. Charge the bet.
    ///   2. Decide the outcome with the RNG (before any animation).
    ///   3. Spin all reels, then stop them one by one onto the pre-decided symbols.
    ///   4. Evaluate the payline and pay out.
    /// Deciding first keeps the game fair and testable: visuals just reveal the result.
    /// </summary>
    public class SlotMachine : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private List<SymbolData> symbols = new List<SymbolData>();
        [SerializeField] private List<Reel> reels = new List<Reel>();

        [Header("Economy")]
        [SerializeField] private int startingBalance = 1000;
        [SerializeField] private int[] betOptions = { 10, 20, 50, 100 };

        [Header("Timing")]
        [Tooltip("Seconds all reels spin before the first one starts stopping. Keep above Reel acceleration time.")]
        [SerializeField] private float minSpinTime = 1f;
        [Tooltip("Delay between each reel stopping (left to right).")]
        [SerializeField] private float stopDelayBetweenReels = 0.4f;

        private WeightedSymbolPicker picker;
        private PayoutEvaluator evaluator;
        private int betIndex;

        public Wallet Wallet { get; private set; }
        public bool IsSpinning { get; private set; }
        public int CurrentBet => betOptions[betIndex];

        public event Action SpinStarted;
        public event Action<WinResult> SpinCompleted;
        public event Action<int> BetChanged;

        private void Awake()
        {
            ValidateSetup();

            IRandomService random = new SystemRandomService();
            picker = new WeightedSymbolPicker(symbols, random);
            evaluator = new PayoutEvaluator();
            Wallet = new Wallet(startingBalance);

            foreach (Reel reel in reels)
                reel.Initialize(picker);
        }

        /// <summary>Called by the Spin button.</summary>
        public void TrySpin()
        {
            if (IsSpinning)
                return;

            if (!Wallet.TrySpend(CurrentBet))
                return; // Not enough credits.

            StartCoroutine(SpinRoutine(CurrentBet));
        }

        /// <summary>Pass +1 / -1 from the bet buttons.</summary>
        public void ChangeBet(int direction)
        {
            if (IsSpinning)
                return;

            betIndex = Mathf.Clamp(betIndex + direction, 0, betOptions.Length - 1);
            BetChanged?.Invoke(CurrentBet);
        }

        private IEnumerator SpinRoutine(int bet)
        {
            IsSpinning = true;
            SpinStarted?.Invoke();

            // 1. Decide the full outcome up front.
            SymbolData[][] outcome = GenerateOutcome();

            // 2. Start every reel spinning.
            foreach (Reel reel in reels)
                reel.BeginSpin();

            yield return new WaitForSeconds(minSpinTime);

            // 3. Stop reels left to right, staggered, then wait for all to settle.
            List<Coroutine> stopRoutines = new List<Coroutine>();
            for (int i = 0; i < reels.Count; i++)
            {
                stopRoutines.Add(StartCoroutine(reels[i].StopOn(outcome[i])));
                yield return new WaitForSeconds(stopDelayBetweenReels);
            }

            foreach (Coroutine routine in stopRoutines)
                yield return routine;

            // 4. Read the payline (middle row) and pay out.
            WinResult result = evaluator.Evaluate(GetPayline(outcome), bet);
            if (result.IsWin)
                Wallet.Add(result.Payout);

            IsSpinning = false;
            SpinCompleted?.Invoke(result);
        }

        /// <summary>Rolls a symbol for every visible cell: outcome[reel][row].</summary>
        private SymbolData[][] GenerateOutcome()
        {
            SymbolData[][] outcome = new SymbolData[reels.Count][];

            for (int r = 0; r < reels.Count; r++)
            {
                outcome[r] = new SymbolData[reels[r].VisibleRows];
                for (int row = 0; row < outcome[r].Length; row++)
                    outcome[r][row] = picker.Pick();
            }

            return outcome;
        }

        /// <summary>The payline is the middle row across all reels.</summary>
        private List<SymbolData> GetPayline(SymbolData[][] outcome)
        {
            int middleRow = reels[0].VisibleRows / 2;
            List<SymbolData> payline = new List<SymbolData>();

            foreach (SymbolData[] reelResult in outcome)
                payline.Add(reelResult[middleRow]);

            return payline;
        }

        private void ValidateSetup()
        {
            if (symbols.Count == 0 || reels.Count == 0 || betOptions.Length == 0)
                throw new InvalidOperationException("SlotMachine needs symbols, reels and bet options assigned.");

            foreach (Reel reel in reels)
            {
                if (reel.VisibleRows != reels[0].VisibleRows)
                    throw new InvalidOperationException("All reels must have the same visibleRows value.");
            }
        }
    }
}
