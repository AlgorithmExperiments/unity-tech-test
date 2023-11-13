# Unity C# Pathfinding


Unity C# Pathfinding Implementation incorporating:
- Complete pathfinding solution utilizing an implementation of the A* algorithm
- Catmull-Rom spline-based smoothing between nodes
- PathSimplifier with radially projected hitTests to increase obstacle avoidance
- Collision Tunneling for rubust postprocessing of pathfinding
- Fully realtime dynamic obstacle detection with configurable layer filtering
- After physics collisions, tiles continuously update around moving obstacles until they halt
- If destination is unreachable (inside an obstacle), reroutes to the nearest best tile
- Resizeable grid dimensions with sliders for adjusting tile size
- Collision box height slider for adjustable obstacle overhang detection.
- In-editor debugging visualizations (toggleable Gizmos) to help illustrate and provide educational insights on the pathfinding algorithm
- Leverages optimizations using a custom PriorityDictionary which combinesconstant time O(1) dictionary lookups with O(1) fetching of the lowest F-Score tile
- Currently performs pathfinding in ~0.3 ms!


## Instructions for Running
1. Clone repo to local desktop
2. Launch Unity Hub and choose Add -> Project from Disk
4. navigate to cloned project containing Assets folder and select Add Project
5. Launch project using 2022.3.9f1 (for best results)
6. Open scene under Pathfinding/Scenes and ensure that Gizmos are toggled to ON in the upper righ corner of the game playback viewport
7. Select the 'NavGrid' gameobject and see the inspector panel to adjust the various settings during in-editor playback. Adjust the 'show debugging gizmos' toggle to control visibility of in-editor debugging visualizations that better portray the A* pathfinding algorithm.


### Features

1. **Pathfinding Implementation**: Develop a custom pathfinding system without using Unity's NavMesh system. You are free to choose any pathfinding algorithm you prefer, such as A* or Dijkstra's algorithm.

3. **2D Grid**: Implement the pathfinding on a 2D grid where each cell represents either an impassable obstacle or a clear area. You have the flexibility to determine how this grid data is populated. Options include creating an editor tool to author it, automatically generating it from scene geometry, procedural generation, or any other method of your choice.

4. **Path Smoothing**: Pathfinding on a 2D grid can result in stair-stepped paths that look unnatural when followed directly. Your character should use a method to smooth out the path following to appear more natural, similar to how a human would move.

## Getting Started
To begin the project, follow these steps:

1. Clone this repository to your local machine.

2. Create a Unity project or use an existing one.

3. Build the custom pathfinding system within your Unity project according to the project requirements outlined above.

4. Implement player control, allowing the character to follow the calculated path when the player clicks on the screen.

5. Ensure the path follows a natural-looking trajectory by implementing path smoothing.

6. Test your project thoroughly to ensure it meets the specified requirements.

7. Document your code, providing clear comments and explanations of your implementation choices.

8. Beyond this, feel free to add whatever other bells and whistles to the project you'd like. This is your opportunity to show off what you can do.

## Submission Guidelines
When you have completed the project, please follow these guidelines for submission:

1. Commit and push your code to your GitHub repository.

2. Update this README with any additional instructions, notes, or explanations regarding your implementation, if necessary.

3. Provide clear instructions on how to run and test your project.

4. Share the repository URL with us!

## Additional Information

Feel free to be creative in how you approach this project. Your solution will be evaluated based on code quality, efficiency, and how well it meets the specified requirements.

Good luck, and we look forward to seeing your Unity pathfinding project! If you have any questions or need clarifications, please reach out to us.
