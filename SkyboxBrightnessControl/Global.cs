using UnityEngine;

namespace SkyboxBrightnessControl;

public static class Global
{
    public static Color getGray(float weight)
    {
        float clampedWeight = Mathf.Clamp((100f - weight) / 100f, 0, 1);
        return new Color(clampedWeight, clampedWeight, clampedWeight);
    }
}