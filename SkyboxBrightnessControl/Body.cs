using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkyboxBrightnessControl;

public class Bodies
{
    public List<Body> members { get; protected set; }
    public List<Body> stars { get; protected set; }

    public Body closestStar => stars[0];

    public Camera primaryCamera { get; protected set; }
    public Camera secondaryCamera { get; protected set; }

    public Bodies(List<CelestialBody> inputCelestialBodies, Camera inputPrimaryCamera, Camera inputSecondaryCamera)
    {
        members = new List<Body>(inputCelestialBodies.Count);
        stars = [];
        foreach (var celestialBody in inputCelestialBodies)
        {
            var body = new Body(this, celestialBody);
            members.Add(body);
            if (celestialBody.isStar) stars.Add(body);
        }

        primaryCamera = inputPrimaryCamera;
        secondaryCamera = inputSecondaryCamera;

        Logging.Info($"Bodies initialized with {members.Count} members, {stars.Count} stars, primary {primaryCamera.name}, & secondary {secondaryCamera.name}.");
    }

    public void updateMemberStats()
    {
        foreach (var body in members) body.updateStats();
    }
}

public class Body
{
    public const float over180 = 1 / 180f;

    public Bodies Parent { get; protected set; }
    public CelestialBody celestialBody { get; }

    public float effectiveRadius { get; }

    public float weightBody { get; protected set; }
    public float distanceBody { get; protected set; }
    public float angleBody { get; protected set; }
    public float angularRadiusBody { get; protected set; }

    public Body(Bodies parent, CelestialBody inputBody)
    {
        Parent = parent;
        celestialBody = inputBody;
        effectiveRadius = (float)celestialBody.Radius +
                          (celestialBody.atmosphere ? (float)celestialBody.atmosphereDepth : 0);
    }

    public void updateStats()
    {
        Camera inputCamera;
        if (MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
        {
            inputCamera = Parent.secondaryCamera;
        }
        else
        {
            inputCamera = Parent.primaryCamera;
        }

        var relativePositionBody = (Vector3)celestialBody.position - inputCamera.transform.position;
        distanceBody = relativePositionBody.magnitude;
        angularRadiusBody = Mathf.Asin(effectiveRadius / distanceBody) * Mathf.Rad2Deg;
        angleBody = Vector3.Angle(relativePositionBody, inputCamera.transform.forward);

        float modifiedFOV = inputCamera.fieldOfView * inputCamera.aspect * 0.5f;

        // Out of view check
        if (angleBody - angularRadiusBody > modifiedFOV)
        {
            weightBody = 0;
            return;
        }

        weightBody = 100;

        // Decrease intensity as object moves out of FOV
        if (angleBody - angularRadiusBody >= modifiedFOV * 0.8f)
            weightBody *= Mathf.SmoothStep(1f, 0f,
                                           (angleBody - angularRadiusBody - modifiedFOV * 0.8f) / (modifiedFOV * 0.2f));

        // Specific calcs
        if (!celestialBody.isStar)
        {
            // Decrease intensity with decreasing angular radius
            weightBody *= Mathf.Clamp(angularRadiusBody / Parent.closestStar.angularRadiusBody, 0, 1);

            // Decrease intensity with increasing phase
            var StarToBody = (Vector3)Parent.closestStar.celestialBody.position - (Vector3)celestialBody.position;
            float phaseAngle = Vector3.Angle(StarToBody, relativePositionBody * -1);
            weightBody *= Mathf.SmoothStep(1f, 0f, phaseAngle * over180);
        }
    }

    public void rayCast()
    {
        Camera inputCamera;
        int layerMask;

        if (MapView.MapIsEnabled || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
        {
            inputCamera = Parent.secondaryCamera;
            layerMask = (int)Layers.ScaledSpace;
        }
        else
        {
            inputCamera = Parent.primaryCamera;
            layerMask = (int)(Layers.Default | Layers.LocalScenery);
        }

        try
        {
            if (Physics.Raycast(inputCamera.transform.position,
                                (Vector3)celestialBody.position - inputCamera.transform.position,
                                distanceBody,
                                layerMask,
                                QueryTriggerInteraction.Ignore))
            {
                weightBody = 0;
            }
        }
        catch (Exception e)
        {
            Logging.Error($"Error raycasting {celestialBody.name}: {e.Message}");
        }
    }
}