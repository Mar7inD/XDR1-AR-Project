# HavenAR - Ground work

[Back](../README.md)

_Lab Week: 2_


_Written by: Martin Donchev_

# Progress

By the end of the first week and into the second, my focus was on building the core interactions for the AR app. I started with a prototype that allowed users to spawn environments on a detected plane. Users could open a menu, select an environment, drag it onto the surface, and scale it to fit different spaces. While it worked, the prototype quickly revealed areas that needed refinement—and plenty of opportunities to learn.

## Script Organization

Initially, I had multiple scripts handling overlapping tasks, which started to feel messy. To simplify things, I consolidated everything into a single AR Placement Manager. This script now handles drag-and-drop for both the environment land and placeable objects, and I added a tagging system to distinguish between them.

This restructuring not only made the scripts cleaner and more robust, but also laid the foundation for future features, like dynamic object spawning and more complex interactions.

## Scaling Objects

One of the trickiest parts was scaling, especially for complex assets like the bonfire fire effect. I learned to use mesh colliders, which allowed me to use the prefabs themselves to create more accurate colliders. This made placement smoother, though scaling remained inconsistent for some asset store models.

After trial and error, I built a new Scale Manager to allow users to scale objects individually via touch. Handling parent-child relationships was a challenge at first, but a small ParentIdentifier script solved the issue, keeping child objects aligned with their parent while staying on top of the environment.

## Handling Assets and Rendering Challenges

There were a few other surprises along the way:

- Some assets didn’t play well with the rendering pipeline, which required switching to alternative versions.
- The bonfire needed recursive resizing for its child objects, but the first approach added too much complexity, so I adjusted the asset instead.
- Shader behavior differed between computer and mobile, especially for outlining selected objects. After debugging, I found a more robust solution that worked across platforms.

Every script was written with fallback procedures, so the system remained stable even if a prefab was missing or misconfigured.

## Key Learnings

This period taught me a lot about AR development:

- How to organize and consolidate scripts efficiently
- Touch-based object manipulation and scaling
- Troubleshooting rendering pipelines and asset inconsistencies
- Debugging cross-platform input and shader behavior

Most importantly, every challenge became an opportunity to deepen my understanding of Unity, AR interactions, and user experience design.
