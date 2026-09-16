using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RCCTrafficRuntime : MonoBehaviour
{
    private const int DefaultVehicleCount = 12;
    private const float SpawnIntervalSeconds = 0.45f;
    private readonly List<RCC_AICarController> npcCars = new List<RCC_AICarController>();
    private RCC_AIWaypointsContainer waypointContainer;
    private RCCTrafficSignalController signalController;

    public void Initialize(Transform playerTransform, int vehicleCount)
    {
        if (playerTransform == null)
        {
            return;
        }

        BuildTrafficRoute(playerTransform);
        BuildTrafficSignals(playerTransform);
        SpawnNpcCars(playerTransform, vehicleCount);
    }

    private void BuildTrafficRoute(Transform playerTransform)
    {
        waypointContainer = FindFirstObjectByType<RCC_AIWaypointsContainer>();
        if (waypointContainer != null)
        {
            waypointContainer.GetAllWaypoints();

            for (int i = 0; i < waypointContainer.waypoints.Count; i++)
            {
                var waypoint = waypointContainer.waypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                waypoint.targetSpeed = Mathf.Min(waypoint.targetSpeed, 22f);
                waypoint.radius = Mathf.Max(waypoint.radius, 8f);
            }

            return;
        }

        var routeRoot = new GameObject("TrafficRoute");
        routeRoot.transform.SetParent(transform, false);
        routeRoot.transform.position = playerTransform.position + new Vector3(0f, 0f, 0f);

        waypointContainer = routeRoot.AddComponent<RCC_AIWaypointsContainer>();

        var routePoints = new[]
        {
            new Vector3(0f, 0f, 70f),
            new Vector3(-55f, 0f, 70f),
            new Vector3(-55f, 0f, 0f),
            new Vector3(-55f, 0f, -70f),
            new Vector3(0f, 0f, -70f),
            new Vector3(55f, 0f, -70f),
            new Vector3(55f, 0f, 0f),
            new Vector3(55f, 0f, 70f)
        };

        for (int i = 0; i < routePoints.Length; i++)
        {
            var point = new GameObject("Waypoint_" + i);
            point.transform.SetParent(routeRoot.transform, false);
            point.transform.localPosition = routePoints[i];
            var waypoint = point.AddComponent<RCC_Waypoint>();
            waypoint.radius = 12f;
            waypoint.targetSpeed = 20f;
        }

        waypointContainer.GetAllWaypoints();
    }

    private void BuildTrafficSignals(Transform playerTransform)
    {
        var signalRoot = new GameObject("TrafficSignal");
        signalRoot.transform.SetParent(transform, false);
        signalRoot.transform.position = playerTransform.position + new Vector3(0f, 0f, 0f);

        var stopLines = new List<Transform>();
        var lightGroups = new List<RCCTrafficSignalController.SignalGroup>();

        if (waypointContainer != null && waypointContainer.waypoints != null && waypointContainer.waypoints.Count >= 4)
        {
            for (int i = 0; i < 4; i++)
            {
                var waypoint = waypointContainer.waypoints[i];
                if (waypoint == null)
                {
                    continue;
                }

                var stopLine = new GameObject("StopLine_" + (i + 1));
                stopLine.transform.SetParent(signalRoot.transform, false);
                stopLine.transform.position = waypoint.transform.position;
                stopLines.Add(stopLine.transform);

                var redLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 3.2f, 0f + (i * 0.2f)), Color.red);
                var yellowLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 2.4f, 0f + (i * 0.2f)), Color.yellow);
                var greenLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 1.6f, 0f + (i * 0.2f)), Color.green);
                lightGroups.Add(new RCCTrafficSignalController.SignalGroup(redLight, yellowLight, greenLight));
            }
        }
        else
        {
            Vector3[] approachOffsets =
            {
                new Vector3(0f, 0f, 28f),
                new Vector3(0f, 0f, -28f),
                new Vector3(28f, 0f, 0f),
                new Vector3(-28f, 0f, 0f)
            };

            for (int i = 0; i < approachOffsets.Length; i++)
            {
                var stopLine = new GameObject("StopLine_" + (i + 1));
                stopLine.transform.SetParent(signalRoot.transform, false);
                stopLine.transform.position = playerTransform.position + approachOffsets[i];
                stopLines.Add(stopLine.transform);

                var redLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 3.2f, 0f + (i * 0.2f)), Color.red);
                var yellowLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 2.4f, 0f + (i * 0.2f)), Color.yellow);
                var greenLight = CreateSignalLight(signalRoot.transform, new Vector3(0f, 1.6f, 0f + (i * 0.2f)), Color.green);
                lightGroups.Add(new RCCTrafficSignalController.SignalGroup(redLight, yellowLight, greenLight));
            }
        }

        signalController = signalRoot.AddComponent<RCCTrafficSignalController>();
        signalController.Initialize(stopLines, lightGroups, 7f, 2.5f);
    }

    private GameObject CreateSignalLight(Transform parent, Vector3 positionOffset, Color lightColor)
    {
        var light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        light.name = lightColor.ToString() + "_Light";
        light.transform.SetParent(parent, false);
        light.transform.localPosition = positionOffset;
        light.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        var renderer = light.GetComponent<Renderer>();
        renderer.material.color = lightColor;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return light;
    }

    private void SpawnNpcCars(Transform playerTransform, int vehicleCount)
    {
        StartCoroutine(SpawnNpcCarsRoutine(playerTransform, vehicleCount));
    }

    private IEnumerator SpawnNpcCarsRoutine(Transform playerTransform, int vehicleCount)
    {
        if (waypointContainer == null || waypointContainer.waypoints == null || waypointContainer.waypoints.Count == 0)
        {
            Debug.LogWarning("No RCC waypoint container was available for traffic NPC spawning.");
            yield break;
        }

        var demoVehicles = RCC_DemoVehicles.Instance;
        if (demoVehicles == null || demoVehicles.vehicles == null || demoVehicles.vehicles.Length == 0)
        {
            Debug.LogWarning("RCC demo vehicle list could not be loaded. Traffic NPC spawning skipped.");
            yield break;
        }

        int spawnCount = Mathf.Min(vehicleCount, waypointContainer.waypoints.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            var prefab = demoVehicles.vehicles[i % demoVehicles.vehicles.Length];
            if (prefab == null)
            {
                yield return null;
                continue;
            }

            var waypoint = waypointContainer.waypoints[i];
            if (waypoint == null)
            {
                yield return null;
                continue;
            }

            Vector3 spawnPosition = waypoint.transform.position;
            Quaternion spawnRotation = waypoint.transform.rotation;

            if (i + 1 < waypointContainer.waypoints.Count)
            {
                var nextWaypoint = waypointContainer.waypoints[i + 1];
                if (nextWaypoint != null)
                {
                    spawnRotation = Quaternion.LookRotation(nextWaypoint.transform.position - waypoint.transform.position);
                }
            }

            spawnPosition += waypoint.transform.forward * 4f;

            var spawnedCar = RCC.SpawnRCC(prefab, spawnPosition, spawnRotation, false, true, true);
            if (spawnedCar == null)
            {
                yield return null;
                continue;
            }

            spawnedCar.transform.SetParent(transform, true);

            spawnedCar.useCounterSteering = false;
            spawnedCar.useSteeringLimiter = true;
            spawnedCar.useSteeringSensitivity = true;
            spawnedCar.steeringSensitivityFactor = 0.55f;
            spawnedCar.steeringHelper = true;
            spawnedCar.tractionHelper = true;
            spawnedCar.tractionHelperStrength = 0.12f;
            spawnedCar.useFuelConsumption = false;
            spawnedCar.useDamage = false;
            spawnedCar.maxspeed = 38f;

            var aiController = spawnedCar.gameObject.AddComponent<RCC_AICarController>();
            aiController.waypointsContainer = waypointContainer;
            aiController.currentWaypointIndex = i % waypointContainer.waypoints.Count;
            aiController.navigationMode = RCC_AICarController.NavigationMode.FollowWaypoints;
            aiController.limitSpeed = true;
            aiController.maximumSpeed = 26f;
            aiController.raycastLength = 4f;
            aiController.raycastAngle = 15f;
            aiController.useRaycasts = true;
            aiController.smoothedSteer = true;

            npcCars.Add(aiController);
            yield return new WaitForSeconds(SpawnIntervalSeconds);
        }
    }

    private void Update()
    {
        if (signalController == null || waypointContainer == null)
        {
            return;
        }

        for (int i = 0; i < npcCars.Count; i++)
        {
            var aiCar = npcCars[i];
            if (aiCar == null || aiCar.CarController == null)
            {
                continue;
            }

            int nearestApproachIndex = signalController.GetNearestApproachIndex(aiCar.transform.position);
            bool shouldBrakeForLight = signalController.ShouldBrakeForApproach(nearestApproachIndex, aiCar.transform.position);

            aiCar.brakeInput = shouldBrakeForLight ? 1f : 0f;
            aiCar.throttleInput = shouldBrakeForLight ? 0f : aiCar.throttleInput;

            if (shouldBrakeForLight && aiCar.CarController.speed < 5f)
            {
                aiCar.handbrakeInput = 1f;
            }
            else
            {
                aiCar.handbrakeInput = 0f;
            }
        }
    }
}

