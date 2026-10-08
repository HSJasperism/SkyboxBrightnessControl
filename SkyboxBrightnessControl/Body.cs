using UnityEngine;

namespace SkyboxBrightnessControl;

public class Body
{
    public CelestialBody celestialBody { get; }
    public Camera camera { get; }
    public float effectiveRadius { get; }

    public float weightBody { get; protected set; }
    public float distanceBody { get; protected set; }
    public float angleBody { get; protected set; }
    public float angularRadiusBody { get; protected set; }

    public Body(CelestialBody inputBody, Camera inputCamera)
    {
        celestialBody = inputBody;
        camera = inputCamera;
        effectiveRadius = (float)celestialBody.Radius + (celestialBody.atmosphere ? (float)celestialBody.atmosphereDepth : 0);
        updateStats();
    }

    public void updateStats()
    {
        var relativePositionBody =  (Vector3)celestialBody.position - camera.transform.position;
        var modifiedFOV = camera.fieldOfView * camera.aspect / 2;
        distanceBody = relativePositionBody.magnitude;
        angularRadiusBody = Mathf.Asin(effectiveRadius / distanceBody) * Mathf.Rad2Deg;
        angleBody = Vector3.Angle(relativePositionBody, camera.transform.forward);

        if (angleBody - angularRadiusBody > modifiedFOV) weightBody = 0;
        else if (angularRadiusBody > 1)
        {
            if (celestialBody.isStar)
            {
                if (angleBody - angularRadiusBody < modifiedFOV * 0.75) weightBody = 100;
                else weightBody = -400 * (angleBody - angularRadiusBody) / modifiedFOV + 400;
            }
            else weightBody = 0;
        }
    }
}