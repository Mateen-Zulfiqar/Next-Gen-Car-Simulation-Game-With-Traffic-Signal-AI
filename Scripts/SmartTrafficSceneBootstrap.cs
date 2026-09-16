using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class SmartTrafficSceneBootstrap : MonoBehaviour
{
    private const string SceneName = "Traffic with lights";
    private const int VehicleCount = 12;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeScene()
    {
        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        if (FindFirstObjectByType<SmartTrafficSceneBootstrap>() != null)
        {
            return;
        }

        var bootstrapObject = new GameObject("SmartTrafficBootstrap");
        bootstrapObject.AddComponent<SmartTrafficSceneBootstrap>();
    }

    private void Awake()
    {
        StartCoroutine(InitializeTrafficCoroutine());
    }

    private IEnumerator InitializeTrafficCoroutine()
    {
        while (FindFirstObjectByType<RCC_CarControllerV4>() == null)
        {
            yield return new WaitForSeconds(0.25f);
        }

        var playerCar = FindFirstObjectByType<RCC_CarControllerV4>();
        if (playerCar == null)
        {
            yield break;
        }

        var runtime = gameObject.AddComponent<RCCTrafficRuntime>();
        runtime.Initialize(playerCar.transform, VehicleCount);
    }
}
