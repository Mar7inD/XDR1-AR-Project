# Title

[Back](../README.md)

_Written by: Martin Donchev_

In this week I docused on delivering the spawning of objects on top of the environment. I faced several challenges. Some from the Rendering Pipelines which made me switch assets, some from the asset sizing and dynamic resizing. The bonfire was unable to resize based on parent so I created a scipt which looks at the parent and applies recursive resizing to each child.

Every script was created with not always assigning prefab in case of forgetting some.