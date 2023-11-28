using MirrorVerse;
using MirrorVerse.UI.MirrorSceneDefaultUI;
using UnityEngine;

public class ExampleWithDefaultUI : MonoBehaviour
{
    private ExampleGame _game;

    private void Awake()
    {
        _game = GetComponent<ExampleGame>();
    }

    private void Start()
    {
        if (MirrorScene.IsAvailable())
        {
            Debug.Log($"MirrorScene API is available.");
            // Application can listen to some events.
            MirrorScene.Get().onSceneReady += OnMirrorSceneReady;
            DefaultUI.Instance.onMenuFinish += OnMenuFinish;
            DefaultUI.Instance.onMenuCancel += OnMenuCancel;
        }
        else
        {
            Debug.Log($"MirrorScene API is not available.");
        }

        _game.ShowMirrorSceneButtons();
    }

    private void OnDestroy()
    {
        DefaultUI.Instance.ExitScene();
    }

    public void StartMainMenu()
    {
        Debug.Log($"Example start MirrorScene menu.");

        _game.ClearAll();
        _game.HideButtons();

        DefaultUI.Instance.Restart();
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

    public void OnMenuFinish()
    {
        // Called by mirrorscene flow finished.
        Debug.Log($"Example MirrorScene menu finished.");
        _game.ShowGameButtons();
    }

    public void OnMenuCancel()
    {
        // Called by mirrorscene flow cancelled.
        Debug.Log($"Example MirrorScene menu cancelled.");
        _game.ShowMirrorSceneButtons();
    }
}
