using UnityEngine;

namespace SkyboxBrightnessControl;

public class Body
{
    public CelestialBody celestialBody { get; }
    public Camera primaryCamera { get; }
    public PlanetariumCamera secondaryCamera { get; }
    public float effectiveRadius { get; }

    public float weightBody { get; protected set; }
    public float distanceBody { get; protected set; }
    public float angleBody { get; protected set; }
    public float angularRadiusBody { get; protected set; }

    public Body(CelestialBody inputBody, Camera inputPrimaryCamera, PlanetariumCamera inputSecondaryCamera)
    {
        celestialBody = inputBody;
        primaryCamera = inputPrimaryCamera;
        secondaryCamera = inputSecondaryCamera;
        effectiveRadius = (float)celestialBody.Radius + (celestialBody.atmosphere ? (float)celestialBody.atmosphereDepth : 0);
        updateStats();
    }

    public void updateStats()
    {
        Vector3 cameraPosition;
        Vector3 cameraAim;
        float cameraAspectRatio;
        float cameraFOV;
        if (MapView.MapIsEnabled)
        {
            cameraPosition = secondaryCamera.transform.position;
            cameraAim = secondaryCamera.transform.forward;
            cameraAspectRatio = 1.78f;
            cameraFOV = 60f;
        }
        else
        {
            cameraPosition = primaryCamera.transform.position;
            cameraAim = primaryCamera.transform.forward;
            cameraAspectRatio = primaryCamera.aspect;
            cameraFOV = primaryCamera.fieldOfView;
        }
        var relativePositionBody =  (Vector3)celestialBody.position - cameraPosition;
        var modifiedFOV = cameraFOV * cameraAspectRatio / 2;
        distanceBody = relativePositionBody.magnitude;
        angularRadiusBody = Mathf.Asin(effectiveRadius / distanceBody) * Mathf.Rad2Deg;
        angleBody = Vector3.Angle(relativePositionBody, cameraAim);

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