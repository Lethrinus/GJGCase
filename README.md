


**Overview**

The primary mechanic involves the player tapping on a group of at least 2 adjacent blocks of the same color, causing them to be removed.
Blocks above then fall down to fill the empty space, and new blocks enter from above if necessary. 
I also implemented multiple icon sprites depending on group size (small, medium, large, extra large) and a non‐random “smart shuffle” feature to handle potential deadlocks.

**Tools And Packages**

Unity 2021+

I used Unity for the entire development process, taking advantage of its 2D workflow and built‐in physics for simpler block dropping.

**C#**

All game logic is written in C# scripts, organized by features (BoardManager, DeadlockResolver, InputHandler, etc.).

**DOTween**

This is a popular tweening engine in Unity. I used DOTween for smooth animations like blocks falling, fading in/out, and scale changes (for both feedback and shuffle animations). 
It helps keep the code tidy and allows for easy control over easing functions and durations.

Run the game via MainMenu. 
For best visual and proper positioning to prefabs use with Full HD 1920x1080 mode and set the scale to the lowest.
 






**3. Key Systems and Methods

A. Board Data and Initialization**

The BoardData class holds information like the number of rows, columns, and the current array of blocks.
On startup, the BoardManager script spawns initial blocks (one per valid cell), assigning each a random color ID from the available color pool (1 to 6 colors).
Each block has references to threshold values (for switching the icon sprite based on group size).

**B. Group Matching**


I used a Depth‐First Search (DFS) or Stack‐based approach to find all adjacent blocks (up, down, left, right) of the same color:
When the player taps a block, I locate its index within the board.
I explore all neighbors of the same color.
If 2 or more connected blocks are found, they form a valid group to be removed.

![](https://github.com/Lethrinus/GJGCase/blob/main/Documentation/document1.png)
![](https://github.com/Lethrinus/GJGCase/blob/main/Documentation/document2.png)

**C. Icon Switching Based on Group Size**

Each block holds multiple sprite references:
Default icon 
First threshold icon (e.g., if group size > A)
Second threshold icon (e.g., if group size > B)
Third threshold icon (e.g., if group size > C)
Once the group size is determined, each block in that group updates its icon accordingly.

![](https://github.com/Lethrinus/GJGCase/blob/main/Documentation/animationmatch.gif)

**D. Removal and Block Falling**

 When a valid group is removed, I clear those blocks and allow blocks above to fall down:
 I reorganize the board from top to bottom within each column.
 If new blocks are needed, they are spawned above the board and dropped into place.
 DOTween animates the movement, scaling, and fading of blocks.
 

**E. Deadlock Detection and Shuffle**

 A Deadlock occurs if there are no groups of 2 or more adjacent same‐colored blocks.
 My DeadlockResolver class checks each block and performs a quick adjacency check to see if any potential group exists.
 If no groups exist, a single smart shuffle routine rearranges the colors in the board (rather than randomizing multiple times). It ensures at least one match.

![](https://github.com/Lethrinus/GJGCase/blob/main/Documentation/deadlock.gif)
                                                                                							         
**4. Performance Considerations**

•	Pooling

I used BlockPool (and optionally a ParticlePool) to avoid excessive Instantiate/Destroy calls. When blocks are removed, they’re returned to a pool, then reused later. 

![image](https://github.com/user-attachments/assets/0fa209fe-ae23-4c68-ab92-417bf742b1bc)
                                                    
•	 DOTween TweensCapacity

In Awake(), I call DOTween.SetTweensCapacity(...) to reduce memory allocation overhead for tween animations.

•	ScriptableObject Usage

Each level has its own unique config file which has separate settings for everything in the levels. 
 
![image](https://github.com/user-attachments/assets/2ef75b54-e456-4241-a1be-fc9f27020f7f)



**1st Level has 8x8 classic rectangular table and a basic goal.**


 ![image](https://github.com/user-attachments/assets/896d580a-47f6-463a-a186-69272745c866)
 
**2nd Level has plus sign shape and basic goal i did the shape with using cell mask logic.**


 ![image](https://github.com/user-attachments/assets/b7df7114-9315-4fa0-9e20-a01412197bd5)
 
**3rd Level has crate mechanic which blasts the crates when a match happen near them.**


 ![image](https://github.com/user-attachments/assets/3a8b89db-3bb5-4110-9f68-00db0247c3e8)
 

