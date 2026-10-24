using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;
using SlotGame.Gameplay;

namespace SlotGame.UI
{
    /// <summary>
    /// HUD for the slot cabinet: Spin / bet buttons, credit display, and the win / lose message.
    /// Buttons stay locked while the reels are moving or the player cannot cover the current bet.
    /// </summary>
    public class SlotUI : MonoBehaviour
    {
        [SerializeField] private SlotMachine machine;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button betDownButton;

        [Header("Texts")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private TMP_Text messageText;

        // Start runs after SlotMachine.Awake, so the wallet already exists when we subscribe.
        private void Start()
        {
            spinButton.onClick.AddListener(machine.TrySpin);
            betUpButton.onClick.AddListener(() => machine.ChangeBet(+1));
            betDownButton.onClick.AddListener(() => machine.ChangeBet(-1));

            machine.Wallet.BalanceChanged += OnBalanceChanged;
            machine.BetChanged += OnBetChanged;
            machine.SpinStarted += OnSpinStarted;
            machine.SpinCompleted += OnSpinCompleted;

            OnBalanceChanged(machine.Wallet.Balance);
            OnBetChanged(machine.CurrentBet);
            messageText.text = "Press SPIN!";
        }

        private void OnDestroy()
        {
            if (machine == null)
                return;

            machine.BetChanged -= OnBetChanged;
            machine.SpinStarted -= OnSpinStarted;
            machine.SpinCompleted -= OnSpinCompleted;
            if (machine.Wallet != null)
                machine.Wallet.BalanceChanged -= OnBalanceChanged;
        }

        private void OnBalanceChanged(int balance)
        {
            balanceText.text = $"Balance: {balance}";
            RefreshButtons();
        }

        private void OnBetChanged(int bet)
        {
            betText.text = $"Bet: {bet}";
            RefreshButtons();
        }

        private void OnSpinStarted()
        {
            messageText.text = "Spinning...";
            RefreshButtons();
        }

        private void OnSpinCompleted(WinResult result)
        {
            if (result.IsWin)
                messageText.text = $"WIN! {result.WinningSymbol.SymbolName} pays +{result.Payout}";
            else
                messageText.text = machine.Wallet.Balance < machine.CurrentBet ? "Out of credits!" : "No win. Try again!";

            RefreshButtons();
        }

        private void RefreshButtons()
        {
            bool canSpin = !machine.IsSpinning && machine.Wallet.Balance >= machine.CurrentBet;
            spinButton.interactable = canSpin;
            betUpButton.interactable = !machine.IsSpinning;
            betDownButton.interactable = !machine.IsSpinning;
        }
    }
}
