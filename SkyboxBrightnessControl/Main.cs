using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

namespace SkyboxBrightnessControl;

[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
public class SkyboxBrightnessControl : MonoBehaviour
{
    public static SkyboxBrightnessControl Instance { get; protected set; }
    protected GalaxyCubeControl Sky;

    protected Bodies allBodies;
    protected Camera primaryCamera;
    protected Camera secondaryCamera;

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

        allBodies = new Bodies(FlightGlobals.Bodies, primaryCamera, secondaryCamera);

        Logging.Info($"Successfuly started skybox brightness control.");
    }

    protected void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (WeightCalculations == null) return;
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
    }

    protected void FixedUpdate()
    {
        // Check if Task exists
        if (WeightCalculations == null)
        {
            WeightCalculations = Task.Run(allBodies.updateMemberStats);
            return;
        }

        // Check Task status
        switch (WeightCalculations.Status)
        {
            case TaskStatus.RanToCompletion:
            {
                allBodies.closestStar.rayCast();

                float weight = 0;
                foreach (var body in allBodies.members) weight = Mathf.Max(weight, body.weightBody);

                targetColor = Global.getGray(weight);

                WeightCalculations.Dispose();
                WeightCalculations = Task.Run(allBodies.updateMemberStats);
                Logging.Verbose($"Successfully calculated weights for {allBodies.members.Count} bodies");
                break;
            }
            case TaskStatus.Running:
            {
                Logging.Verbose("Still waiting for calculations"); // May be an indication of low system resources if this occurs too much
                break;
            }
            case TaskStatus.Faulted:
            {
                if (calculationRetries <= 30)
                {
                    calculationRetries++;
                    Logging.Error($"Calculation faulted. Retrying...\n{WeightCalculations.Exception}");
                    WeightCalculations.Dispose();
                    WeightCalculations = Task.Run(allBodies.updateMemberStats);
                }
                else
                {
                    {
                        Logging.Error("Exceeded maximum retries for calculation");
                        enabled = false;
                    }
                }

                break;
            }
            default:
            {
                Logging.Verbose($"Calculation status: {WeightCalculations.Status}");
                break;
            }
        }

        GalaxyCubeControl.Instance.maxGalaxyColor = Global.blendColorsLL(GalaxyCubeControl.Instance.maxGalaxyColor,
                                                                         targetColor,
                                                                         95,
                                                                         5);
    }
}