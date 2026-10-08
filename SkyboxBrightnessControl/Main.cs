using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkyboxBrightnessControl;

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class SkyboxBrightnessControl : MonoBehaviour
{
    public static SkyboxBrightnessControl Instance;
    protected GalaxyCubeControl Sky;

    protected bool hasStarted;
    protected short startAttempts;

    protected List<Body> allBodies;
    protected Camera mainCamera;

    protected Task WeightCalculations;
    protected short calculationRetries;
    protected Color targetColor;

    protected void Awake()
    {
        switch (HighLogic.LoadedScene)
        {
            case GameScenes.FLIGHT:
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
        if (!GalaxyCubeControl.Instance)
        {
            startAttempts++;
            if (startAttempts >= 120) Logging.Warning("Starting attempts exceeded 120!");
            return;
        }

        Sky = GalaxyCubeControl.Instance;
        mainCamera = FlightCamera.fetch.mainCamera;

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

        allBodies = new List<Body>(FlightGlobals.Bodies.Count);
        WeightCalculations = Task.Run(updateWeights);

        startAttempts = 0;
        hasStarted = true;
        Logging.Info($"Successfuly started skybox brightness control with {FlightGlobals.Bodies.Count} bodies");
    }

    protected void OnDestroy()
    {
        switch (WeightCalculations.Status)
        {
            case TaskStatus.Running:
            {
                WeightCalculations.Wait();
                WeightCalculations.Dispose();
                break;
            }
            case TaskStatus.RanToCompletion:
            case TaskStatus.Canceled:
            case TaskStatus.Faulted:
            {
                WeightCalculations.Dispose();
                break;
            }
        }
        WeightCalculations = null;
        if (Sky)
        {
            Sky.maxGalaxyColor = Color.white;
            Sky = null;
        }
        if (mainCamera) mainCamera = null;
        allBodies = null;
        Instance = null;
    }

    protected void FixedUpdate()
    {
        if (!hasStarted && startAttempts <= 120)
        {
            Start();
            return;
        }

        // Checking for each body
        // Check if we are in the Penumbra, Umbra, or Antumbra

        // Check Task status
        switch (WeightCalculations.Status)
        {
            case TaskStatus.RanToCompletion:
            {
                float weight = 0;
                foreach (var body in allBodies) weight = Mathf.Max(weight, body.weightBody);

                targetColor = Global.blendColors(Color.black, Color.white, weight, 100 - weight);

                WeightCalculations.Dispose();
                WeightCalculations = Task.Run(updateWeights);
                Logging.Verbose($"Successfully calculated weights for {allBodies.Count} bodies");
                break;
            }
            case TaskStatus.Running:
            {
                Logging.Verbose("Still waiting for calculations");
                break;
            }
            case TaskStatus.Faulted:
            {
                if (calculationRetries <= 30)
                {
                    calculationRetries++;
                    Logging.Error($"Calculation faulted. Retrying...\n{WeightCalculations.Exception}");
                    WeightCalculations.Dispose();
                    WeightCalculations = Task.Run(updateWeights);
                }
                break;
            }
            default:
            {
                Logging.Verbose($"Calculation status: {WeightCalculations.Status}");
                break;
            }
        }

        GalaxyCubeControl.Instance.maxGalaxyColor = Global.blendColors(GalaxyCubeControl.Instance.maxGalaxyColor,
            targetColor,
            95,
            5);
    }

    public void updateWeights()
    {
        if (allBodies.Count == 0)
        {
            foreach (var celestialBody in FlightGlobals.Bodies)
            {
                allBodies.Add(new Body(celestialBody, mainCamera));
            }
        }
        else
        {
            foreach (var body in allBodies)
            {
                body.updateStats();
            }
        }
    }
}