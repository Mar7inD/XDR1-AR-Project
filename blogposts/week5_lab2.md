# HavenAR - Progress and Challenges

[Back](../README.md)

_Written by: Martin Donchev_

## Progress

By the end of the first week and throughout the second, I focused on building the core interactions for the app. Overall, the process has been moving according to plan, though I did face some challenges along the way.

During week one, I developed a working prototype that allowed the user to spawn an environment on a detected plane. Users could open a menu, select an environment, and drag it onto the surface. I also implemented scaling so the environment could fit different spaces. While the solution worked, it quickly became messy—progress brought many new ideas, but also too many scripts, some of which overlapped or did redundant work.

## Challenges

### Script overload
Managing multiple scripts became a problem, so I needed to clean up and simplify. I consolidated everything into one main script, the AR Placement Manager, which now handles drag-and-drop functionality for both land and objects. I also introduced a tag system to distinguish between environment land and placeable objects (e.g., a fire on the ground). This restructuring not only made the scripts more robust but also opened the door for extended functionality, such as object spawning.

### Scaling difficulties
One of the trickiest parts was scaling objects—especially the bonfire fire effect. I learned to use mesh colliders, which let me take advantage of existing prefab meshes for more accurate colliders. This made placement smoother, but scaling remained inconsistent due to how some asset store models were created. After a lot of trial and error with different assets (and sometimes pipeline-related issues), I finally found a reliable scaling solution.

To make scaling more flexible, I replaced the original Environment Scale Controller with a new Scale Manager. Now, users can select and scale individual objects via touch. Handling parent prefabs caused some issues at first, but with the help of a small ParentIdentifier script, I managed to resolve them successfully.

Have to make objects stay on top of the land.

In this week I docused on delivering the spawning of objects on top of the environment. I faced several challenges. Some from the Rendering Pipelines which made me switch assets, some from the asset sizing and dynamic resizing. The bonfire was unable to resize based on parent so I created a scipt which looks at the parent and applies recursive resizing to each child.




Every script was created with not always assigning prefab in case of forgetting some.

