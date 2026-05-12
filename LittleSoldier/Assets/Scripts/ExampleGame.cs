using MirrorVerse;
using MirrorVerse.UI.MirrorSceneClassyUI;
using MirrorVerse.UI.Renderers;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class ExampleGame : MonoBehaviour
{
    public StaticMeshRenderer staticMeshRenderer;
    public NavMeshRenderer navMeshRenderer;
    public Canvas gameUI;
    public Canvas rootUI;
    public GameObject soldierPrefab;
    public GameObject obstaclePrefab;

    private List<GameObject> _soldiers = new();
    private List<GameObject> _obstacles = new();

	private static object GetCoreImplInternalObject()
	{
		IMirrorScene iMirrorScene = MirrorScene.Get();
		const BindingFlags InstanceBindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var coreField = iMirrorScene.GetType().GetField("_core", InstanceBindFlags);
		var coreValue = coreField.GetValue(iMirrorScene);
		return coreValue;
	}

	private static void SetArSessionServiceEndpoint()
	{
        // Set endpoint by area type.
        string endpoint = "US";

		const BindingFlags InstanceBindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var coreValue = GetCoreImplInternalObject();

		var setArSessionServiceEndpointMethod = coreValue.GetType().GetMethod("SetArSessionServiceEndpoint", InstanceBindFlags);
		Debug.Log($"SetArSessionServiceEndpoint {setArSessionServiceEndpointMethod}, is set to: [{endpoint}]");
		setArSessionServiceEndpointMethod.Invoke(coreValue, new object[] { endpoint });

		var SetAuthServiceEndpointMethod = coreValue.GetType().GetMethod("SetAuthServiceEndpoint", InstanceBindFlags);
		Debug.Log($"SetAuthServiceEndpoint {SetAuthServiceEndpointMethod}, is set to: [{endpoint}]");
		SetAuthServiceEndpointMethod.Invoke(coreValue, new object[] { endpoint });
	}

	private void Start()
    {
        rootUI.gameObject.SetActive(true);
        gameUI.gameObject.SetActive(false);

		if (MirrorScene.IsAvailable())
        {

			SetArSessionServiceEndpoint();

			Debug.Log($"MirrorScene API is available.");
            // Application can listen to some events.
            MirrorScene.Get().onSceneReady += OnMirrorSceneReady;
            ClassyUI.Instance.onMenuFinish += OnMenuFinish;
            ClassyUI.Instance.onMenuCancel += OnMenuCancel;
        }
        else
        {
            Debug.Log($"MirrorScene API is not available.");
        }
    }

    public void OnMenuFinish()
    {
        // Called by custom finish prefab.
        Debug.Log($"Example scene scan flow finished.");
        // Update UI.
        gameUI.gameObject.SetActive(true);
        rootUI.gameObject.SetActive(true);
    }

    public void OnMenuCancel()
    {
        // Called by custom finish prefab.
        Debug.Log($"Example scene scan flow cancelled.");

        // Update UI.
        gameUI.gameObject.SetActive(false);
        rootUI.gameObject.SetActive(true);
    }
    
    public void OnMirrorSceneReady(StatusOr<SceneInfo> sceneInfo)
    {
        // Called by MirrorScene system when a scene has processed.
        if (sceneInfo.HasValue)
        {
            Debug.Log($"Example scanned scene is ready to use. {sceneInfo.Value.status}");
        }
        else
        {
            Debug.Log($"Example scanned scene failed to process.");
        }
    }

    public void OnScanButtonClicked()
    {
        Debug.Log($"Example re-start scene scan flow.");
        // Update UI.
        gameUI.gameObject.SetActive(false);
        rootUI.gameObject.SetActive(false);
        
        // Clear all field.
        OnClearButtonClicked();

        ClassyUI.Instance.ExitScene();

        ClassyUI.Instance.RestartToCreate();
    }

    public void OnSpawnSoldierButtonClicked()
    {
        var pose = MirrorScene.Get().GetCurrentCursorPose();
        if (pose.HasValue)
        {
            Vector3 orientation = pose.Value.rotation.eulerAngles;
            orientation.x = 0;
            orientation.z = 0;
            var soldier = Instantiate(soldierPrefab, pose.Value.position, Quaternion.Euler(orientation));
            _soldiers.Add(soldier);
        }
    }

    public void OnSpawnObstacleButtonClicked()
    {
        var pose = MirrorScene.Get().GetCurrentCursorPose();
        if (pose.HasValue)
        {
            Vector3 orientation = pose.Value.rotation.eulerAngles;
            orientation.x = 0;
            orientation.z = 0;
            var obstacle = Instantiate(obstaclePrefab, pose.Value.position, Quaternion.Euler(orientation));
            _obstacles.Add(obstacle);
        }
    }

    public void OnClearButtonClicked()
    {
        foreach (GameObject soldier in _soldiers)
        {
            Destroy(soldier);
        }
        _soldiers.Clear();
        foreach (GameObject obstacle in _obstacles)
        {
            Destroy(obstacle);
        }
        _obstacles.Clear();
    }

    public void ToggleMesh(Toggle change)
    {
        print("ToggleMesh:" + change.isOn);
        staticMeshRenderer.options.withOcclusion = !change.isOn;
        staticMeshRenderer.options.receivesShadow = !change.isOn;
    }
    
    public void ToggleNavMesh(Toggle change)
    {
        print("ToggleNavMesh:" + change.isOn);
        navMeshRenderer.options.visible = change.isOn;
    }

    private void Update()
    {
        var pose = MirrorScene.Get().GetCurrentCursorPose();
        if (pose.HasValue)
        {
            foreach (var bot in _soldiers)
            {
                var navigationBot = bot.GetComponent<NavigationBot>();
                if (navigationBot != null)
                {
                    navigationBot.SetDestination(pose.Value.position);
                }
            }
        }
    }
}
