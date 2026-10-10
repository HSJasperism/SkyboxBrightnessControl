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

        Logging.Info($"Successfuly started.");
    }

    protected void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (SkywashCalculations == null) return;
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
        }
    }

    protected void FixedUpdate()
    {
        // Check if Task exists
        if (SkywashCalculations == null)
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

                Logging.Verbose($"Successfully calculated weights for {allBodies.bodies.Count} bodies");
                break;
            }
            case TaskStatus.Running:
            {
                Logging.Verbose(
                    "Still waiting for calculations"); // May be an indication of low system resources if this occurs too much
                break;
            }
            case TaskStatus.Faulted:
            {
                if (calculationRetries <= 30)
                {
                    calculationRetries++;
                    Logging.Error($"Calculation faulted. Retrying...\n{SkywashCalculations.Exception}");
                    SkywashCalculations.Dispose();
                    SkywashCalculations = Task.Run(allBodies.updateAllStats);
                }
                else
                {
                    Logging.Error("Exceeded maximum retries for calculation");
                    enabled = false;
                }

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