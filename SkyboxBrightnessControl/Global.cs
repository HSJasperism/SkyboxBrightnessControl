using UnityEngine;

namespace SkyboxBrightnessControl
{
    public static class Global
    {
        public static Color getGray(float weight)
        {
            weight = Mathf.Clamp((100f - weight) / 100f, 0f, 1f);
            return new Color(weight, weight, weight);
        }
    }
}