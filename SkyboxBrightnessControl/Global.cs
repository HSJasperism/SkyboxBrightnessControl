using UnityEngine;

namespace SkyboxBrightnessControl;

public static class Global
{
    public static Color blendColors(Color color1, Color color2, float weight1 = 1, float weight2 = 1)
    {
        // Deal with floating point shenanigans first
        if (weight1 < 0) weight1 = 0;
        if (weight2 < 0) weight2 = 0;
        if (weight1 + weight2 == 0)
        {
            weight1 = 1;
            weight2 = 1;
        }

        var red = Mathf.Sqrt((color1.r * color1.r * weight1 + color2.r * color2.r * weight2) / (weight1 + weight2));
        var green = Mathf.Sqrt((color1.g * color1.g *  weight1 + color2.g * color2.g * weight2) / (weight1 + weight2));
        var blue = Mathf.Sqrt((color1.b * color1.b *  weight1 + color2.b * color2.b * weight2) / (weight1 + weight2));

        // Deal with floating point shenanigans again
        if (red > 1) red = 1;
        if (green > 1) green = 1;
        if (blue > 1) blue = 1;

        return new Color(red, green, blue);
    }
}