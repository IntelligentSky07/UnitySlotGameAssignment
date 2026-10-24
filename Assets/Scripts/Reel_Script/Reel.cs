using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// One vertical strip of symbols on the cabinet.
    /// SlotMachine already knows what this reel will land on; this class only scrolls the pictures
    /// so the player sees them drop into place.
    ///
    /// A short stack of Image cells moves down. Each time the stack travels one symbol height,
    /// symbols shuffle down a slot and a new one enters at the top — a looping reel with only
    /// the visible rows plus a hidden cell above and below the window.
    ///
    /// Cell 0 sits just above the mask, cells 1..visibleRows are what the player sees (top to bottom),
    /// and the last cell sits just below the mask.
    /// </summary>
    public class Reel : MonoBehaviour
    {
        private enum State { Idle, Spinning, Landing }

        [Header("Layout")]
        [Tooltip("Holds the symbol images. Put a RectMask2D on it so cells outside the window are clipped.")]
        [SerializeField] private RectTransform symbolContainer;
        [Tooltip("Prefab with an Image used for each cell on this reel.")]
        [SerializeField] private Image symbolImagePrefab;
        [SerializeField] private float symbolHeight = 150f;
        [SerializeField, Range(1, 5)] private int visibleRows = 3;

        [Header("Motion")]
        [Tooltip("How fast the strip scrolls while spinning (pixels per second).")]
        [SerializeField] private float maxSpinSpeed = 2200f;
        [Tooltip("Time to ramp from a standstill up to full spin speed.")]
        [SerializeField] private float accelerationTime = 0.25f;
        [Tooltip("How far the reel overshoots its stop before bouncing back into place.")]
        [SerializeField] private float bounceDistance = 18f;
        [SerializeField] private float bounceDuration = 0.2f;

        private const int BufferCells = 2; // Hidden cell above the window and one below it.

        private Image[] cellImages;
        private SymbolData[] cellSymbols;
        private readonly Queue<SymbolData> forcedSymbols = new Queue<SymbolData>();
        private WeightedSymbolPicker filler;

        private State state = State.Idle;
        private float scroll;        // Progress toward the next full-symbol shift (0 to symbolHeight).
        private float currentSpeed;  // Current scroll speed in pixels per second.
        private float bounceOffset;  // Extra downward dip only used on the landing bounce.
        private int shiftsSinceStop;

        public int VisibleRows => visibleRows;

        [Tooltip("Shifts all symbols vertically. Negative = down, positive = up.")]
        [SerializeField] private float verticalOffset = 0f;

        /// <summary>
        /// Builds the cell images and fills the reel with a random starting strip so it is not empty at boot.
        /// </summary>
        public void Initialize(WeightedSymbolPicker symbolFiller)
        {
            filler = symbolFiller;

            int cellCount = visibleRows + BufferCells;
            cellImages = new Image[cellCount];
            cellSymbols = new SymbolData[cellCount];

            symbolContainer.sizeDelta = new Vector2(symbolContainer.sizeDelta.x, visibleRows * symbolHeight);

            for (int i = 0; i < cellCount; i++)
            {
                Image image = Instantiate(symbolImagePrefab, symbolContainer);
                RectTransform rect = image.rectTransform;

                // Pin each cell to the top of the window and stretch it across the reel width.
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(0f, symbolHeight);

                image.preserveAspect = true;
                cellImages[i] = image;
                cellSymbols[i] = filler.Pick();
            }

            RefreshSprites();
            ApplyLayout();
        }

        /// <summary>
        /// Starts the reel blurring downward. Call StopOn later with the symbols that should land in the window.
        /// </summary>
        public void BeginSpin()
        {
            if (state != State.Idle)
                return;

            currentSpeed = 0f;
            bounceOffset = 0f;
            state = State.Spinning;
        }

        /// <summary>
        /// Slows the reel and feeds in the chosen symbols from the bottom row up so they sit
        /// in the visible window when motion stops. Yield until the bounce has finished.
        /// </summary>
        public IEnumerator StopOn(IReadOnlyList<SymbolData> targetsTopToBottom)
        {
            if (state != State.Spinning)
                yield break;

            if (targetsTopToBottom.Count != visibleRows)
                throw new ArgumentException("Target count must equal visibleRows.");

            state = State.Landing;

            // New symbols enter at the top and travel down, so enqueue the bottom row first.
            // After visibleRows + 1 shifts, the chosen symbols occupy the window and a filler
            // symbol sits in the hidden cell above.
            for (int row = visibleRows - 1; row >= 0; row--)
                forcedSymbols.Enqueue(targetsTopToBottom[row]);

            int shiftsNeeded = visibleRows + 1;
            shiftsSinceStop = 0;

            // Remaining travel so the last shift lands with scroll exactly at 0 (symbols aligned in the window).
            float distance = shiftsNeeded * symbolHeight - scroll;

            // Ease-out cubic begins at 3 × distance / duration. Matching that to the current spin speed
            // keeps the slowdown from popping when the reel starts to stop.
            float duration = 3f * distance / Mathf.Max(currentSpeed, 300f);
            float travelled = 0f;

            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - t / duration, 3f);
                float targetTravel = distance * eased;
                Advance(targetTravel - travelled);
                travelled = targetTravel;
                yield return null;
            }

            Advance(distance - travelled);

            while (shiftsSinceStop < shiftsNeeded)
                ShiftDown();

            scroll = 0f;
            ApplyLayout();

            yield return Bounce();
            state = State.Idle;
        }

        private void Update()
        {
            if (state != State.Spinning)
                return;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed, maxSpinSpeed, maxSpinSpeed / accelerationTime * Time.deltaTime);

            Advance(currentSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Scrolls the strip down. Whenever it has moved a full symbol height, shuffle the cells.
        /// </summary>
        private void Advance(float delta)
        {
            scroll += delta;

            while (scroll >= symbolHeight)
            {
                scroll -= symbolHeight;
                ShiftDown();
            }

            ApplyLayout();
        }

        /// <summary>
        /// Moves every symbol down one cell and places a new symbol in the top buffer
        /// (the next landing symbol if we are stopping, otherwise a random filler).
        /// </summary>
        private void ShiftDown()
        {
            for (int i = cellSymbols.Length - 1; i > 0; i--)
                cellSymbols[i] = cellSymbols[i - 1];

            cellSymbols[0] = forcedSymbols.Count > 0 ? forcedSymbols.Dequeue() : filler.Pick();
            shiftsSinceStop++;
            RefreshSprites();
        }

        private void RefreshSprites()
        {
            for (int i = 0; i < cellImages.Length; i++)
                cellImages[i].sprite = cellSymbols[i].Sprite;
        }

        private void ApplyLayout()
        {
            for (int i = 0; i < cellImages.Length; i++)
            {
                float y = -(i - 1) * symbolHeight - scroll - bounceOffset + verticalOffset;
                cellImages[i].rectTransform.anchoredPosition = new Vector2(0f, y);
            }
        }

        /// <summary>
        /// Short overshoot then settle, so the reel feels like it clicks into the payline.
        /// </summary>
        private IEnumerator Bounce()
        {
            for (float t = 0f; t < bounceDuration; t += Time.deltaTime)
            {
                bounceOffset = Mathf.Sin(t / bounceDuration * Mathf.PI) * bounceDistance;
                ApplyLayout();
                yield return null;
            }

            bounceOffset = 0f;
            ApplyLayout();
        }
    }
}
