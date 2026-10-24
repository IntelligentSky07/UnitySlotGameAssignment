using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// One symbol that can land on a reel (Cherry, BAR, Wild, and so on).
    /// Weight controls how often it appears; payoutMultiplier is how many times the bet it pays on a full line.
    /// </summary>
    [CreateAssetMenu(fileName = "Symbol_", menuName = "SlotGame/Symbol Data")]
    public class SymbolData : ScriptableObject
    {
        [SerializeField] private string symbolName;
        [SerializeField] private Sprite sprite;

        [Tooltip("Credits paid = current bet × this value when this symbol fills the payline.")]
        [SerializeField, Min(1)] private int payoutMultiplier = 5;

        [Tooltip("How often this symbol is rolled. Higher weight = more common on the reels.")]
        [SerializeField, Min(0)] private int weight = 10;

        [Tooltip("Wilds count as a match for any other symbol on the payline.")]
        [SerializeField] private bool isWild;

        public string SymbolName => symbolName;
        public Sprite Sprite => sprite;
        public int PayoutMultiplier => payoutMultiplier;
        public int Weight => weight;
        public bool IsWild => isWild;
    }
}
