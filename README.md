# Skybox Brightness Control

Takes over stock KSP's skybox visibility manipulation, using Tasks to minimize the overhead. This has not been tested with Volumetric Clouds V4 and V5, but should not have any conflicts.

## Images
* [Imgur](https://imgur.com/a/W76VBDg)
* [Tumblr](https://www.tumblr.com/cleistocactus/830882221806272513?source=share)

## Features
* Simulates sky wash from bright celestial bodies in the foreground, not just stars
* Includes occlusion in sky wash calculations, ensuring a visible skybox when no celestial body is visibly lit
* Simulates sky wash from lit surface terrain
* Simulates eye adaptation in changing conditions
* Disables stock atmospheric extinction in the night, allowing the sky box be visible and be affected by Scatterer and Volumetric Clouds V3
* Scatterer will limit what will be visible depending on the atmospheric depth. The deeper you go, the brighter features need to be to show.
* Clouds from Volumetric Clouds V3 will directly obscure the sky box.
* Works in Flight, Flight Map View, Tracking Station, and KSC
* Supports the closest star in multi-star systems
* Uses C# Tasks, made available by KSP 1.12.x, to reduce overhead
* Uses only Mathf trigonometry and vector calculations, no physics raycasts used

## Planned Features
* Customizable settings for each feature
* Compare stars' apparent brightness instead of using the closest star as the reference light by default
* Module Manager patch for Scatterer colors (white stars in Kerbin sea level will look orange rather than yellow-orange)
* Atmospheric scintillation
* Support for Camera mods (camera mods will currently see the skybox as defined by the flight camera)

## Limitations
For now, occlusion is limited to hard shadows. Since KSP stars emit light as a point, the calculations assume point light sources. This means penumbras and antumbras with stock lighting aren't possible. Scatterer's sunflares are also point raycasts, so their behavior is also as points. With trig it is possible to simulate the partial shadow conditions and an accurately shaped umbra, but both the scene lighting and sunflares would not match up with what the sky box would be doing.

## Requirements
* KSP 1.12.x is mandatory, older versions do not support C# Tasks. In general, if you can use [KSP Texture Loader](https://github.com/Phantomical/KSPTextureLoader), this mod will work.
* [Scatterer](https://github.com/LGhassen/Scatterer) is strongly recommended, but will work without
* Disable Distant Object Enhancement's dimming feature, it is a direct conflict

## Copyleft
* Skybox Brightness Control is distributed under GPLv3, ensuring the source and compiled code remain free to use, change, and distribute by users and authors with the same rights and freedom.
* Collaboration is strongly invited.
* Please see the included [license](https://github.com/HSJasperism/SkyboxBrightnessControl/blob/master/LICENSE.md) for details.

## Contribution

### [KSP Forums Link](https://forum.kerbalspaceprogram.com/profile/195020-eridan/)

### [Github](https://github.com/HSJasperism/SkyboxBrightnessControl)

### [Issues at Github](https://github.com/HSJasperism/SkyboxBrightnessControl/issues)