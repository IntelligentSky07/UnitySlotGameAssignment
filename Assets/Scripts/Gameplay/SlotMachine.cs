using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Core;
using SlotGame.Reels;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Runs a spin on the cabinet:
    /// take the bet, roll every visible cell, spin the reels, stop them left to right on those cells,
    /// then score the middle row and pay if it matches.
    /// The pictures on the reels always show this pre-rolled grid; they do not pick the win themselves.
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
        [Tooltip("How long every reel keeps spinning before the leftmost one begins to stop.")]
        [SerializeField] private float minSpinTime = 1f;
        [Tooltip("Pause between each reel locking, left to right.")]
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

        /// <summary>
        /// Spin button. Does nothing if a spin is already playing or the player cannot cover the bet.
        /// </summary>
        public void TrySpin()
        {
            if (IsSpinning)
                return;

            if (!Wallet.TrySpend(CurrentBet))
                return;

            StartCoroutine(SpinRoutine(CurrentBet));
        }

        /// <summary>
        /// Steps through betOptions. +1 is the next higher stake, -1 the next lower.
        /// </summary>
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

            // Roll the full 3-row grid before anything moves, then reveal it on the reels.
            SymbolData[][] outcome = GenerateOutcome();

            foreach (Reel reel in reels)
                reel.BeginSpin();

            yield return new WaitForSeconds(minSpinTime);

            // Lock reels one at a time so the player can watch each column settle.
            List<Coroutine> stopRoutines = new List<Coroutine>();
            for (int i = 0; i < reels.Count; i++)
            {
                stopRoutines.Add(StartCoroutine(reels[i].StopOn(outcome[i])));
                yield return new WaitForSeconds(stopDelayBetweenReels);
            }

            foreach (Coroutine routine in stopRoutines)
                yield return routine;

            // Middle row across the reels is the payline.
            WinResult result = evaluator.Evaluate(GetPayline(outcome), bet);
            if (result.IsWin)
                Wallet.Add(result.Payout);

            IsSpinning = false;
            SpinCompleted?.Invoke(result);
        }

        /// <summary>
        /// Picks a symbol for every visible cell. outcome[reel][row] is what that reel will show, top to bottom.
        /// </summary>
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

        /// <summary>
        /// Reads the centre row of each reel — that is the only line that pays in this game.
        /// </summary>
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
