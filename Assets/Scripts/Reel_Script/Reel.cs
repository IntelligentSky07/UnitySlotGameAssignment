using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SlotGame.Core;

namespace SlotGame.Reels
{
    /// <summary>
    /// One vertical reel. It only handles VISUALS: the result is decided beforehand
    /// by SlotMachine and handed to StopOn(), so animation can never change the outcome.
    ///
    /// How it works: a small strip of Image cells scrolls downward. Whenever the strip
    /// has moved one symbol-height, every symbol shifts down one slot and a new symbol
    /// enters at the top (a "wheel" illusion with only visibleRows + 2 images).
    ///
    /// Cell indexes:  0 = hidden buffer above the window
    ///                1..visibleRows = visible rows (top to bottom)
    ///                last = hidden buffer below the window
    /// </summary>
    public class Reel : MonoBehaviour
    {
        private enum State { Idle, Spinning, Landing }

        [Header("Layout")]
        [Tooltip("Parent of the symbol images. Add a RectMask2D to it so overflow is hidden.")]
        [SerializeField] private RectTransform symbolContainer;
        [Tooltip("Prefab containing just an Image component.")]
        [SerializeField] private Image symbolImagePrefab;
        [SerializeField] private float symbolHeight = 150f;
        [SerializeField, Range(1, 5)] private int visibleRows = 3;

        [Header("Motion")]
        [Tooltip("Pixels per second at full spin speed.")]
        [SerializeField] private float maxSpinSpeed = 2200f;
        [Tooltip("Seconds to reach full speed from standstill.")]
        [SerializeField] private float accelerationTime = 0.25f;
        [Tooltip("How far (pixels) the reel dips past its final position before settling.")]
        [SerializeField] private float bounceDistance = 18f;
        [SerializeField] private float bounceDuration = 0.2f;

        private const int BufferCells = 2; // One above + one below the visible window.

        private Image[] cellImages;
        private SymbolData[] cellSymbols;
        private readonly Queue<SymbolData> forcedSymbols = new Queue<SymbolData>();
        private WeightedSymbolPicker filler;

        private State state = State.Idle;
        private float scroll;        // 0..symbolHeight progress toward the next shift.
        private float currentSpeed;  // Pixels per second.
        private float bounceOffset;  // Visual-only dip applied when landing.
        private int shiftsSinceStop;

        public int VisibleRows => visibleRows;

        /// <summary>Builds the cell images and fills them with random symbols.</summary>
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

                // Stretch horizontally, anchor to the top so y=0 means "top of window".
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

        /// <summary>Starts spinning (accelerates up to full speed).</summary>
        public void BeginSpin()
        {
            if (state != State.Idle)
                return;

            currentSpeed = 0f;
            bounceOffset = 0f;
            state = State.Spinning;
        }

        /// <summary>
        /// Smoothly decelerates and lands so the visible rows show exactly
        /// targetsTopToBottom. Yield on this to wait until the reel has settled.
        /// </summary>
        public IEnumerator StopOn(IReadOnlyList<SymbolData> targetsTopToBottom)
        {
            if (state != State.Spinning)
                yield break;

            if (targetsTopToBottom.Count != visibleRows)
                throw new ArgumentException("Target count must equal visibleRows.");

            state = State.Landing;

            // Symbols enter from the top and slide down, so the BOTTOM row must be
            // fed first. After visibleRows + 1 shifts the targets sit in rows 1..N
            // and one random "buffer" symbol sits just above the window.
            for (int row = visibleRows - 1; row >= 0; row--)
                forcedSymbols.Enqueue(targetsTopToBottom[row]);

            int shiftsNeeded = visibleRows + 1;
            shiftsSinceStop = 0;

            // Total distance to travel so the last shift lands exactly on scroll = 0.
            float distance = shiftsNeeded * symbolHeight - scroll;

            // Ease-out cubic starts at a speed of 3 * distance / duration. Choosing
            // duration = 3 * distance / currentSpeed makes the slowdown start at the
            // exact speed we were already spinning at, so there is no visible jolt.
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

            // Safety net against floating point drift: guarantee all shifts happened.
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

        /// <summary>Moves the strip down by delta pixels, shifting symbols when needed.</summary>
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

        /// <summary>Every symbol moves down one slot; a new one enters at the top.</summary>
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
                float y = -(i - 1) * symbolHeight - scroll - bounceOffset;
                cellImages[i].rectTransform.anchoredPosition = new Vector2(0f, y);
            }
        }

        /// <summary>Small downward dip then settle, like a physical reel locking in.</summary>
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
