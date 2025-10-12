# HavenAR - Gesture control and object dynamics

[Back](../README.md)

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

Lean Touch is a widely used gesture control asset for Unity, and after trying it out, I immediately noticed how smooth and well-optimized it was. The package includes several example scenes with clear documentation, making it easy to understand how each interaction works.

While Lean Touch performed better in terms of responsiveness and polish, I ultimately decided not to switch to it. My custom Object Manipulator already had built-in functionality that integrated deeply with my project. For example, it supported real-time updates to object positions as the environment changed, and worked alongside my [StayOnTop](../HavenAR/Assets/Scripts/StayOnTop.cs) script — which ensures that objects remain properly positioned on land surfaces and don’t sink below them.

In the end, while Lean Touch was very good, developing my own gesture system gave me a much deeper understanding of input handling, debugging mobile interactions, and balancing between custom control and existing frameworks. It also made the interactions in my project feel uniquely mine — tailored exactly to how I want the AR experience to behave.


