# HavenAR - Research, decisions and intial work

[Back](../README.md)

_Written by: Martin Donchev_

## Introduction

HavenAR is a place were you can immerse into an environment using only your phone and headphones. During busy days we do not have a lot of time for relaxation, so we stress out. HeavenAR aims to be an app where you can immerse in a calm environment projected on nearby table. This way you focus on the chosen by you environment and isolate the stress factor.

## Current progress

### Ideation

The final concept emerged after a long process of ideation. It all began with the idea of using AR to project an object. My first thought was to create a sorting app where AR could help users place trash into the correct bins. However, I realized that simpler non-XR solution already exist in Denmark, with packaging labels that clearly indicate the appropriate recycling bin.

From there, I considered developing an AR project using 3D models to enhance understanding, but I found that many such implementations already existed online, and even our teacher showed us student examples. Because of this, I set aside my ideas related to biology and the solar system.

Still, while brainstorming around the solar system, I imagined portals to different planets and began to wonder: where should a portal to Earth lead? At first, I pictured a bustling city like New York, but then I thought about my own preference—I would rather be in a calm, relaxing place.

That shift in perspective inspired my final idea: creating an AR experience that helps people immerse themselves in their favorite peaceful environment. A beach where you can hear waves splashing, a forest filled with birds and deer, or a campfire crackling in the night. A place to focus, recharge, and reduce stress.

### Assets

Since the sea and forest models needed to fit onto different tables and floors, one of the main challenges was finding assets that could be scaled appropriately. In the process, I explored many models and gained insight into different rendering approaches—especially how water effects can vary depending on the implementation and use case.

The first asset I tried for the water effect was [Water Works by GapperGames Studios](https://assetstore.unity.com/packages/3d/environments/waterworks-simple-water-ocean-river-system-for-urp-reflection-re-206909). It looked promising with its wave animations and initially matched the project’s vision. However, when scaled down, the effects disappeared because the system relies on a simple plane, and reducing the scale caused the visual details to vanish. To address this, I switched to [URP Stylized Water Shader by BitGem](https://assetstore.unity.com/packages/vfx/shaders/urp-stylized-water-shader-proto-series-187485) which proved to be much more robust and maintained its quality even at smaller scales.

### UI

So far, I’ve made progress on the UI and the first core features. The environment pane can now be opened, environments can be dragged into place, and their size can be adjusted through scaling. Getting this to work required some setup with controllers and managers, which took time to configure properly.

However, the effort paid off. The drag-and-drop interaction feels intuitive and gives a stronger sense of control compared to a simple select-and-click approach. It creates a smoother, more natural workflow for the user, as placing and resizing environments now feels like a hands-on interaction rather than a detached menu action.

## Assets:
- [Campfire](https://assetstore.unity.com/packages/3d/props/the-free-medieval-and-war-props-174433)
- [Water](https://assetstore.unity.com/packages/vfx/shaders/urp-stylized-water-shader-proto-series-187485)
- [Forest](https://assetstore.unity.com/packages/3d/vegetation/environment-pack-free-forest-sample-168396)
- [Fire](https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-fire-vfx-urp-266226)
- [Dog](https://assetstore.unity.com/packages/3d/characters/animals/mammals/3d-stylized-animated-dogs-kit-284699)
- [Pictures](https://www.flaticon.com/)