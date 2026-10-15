/*
Skybox Brightness Control - asynchronous darkening of KSP's skybox in various situations
Copyright (C) 2026 HSJasperism <https://github.com/HSJasperism/>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SkyboxBrightnessControl;

public class BodyContainer
{
    public List<Body> bodies { get; }
    public List<Body> stars { get; }
    public bool starsSorted { get; protected set; }

    public Body closestStar
    {
        get
        {
            if (stars.Count > 1 && !starsSorted)
            {
                stars.Sort((a, b) => a.distanceToViewer.CompareTo(b.distanceToViewer));
                if (stars[0].distanceToViewer > 0) starsSorted = true;
            }

            return stars[0];
        }
    }

    public Camera primaryCamera { get; }
    public Camera secondaryCamera { get; }

    public void ActiveCamera(out Vector3 position, out Vector3 forward, out float fov)
    {
        switch (HighLogic.LoadedScene)
        {
            case GameScenes.SPACECENTER:
            case GameScenes.FLIGHT:
            {
                if (MapView.MapIsEnabled)
                {
                    position = ScaledSpace.ScaledToLocalSpace(secondaryCamera.transform.position);
                    forward = secondaryCamera.transform.forward;
                    fov = secondaryCamera.fieldOfView * secondaryCamera.aspect * 0.5f;
                }
                else
                {
                    position = primaryCamera.transform.position;
                    forward = primaryCamera.transform.forward;
                    fov = primaryCamera.fieldOfView * primaryCamera.aspect * 0.5f;
                }

                break;
            }
            case GameScenes.TRACKSTATION:
            {
                position = ScaledSpace.ScaledToLocalSpace(secondaryCamera.transform.position);
                forward = secondaryCamera.transform.forward;
                fov = secondaryCamera.fieldOfView * secondaryCamera.aspect * 0.5f;
                break;
            }
            default:
            {
                position = primaryCamera.transform.position;
                forward = primaryCamera.transform.forward;
                fov = primaryCamera.fieldOfView * primaryCamera.aspect * 0.5f;
                break;
            }
        }
    }

    public float maxSkywash { get; protected set; }

    public BodyContainer(List<CelestialBody> inputCelestialBodies,
                         Camera inputPrimaryCamera,
                         Camera inputSecondaryCamera)
    {
        bodies = new List<Body>(inputCelestialBodies.Count);
        stars = [];
        foreach (var celestialBody in inputCelestialBodies)
        {
            var body = new Body(this, celestialBody);
            bodies.Add(body);
            if (celestialBody.isStar) stars.Add(body);
        }

        primaryCamera = inputPrimaryCamera;
        secondaryCamera = inputSecondaryCamera;

        Logging.Info(
            $"Bodies initialized with {bodies.Count} members, {stars.Count} stars, primary {primaryCamera.name}, & secondary {secondaryCamera.name}.");
    }

    public void updateAllStats()
    {
        ActiveCamera(out var cameraPosition, out var cameraForward, out float cameraFOV);

        foreach (var body in bodies) body.updateStats(cameraPosition, cameraForward);
        foreach (var body in bodies) body.updateOcclusion(cameraPosition, cameraForward, cameraFOV);

        maxSkywash = 0;
        foreach (var body in bodies)
        {
            if (body.isOccluded)
            {
                body.resetOcclusionFlag();
                continue;
            }

            maxSkywash = Mathf.Max(maxSkywash, body.skywash);
        }
    }
}

public class Body
{
    public BodyContainer bodyContainer { get; }
    public CelestialBody celestialBody { get; }

    public float effectiveRadius { get; }
    public Body closestStar => bodyContainer.closestStar;
    public bool isOccluded { get; protected set; }

    public float skywash { get; protected set; }
    public float distanceToViewer { get; protected set; }
    public float angleToViewer { get; protected set; }
    public float angularRadiusToViewer { get; protected set; }
    public float phaseAngleStar { get; protected set; }
    public float angularRadiusToStar { get; protected set; }

    public Body(BodyContainer bodyContainer, CelestialBody inputBody)
    {
        this.bodyContainer = bodyContainer;
        celestialBody = inputBody;
        effectiveRadius = (float)celestialBody.Radius;
        // effectiveRadius += (celestialBody.atmosphere ? (float)celestialBody.atmosphereDepth : 0);
    }

    public void updateStats(Vector3 inputCameraPosition, Vector3 inputCameraForward)
    {
        var relativePositionBody = (Vector3)celestialBody.position - inputCameraPosition;

        distanceToViewer = relativePositionBody.magnitude;
        angularRadiusToViewer = Mathf.Asin(effectiveRadius / distanceToViewer) * Mathf.Rad2Deg;
        angleToViewer = Vector3.Angle(relativePositionBody, inputCameraForward);

        // Sanity check, arc sin returns NaN if viewer is on or below the surface
        if (float.IsNaN(angularRadiusToViewer)) angularRadiusToViewer = float.PositiveInfinity;
    }

    public void updateOcclusion(Vector3 inputCameraPosition, Vector3 inputCameraForward, float modifiedFOV)
    {
        if (celestialBody.isStar)
        {
            // Out of view or occluded
            if (angleToViewer - angularRadiusToViewer > modifiedFOV || isOccluded) skywash = 0;
            else
            {
                skywash = 100;

                // Decrease intensity as star moves out of FOV
                if (angleToViewer - angularRadiusToViewer >= modifiedFOV * 0.8f)
                    skywash *= Mathf.SmoothStep(
                        1f, 0f, (angleToViewer - angularRadiusToViewer - modifiedFOV * 0.8f) / (modifiedFOV * 0.2f));
            }

            // Ignore the rest of the method
            return;
        }

        // Check if the planet or moon is occluding the closest star
        var StarToBody = (Vector3)closestStar.celestialBody.position - (Vector3)celestialBody.position;
        var ViewerToBody = inputCameraPosition - (Vector3)celestialBody.position;

        phaseAngleStar = Vector3.Angle(StarToBody, ViewerToBody);
        angularRadiusToStar = Mathf.Asin(effectiveRadius / StarToBody.magnitude) * Mathf.Rad2Deg;

        float theta = Mathf.Acos(effectiveRadius / distanceToViewer);
        if (float.IsNaN(theta)) theta = float.NegativeInfinity;
        float maxPhaseAngle = theta * Mathf.Rad2Deg + 90 - angularRadiusToStar;
        float phi = Mathf.Acos(effectiveRadius / distanceToViewer);
        if (float.IsNaN(phi)) phi = float.NegativeInfinity;
        float minPhaseAngle = (90 - angularRadiusToStar) - phi * Mathf.Rad2Deg;

        // Phase Angle & Occlusion
        if (phaseAngleStar >= maxPhaseAngle)
        {
            skywash = 0;
            closestStar.setOcclusionFlag();
        }
        else if (phaseAngleStar <= minPhaseAngle)
        {
            skywash = 100;
        }
        else
        {
            skywash = Mathf.SmoothStep(100f, 0f, (phaseAngleStar - minPhaseAngle) / (maxPhaseAngle - minPhaseAngle));
        }

        // Decrease intensity with decreasing angular radius (uncapped)
        skywash *= Mathf.Pow(angularRadiusToViewer, 2) / Mathf.Pow(closestStar.angularRadiusToViewer, 2);

        float phaseAngleModifier = Mathf.SmoothStep(1f, -1f, (phaseAngleStar - minPhaseAngle) / (maxPhaseAngle - minPhaseAngle));
        // FOV - Needs work, must not ignore camera forward transform
        if (angleToViewer - angularRadiusToViewer * phaseAngleModifier > modifiedFOV)
        {
            skywash = 0;
        }
        else if (angleToViewer - angularRadiusToViewer * phaseAngleModifier >= modifiedFOV * 0.8f)
        {
            skywash *= Mathf.SmoothStep(1f, 0f, (angleToViewer - angularRadiusToViewer * phaseAngleModifier - modifiedFOV * 0.8f) / (modifiedFOV * 0.2f));
        }


        // Parent body occlusion
        // foreach (var neighbor in planetarySystem)
        // {
        // unimplemented
        // }
    }

    public List<Body> planetarySystem
    {
        get
        {
            if (field is null)
            {
                field = new List<Body>(5);
                if (celestialBody.referenceBody && !celestialBody.referenceBody.isStar)
                {
                    field.Add(bodyContainer
                              .bodies
                              .First(t => t.celestialBody.name == celestialBody.referenceBody.name));
                    field.AddRange(bodyContainer
                                   .bodies
                                   .Where(t => t.celestialBody.referenceBody.name == celestialBody.referenceBody.name
                                               && t.celestialBody.name != celestialBody.name));
                    if (field[0].celestialBody.referenceBody && !field[0].celestialBody.referenceBody.isStar)
                    {
                        field.Add(bodyContainer
                                  .bodies
                                  .First(t => t.celestialBody.name == field[0].celestialBody.referenceBody.name));
                        field.AddRange(bodyContainer
                                       .bodies
                                       .Where(t => t.celestialBody.referenceBody.name ==
                                                   field[0].celestialBody.referenceBody.name
                                                   && t.celestialBody.name != field[0].celestialBody.name));
                    }
                }
            }

            return field;
        }
    }

    public void setOcclusionFlag() => isOccluded = true;
    public void resetOcclusionFlag() => isOccluded = false;
}