public class RCCTrafficSignalController : MonoBehaviour
{
    private List<Transform> stopLines = new List<Transform>();
    private List<SignalGroup> signalGroups = new List<SignalGroup>();
    private float greenDuration;
    private float yellowDuration;
    private float timer;
    private int currentGreenIndex;
    private TrafficLightPhase currentPhase;

    public int GetNearestApproachIndex(Vector3 position)
    {
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < stopLines.Count; i++)
        {
            if (stopLines[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(position, stopLines[i].position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    public bool ShouldBrakeForApproach(int approachIndex, Vector3 position)
    {
        if (approachIndex < 0 || approachIndex >= stopLines.Count)
        {
            return false;
        }

        if (stopLines[approachIndex] == null)
        {
            return false;
        }

        float distanceToStopLine = Vector3.Distance(position, stopLines[approachIndex].position);
        if (distanceToStopLine > 18f)
        {
            return false;
        }

        return !IsGreen(approachIndex);
    }

    public bool IsGreen(int approachIndex)
    {
        if (approachIndex < 0 || approachIndex >= signalGroups.Count)
        {
            return false;
        }

        return currentGreenIndex == approachIndex && currentPhase == TrafficLightPhase.Green;
    }

    public struct SignalGroup
    {
        public GameObject redLight;
        public GameObject yellowLight;
        public GameObject greenLight;

        public SignalGroup(GameObject red, GameObject yellow, GameObject green)
        {
            redLight = red;
            yellowLight = yellow;
            greenLight = green;
        }
    }

    private enum TrafficLightPhase
    {
        Green,
        Yellow
    }

    public void Initialize(List<Transform> stopLineTransforms, List<SignalGroup> groups, float greenSeconds, float yellowSeconds)
    {
        stopLines = stopLineTransforms;
        signalGroups = groups;
        greenDuration = greenSeconds;
        yellowDuration = yellowSeconds;
        currentGreenIndex = 0;
        currentPhase = TrafficLightPhase.Green;
        timer = greenDuration;

        UpdateVisuals();
    }

    private void Update()
    {
        if (signalGroups == null || signalGroups.Count == 0)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (currentPhase == TrafficLightPhase.Green && timer <= 0f)
        {
            currentPhase = TrafficLightPhase.Yellow;
            timer = yellowDuration;
            UpdateVisuals();
            return;
        }

        if (currentPhase == TrafficLightPhase.Yellow && timer <= 0f)
        {
            currentGreenIndex = (currentGreenIndex + 1) % signalGroups.Count;
            currentPhase = TrafficLightPhase.Green;
            timer = greenDuration;
            UpdateVisuals();
        }
    }

    private void UpdateVisuals()
    {
        for (int i = 0; i < signalGroups.Count; i++)
        {
            bool isGreen = i == currentGreenIndex && currentPhase == TrafficLightPhase.Green;
            bool isYellow = i == currentGreenIndex && currentPhase == TrafficLightPhase.Yellow;

            if (signalGroups[i].redLight != null)
            {
                signalGroups[i].redLight.SetActive(!isGreen && !isYellow);
            }

            if (signalGroups[i].yellowLight != null)
            {
                signalGroups[i].yellowLight.SetActive(isYellow);
            }

            if (signalGroups[i].greenLight != null)
            {
                signalGroups[i].greenLight.SetActive(isGreen);
            }
        }
    }
}
