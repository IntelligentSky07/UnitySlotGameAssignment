# Unity Slot Game

A 3-reel slot machine I made in Unity for the assignment. You press Spin, the lever gets pulled, the reels spin and stop one at a time, and if all three symbols on the middle row match, you win.

Play it here: https://intelligentsky07.itch.io/slot-machine-game

## How the game works

There are four symbols: Seven, Bar, Bell and Cherry. You start with 1000 credits and can set your bet to 10, 20, 50 or 100 using the + and - buttons. If all three reels land on the same symbol, you win your bet multiplied by that symbol's multiplier.

Rarer symbols pay more:

| Symbol | Weight | Multiplier |
|--------|--------|------------|
| Cherry | 50     | 4x         |
| Bell   | 25     | 15x        |
| Bar    | 15     | 40x        |
| Seven  | 10     | 100x       |

Weight is just how often a symbol shows up. I worked out the return-to-player from these numbers and it comes to about 97%, which is close to what real slot machines aim for. The weights and multipliers live in `SymbolData` assets, so they can be changed in the Inspector without touching any code.

## Running the WebGL build

The easiest way is the itch.io link above.

If you want to run it from the repo, the build is in the `Build` folder. WebGL builds don't work if you just double click `index.html`, so you need a small local server:

1. Open a terminal in the `Build` folder (the one with `index.html` in it)
2. Run `python -m http.server 8000`
3. Go to `http://localhost:8000` in your browser

You can also open the project in Unity, load the scene from `Assets/Scenes` and hit Play.

## Extras I added

- The lever animates when you press Spin
- Reels speed up, slow down gradually, and bounce a little when they land
- Reels stop one after another, left to right
- Bet selector and a live balance
- Messages for wins and for running out of credits

## How I approached it

The main thing I wanted was for the result to be fair, so the random result for each spin is decided before the reels even start moving. The reels then just animate towards that result. That way the animation can't affect the outcome, and it keeps the game logic separate from the visuals.

For the randomness, each symbol has a weight and a picker chooses symbols based on those weights, so common symbols come up more and rare ones less.

I tried to keep the code split into small classes that each do one thing:

- `SymbolData` holds the sprite, weight and payout for a symbol (a ScriptableObject)
- `WeightedSymbolPicker` and `RandomService` handle picking random symbols
- `PayoutEvaluator` checks if a spin is a win and how much it pays
- `Wallet` keeps track of the balance
- `Reel` handles the spinning, slowing down and landing animation
- `SlotMachine` runs the spin from start to finish
- `SlotUI` updates the buttons and text
- `LeverAnimation` swaps the lever sprite when a spin starts

The UI and the lever don't talk to each other directly. They both just listen for the `SpinStarted` and `SpinCompleted` events from `SlotMachine`, which kept things easy to change as I went.
