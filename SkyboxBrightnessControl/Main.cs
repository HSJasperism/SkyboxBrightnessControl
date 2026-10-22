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
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SkyboxBrightnessControl;

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class SkyboxBrightnessControl : MonoBehaviour
{
    public static SkyboxBrightnessControl Instance { get; protected set; }
    protected GalaxyCubeControl Sky;

    protected BodyContainer allBodies;
    protected Camera primaryCamera;
    protected Camera secondaryCamera;

    protected Task SkywashCalculations;
    protected short calculationRetries;
    protected Color targetColor;

    protected void Awake()
    {
        switch (HighLogic.LoadedScene)
        {
            case GameScenes.FLIGHT:
            case GameScenes.SPACECENTER:
            case GameScenes.TRACKSTATION:
            {
                Instance = this;
                break;
            }
            default:
            {
                enabled = false;
                Logging.Info($"Disabled for scene '{HighLogic.LoadedScene.ToString()}'");
                break;
            }
        }
    }

    protected void Start()
    {
        try
        {
            Sky = GalaxyCubeControl.Instance;
            primaryCamera = FlightCamera.fetch.mainCamera;
            secondaryCamera = Camera.allCameras.FirstOrDefault(c => c.name == "Camera ScaledSpace");

            // Disable stock sun fading
            Sky.glareFadeLimit = 0f;
            Sky.glareFadeLerpRate = 1f;

            // Allow full fading on vacuum bodies during the day
            Sky.daytimeFadeLimit = 1f;

            // Let Scatterer handle sky color and fading
            Sky.airPressureFade = 0f;
            Sky.atmosFadeLimit = 0f;

            Sky.minGalaxyColor = Color.black;
            Sky.maxGalaxyColor = Color.white;
            targetColor = Color.white;

            allBodies = new BodyContainer(FlightGlobals.Bodies, primaryCamera, secondaryCamera);
        }
        catch (Exception e)
        {
            Logging.Error($"Failed to initialize skybox brightness control with the following error:\n{e.Message}");
            enabled = false;
            return;
        }

        Logging.Info($"Successfuly started.");
        Logging.printActiveCameras();
    }

    protected void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (SkywashCalculations is null) return;
        switch (SkywashCalculations.Status)
        {
            case TaskStatus.Running:
            {
                SkywashCalculations.Wait();
                SkywashCalculations.Dispose();
                break;
            }
            case TaskStatus.RanToCompletion:
            case TaskStatus.Canceled:
            case TaskStatus.Faulted:
            {
                SkywashCalculations.Dispose();
                break;
            }
            default:
            {
                Logging.Warning($"Could not dispose of calculations. Status: {SkywashCalculations.Status}");
                break;
            }
        }
    }

    protected void Update()
    {
        // Check if Task exists
        if (SkywashCalculations is null)
        {
            SkywashCalculations = Task.Run(allBodies.updateAllStats);
            return;
        }

        // Check Task status
        switch (SkywashCalculations.Status)
        {
            case TaskStatus.RanToCompletion:
            {
                targetColor = Global.getGray(allBodies.maxSkywash);
                SkywashCalculations.Dispose();
                SkywashCalculations = Task.Run(allBodies.updateAllStats);
                break;
            }
            case TaskStatus.Running:
            {
                break;
            }
            case TaskStatus.Faulted:
            {
                if (calculationRetries > 30)
                {
                    Logging.Error($"Exceeded maximum calcullation retries with error:\n{SkywashCalculations.Exception}");
                    Sky.maxGalaxyColor = Color.white;
                    enabled = false;
                    return;
                }
                calculationRetries++;
                Logging.Error($"Calculation faulted. Retrying...\n{SkywashCalculations.Exception}");
                SkywashCalculations.Dispose();
                SkywashCalculations = Task.Run(allBodies.updateAllStats);
                break;
            }
            default:
            {
                Logging.Verbose($"Calculation status: {SkywashCalculations.Status}");
                break;
            }
        }

        Sky.maxGalaxyColor = Color.Lerp(Sky.maxGalaxyColor, targetColor,
                                        Sky.maxGalaxyColor.maxColorComponent > targetColor.maxColorComponent
                                            ? 0.150f
                                            : 0.005f);
    }
}