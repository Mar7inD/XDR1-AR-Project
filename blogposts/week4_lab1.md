# HavenAR - Research, decisions and intial work

[Back](../README.md)

_Lab Week: 1_

_Written by: Martin Donchev_

## Introduction

HeavenAR is an app with purpose to let users escape into a calming environment using only their phone and headphones. In our busy daily lives, finding time to relax can be difficult, which often leads to stress. HeavenAR aims to offer a quick mental getaway — an app where you can project a peaceful scene onto a nearby surface like a table or floor. The main goal of the app is to be able with a few clicks to immerse into a soothing environment where you can disconnect and relax for a few moments.

## Progress

### Ideation

The final concept emerged several phases of ideation and refinement. It all began with the idea and motivation behind using AR to project an object and being able to control that object through finger gestures. 

My first thought was to create a sorting app where AR could help users place trash into the correct bins. However, I realized that simpler non-XR solution already exist in Denmark, with labels on packaging that clearly indicate the appropriate recycling bin.

From there, I considered developing an AR project using 3D models to enhance understanding, but I found that many such implementations already existed online, and even our teacher showed us student examples. This motivated me to go with something more unique, so I set aside my ideas related to waste sorting, biology, the solar system.

While brainstorming around the solar system idea and more specifically the portals I was planing to include, I started thinking about where should a portal to Earth lead? At first, I pictured a city like New York, but then I thought that if it was me I would like to be in a more calm place. Somewhere I can be more relaxed.

That shift in perspective inspired my final idea: creating an AR experience that helps people immerse themselves in their favorite peaceful environment. A beach where you can hear waves splashing, a forest filled with birds and deer, or a campfire crackling in the night. A place to focus, recharge, and calm yourself.

### Role of AR Technologies

Augmented Reality is an important part in achieving the concept of HavenAR. Unlike traditional relaxation apps which rely on images or videos and some audio, AR allows users to project immersive 3D environments directly into their real-world space. For my app, using AR will bring a stronger sense of presence and connection, as the calming scene becomes part of the user’s surroundings rather than just viewing something on a flat screen.

By leveraging surface detection, real-time lighting, and spatial audio, AR technologies will make the experience feel more natural and adaptive. For instance, in HavenAR the user will be able to place the environment on a nearby table or the floor, reacting to the physical layout of the room. Through these interactions the user can personalize an ordinary space — whether it’s a miniature beach at your desk or a small forest beside your bed.

In short, HeavenAR benefits from AR by enhancing immersion, engagement, and personalization. The technology bridges the gap between the digital and physical worlds, allowing users to take short yet meaningful mental breaks wherever they are, without needing specialized equipment.

### Assets

Since the sea and forest models needed to fit onto different tables and floors, one of the main challenges was finding assets that could be scaled appropriately. In the process, I explored many models and gained insight into different rendering approaches—especially how water effects can vary depending on the implementation and use case.

The first asset I tried for the water effect was [Water Works by GapperGames Studios](https://assetstore.unity.com/packages/3d/environments/waterworks-simple-water-ocean-river-system-for-urp-reflection-re-206909). It looked promising with its wave animations and initially matched the project’s vision. However, when scaled down, the effects disappeared because the system relies on a simple plane, and reducing the scale caused the visual details to vanish. To address this, I switched to [URP Stylized Water Shader by BitGem](https://assetstore.unity.com/packages/vfx/shaders/urp-stylized-water-shader-proto-series-187485) which proved to be much more robust and maintained its quality even at smaller scales.

### UI

So far, I’ve made progress on the UI and the first core features. The environment pane can now be opened, environments can be dragged into place, and their size can be adjusted through scaling panel. Getting this to work required some setup with controllers and managers, which took time to configure properly.

The final result was good. The drag-and-drop interaction were intuitive and gives a stronger sense of control compared to a simple select-and-click approach. It creates a smoother, more natural workflow for the user, as placing environments now feels like a hands-on interaction rather than a detached menu action.

With this my first milestone around setting the environment was done. Next steps are to clean the scripts and add more functionality like spawning objects, panel workflow, audio to objects, clear button, etc. 

## Assets:
- [Campfire](https://assetstore.unity.com/packages/3d/props/the-free-medieval-and-war-props-174433)
- [Water](https://assetstore.unity.com/packages/vfx/shaders/urp-stylized-water-shader-proto-series-187485)
- [Forest](https://assetstore.unity.com/packages/3d/vegetation/environment-pack-free-forest-sample-168396)
- [Fire](https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-fire-vfx-urp-266226)
- [Dog](https://assetstore.unity.com/packages/3d/characters/animals/mammals/3d-stylized-animated-dogs-kit-284699)
- [Pictures](https://www.flaticon.com/)
- [Sounds](https://pixabay.com/)