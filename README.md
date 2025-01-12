## ****Key Features****

**Tile Matching / Collapse**

  Player taps a group of 2+ same-colored blocks to remove them.
  Blocks above fall down, newly spawned blocks arrive from the top.

**Deadlock Detection**
  We detect whether there is any connected component of at least size 2. If not, it’s a deadlock.
  
**One-Pass Shuffle (No Blind Repeats)**

  When deadlock is detected, we collect all colorIDs from the board.
  Perform one Fisher–Yates shuffle.
  Ensure at least one pair of same color are adjacent (e.g., index 0 and 1) to guarantee a new match.
  Reassign colorIDs in a single pass, fade out/in for a shuffle animation.

**Group Size & Thresholds**

  Each block has different icons based on its group size.
  Three thresholds (A, B, C) define how sprites change for bigger groups.

**Performance Considerations**

  Single BFS per connected region for group detection (no repeated expansions).
  One shuffle pass on deadlock (not multiple attempts).
  Object pooling or optional if you want to reduce instantiation cost (not mandatory but recommended).
  Color assignment and sprite updates happen after a short fade animation, so the CPU overhead remains low.

## **How to Use**

  Open the project in Unity 2021+ (or later).
  Press Play to generate the board.
  Tap/Click on any group (>=2 blocks) to blast them.
  If a deadlock occurs, the game auto-shuffles once (Fisher–Yates) to ensure at least one valid match.









**This is the case-study project for the Good Job Games studios.**
