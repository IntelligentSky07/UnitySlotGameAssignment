using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// A scriptable object that contains the data for a symbol in the slot machine.
    /// </summary>
    [CreateAssetMenu(fileName = "Symbol_", menuName = "SlotGame/Symbol Data")]
    public class SymbolData : ScriptableObject
    {
        [SerializeField] private string symbolName;
        [SerializeField] private Sprite sprite;

        [Tooltip("Win amount = bet x this multiplier when a full line of this symbol lands.")]
        [SerializeField, Min(1)] private int payoutMultiplier = 5;

        [Tooltip("Relative chance of appearing. Higher = more common. Rare symbols pay more.")]
        [SerializeField, Min(0)] private int weight = 10;

        [Tooltip("Bonus feature: a wild substitutes for any other symbol on the payline.")]
        [SerializeField] private bool isWild;

        public string SymbolName => symbolName;
        public Sprite Sprite => sprite;
        public int PayoutMultiplier => payoutMultiplier;
        public int Weight => weight;
        public bool IsWild => isWild;
    }
}
