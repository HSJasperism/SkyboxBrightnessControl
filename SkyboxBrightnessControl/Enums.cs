using System;

namespace SkyboxBrightnessControl;

[Flags]
public enum Layers
{
    Default = 1 << 0,
    ScaledSpace = 1 << 10,
    LocalScenery = 1 << 15
}