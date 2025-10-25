# HavenAR - Gesture control and object dynamics

[Back - HavenAR](https://github.com/Mar7inD/XDR1-AR-Project)
[Back - EscapeRoomVR](https://github.com/Mar7inD/XDR1-VR-Project)

_Lab Week: 3_

_Written by: Martin Donchev_

# Progress

During the third week, the biggest challenge I faced was implementing gesture controls. I went through many prototypes and pre-made solutions, and although there are several gesture control assets available online, I eventually decided to build my own. Here’s how the process unfolded:

## 1. The [Scale manager](../HavenAR/Assets/Scripts/ScaleManager.cs)

The initial Scale Manager was built with a simple helper panel that included a slider, a reset button, and a text field to display the current scale. It worked fine, but it felt too basic and not very natural. It only supported object scaling, which wasn’t enough for my needs. That’s why I decided to completely rework it — and that’s how the Object Manipulator was born.

## 2. [Object Manipulator](../HavenAR/Assets/Scripts/ObjectManipulator.cs) (the child of [Scale manager](../HavenAR/Assets/Scripts/ScaleManager.cs))

The Object Manipulator taught me a lot, especially about how different keyboard and mouse input is compared to touch input on a phone.

Implementing touch gestures turned out to be quite tricky. I first needed to understand how touch input worked in Unity, so I did a bit of reverse engineering and debugging on iOS through Xcode (and learned a bit about Xcode in the process).

At one point, I ran into a strange issue — I kept detecting what I called “ghost inputs.” After some testing, I realized Unity tracks up to 10 touch inputs simultaneously, each corresponding to one of my fingers. Once that clicked, things started to make sense. My initial logic was too keyboard-and-mouse–like, so I rewrote it to handle these 10 simultaneous inputs.

Along the way, I discovered useful data properties like pressure, phase, and sometimes radius, which helped me identify active touches more accurately. I also had to manage finger sensitivity — since users can’t place or lift multiple fingers at the exact same time, I implemented debouncing and even added a four-finger touch gesture for deselection.

After nearly finishing my custom gesture system, I thought, “There’s no way no one has solved this already.” That’s when I discovered Lean Touch.

## 3. [Lean Touch](https://assetstore.unity.com/packages/tools/input-management/lean-touch-30111) approach

Lean Touch is a popular and well-optimized gesture control asset for Unity. After testing it, I was impressed by how smooth and responsive it felt. The package came with clear documentation and example scenes, making it easy to understand how each interaction worked.

One of the first issues I encountered was with object selection outlines. The default Lean Touch setup didn’t highlight selected objects properly because it assumed that meshes were merged on the parent prefab. In my project, each child retained its own mesh, so I modified the [LeanSelectableRendererColor]( ../HavenAR\Assets\Plugins\CW\LeanCommon\Extras\Scripts\LeanSelectableRendererColor.cs) script to change the color of child meshes instead. After a few challenges, I got it working — the default color now comes from each child’s original color, and the highlight color is fully customizable.

Another issue involved spawning environments and objects. I needed a script that allowed objects to stay grounded and move only along the X and Z axes. To achieve this, I created [DragOnGround](../HavenAR/Assets/Scripts/LeanTouch/DragOnGround.cs), an extension of my existing [StayOnTop](../HavenAR/Assets/Scripts/StayOnTop.cs) script. It ensures that spawned objects align with the terrain height and remain properly positioned on the surface.

## 4. Reflection

Although Lean Touch proved to be good and efficient framework, building my own system from scratch was an invaluable learning experience. It gave me a deeper understanding of touch input handling, multi-touch logic, and gesture detection in Unity. More importantly, it strengthened my ability to debug complex behaviors on mobile devices. Combining Lean Touch with my gathered knowledge helped me fit the provided solution to the specific needs of my project.

In the end, even though I transitioned to a ready-made solution, the process of creating and refining my own gesture system helped me understand better mobile controls for AR applications.

## 5. Further improvements

As of now, the AR app represents an MVP — the core interactions are functional, and users can already engage with it effectively. Although it has undergone many improvements over the past weeks, there is still room for refinement. 

The main goal of this project was to create an app where users can spawn environments, place additional objects and be able to scale whole environment or just selected objects in order to build a relaxing atmosphere. The controls are responsive, the workflow is intuitive, and elements can be arranged according to the user’s preferences.

Naturally, a few bugs remain that could be addressed in future iterations — for instance, improving ground detection when moving objects and refining the script that adjusts object height relative to the terrain. The underlying logic and structure are already in place and only minor adjustments are needed to handle edge cases more reliably.