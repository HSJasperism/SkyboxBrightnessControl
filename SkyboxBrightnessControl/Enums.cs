using System;

namespace SkyboxBrightnessControl;

public enum Occultation
{
    None = 0,
    Penumbra = 1,
    Antumbra = 2,
    Umbra = 3
}

[Flags]
public enum Layers
{
    Default = 1 << 0,
    Atmosphere = 1 << 9,
    ScaledSpace = 1 << 10
}