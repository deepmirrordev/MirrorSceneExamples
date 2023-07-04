using MirrorVerse;
using MirrorVerse.UI.MirrorSceneDefaultUI;
using MirrorVerse.UI.Renderers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExampleGame : MonoBehaviour
{
    public StaticMeshRenderer staticMeshRenderer;
    public NavMeshRenderer navMeshRenderer;
    public Canvas gameUI;

    public GameObject soldierPrefab;
    public GameObject obstaclePrefab;

    private List<GameObject> _soldiers = new();
    private List<GameObject> _obstacles = new();

    private void Start()
    {
        gameUI.gameObject.SetActive(false);
        if (MirrorScene.IsAvailable())
        {
            Debug.Log($"MirrorScene API is available.");
            // Application can listen to some events.
            DefaultUI.Instance.onMenuFinish += OnMenuFinish;
            DefaultUI.Instance.onMenuCancel += OnMenuCancel;
        }
        else
        {
            Debug.Log($"MirrorScene API is not available.");
        }

        StartMirrorSceneScanFlow();
    }

    private void StartMirrorSceneScanFlow()
    {
        Debug.Log($"Example start scene scan flow.");
        DefaultUI.Instance.Restart();

        // Update UI.
        gameUI.gameObject.SetActive(false);
    }

    public void OnMenuFinish()
    {
        // Called by custom finish prefab.
        Debug.Log($"Example scene scan flow finished.");
        // Update UI.
        gameUI.gameObject.SetActive(true);
    }

    public void OnMenuCancel()
    {
        // Called by custom finish prefab.
        Debug.Log($"Example scene scan flow cancelled.");
        // Quit the app.
        Application.Quit();
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

    public void OnResetButtonClicked()
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
