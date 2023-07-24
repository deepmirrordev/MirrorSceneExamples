using Colyseus;
using MirrorVerse;
using MirrorVerse.UI.MirrorSceneDefaultUI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using zFrame.UI;

public struct EntityInfo
{
    public EntityState entityState;
    public GameObject entityRoot;
}

// Manages a room where multiple entites can sync.
public class RoomManager : ColyseusManager<RoomManager>
{
    public GameObject femalePrefab;
    public GameObject malePrefab;
    public GameObject cameraObj;
    public GameObject gameUI;
    public GameObject chooseAvatarUI;
    public GameObject controlsUI;
    public InputField serverSpecInput;
    public Joystick joystick;
    
    private IMirrorScene _api = null;
    private bool _isSelfEntityInit = false;

    private EntityInfo _selfEntity = new();
    private readonly Dictionary<string, EntityInfo> _remoteEntities = new();
    
   
    private readonly List<EntityState> _remoteEntitiesWaitForInit = new();
    private readonly Dictionary<string, GameObject> _avatarPrefab = new();
    private string _selfAvatarGender;

    public ColyseusRoom<MyRoomState> Room { set; get; }

    protected override void Start()
    {
        base.Start();

        ColyseusSettings settings = CloneSettings();
        serverSpecInput.text = string.Format("{0}:{1}", settings.colyseusServerAddress, settings.colyseusServerPort);

        _avatarPrefab["female"] = femalePrefab;
        _avatarPrefab["male"] = malePrefab;

        InitializeClient();

        DefaultUI.Instance.onMenuFinish += OnMenuFinish;
        DefaultUI.Instance.onMenuCancel += OnMenuCancel;
        DefaultUI.Instance.HideMenu();

        if (MirrorScene.IsAvailable())
        {
            _api = MirrorScene.Get();
            Debug.Log($"MirrorScene API is available.");
        }
        else
        {
            Debug.Log($"MirrorScene API is not available.");
        }

        if (_api != null)
        {
            // Application can listen to some events.
            _api.onSceneReady += OnMirrorSceneReady;
        }
    }

    protected void Update()
    {
        TryInitRemoteEntities();
    }

    private GameObject CreateAvatarObject(ref EntityState entityState, Vector3 position, Quaternion rotation, bool isSelf = false)
    {
        GameObject avatarObject = Instantiate(_avatarPrefab[entityState.type], position, rotation);
        avatarObject.name = "entity_" + entityState.id;
        AvatarManager avatarManager = avatarObject.GetComponent<AvatarManager>();
        avatarManager.SetEntityState(ref entityState);
        if (isSelf)
        {
            avatarManager.joystick = joystick;
            avatarManager.cameraObj = cameraObj;
        }
        return avatarObject;
    }

    private void SpawnEntity(ref EntityInfo info, bool isSelf = false)
    {
        Vector3 position = info.entityState.GetPosition();
        Quaternion rotation = info.entityState.GetRotation();
        if (_avatarPrefab[info.entityState.type] != null)
        {
            info.entityRoot = CreateAvatarObject(ref info.entityState, position, rotation, isSelf);
            Debug.Log($"Spawned entity {info.entityState.id} at {info.entityState.GetPosition()}");
        }
        else
        {
            Debug.LogError("Avatar prefab not found.");
        }
    }

    // Init all entities current existed after self player connects arsession and colyseus servers
    private void InitMySelf()
    {
        // Spawn self entity.
        if (_selfEntity.entityState != null && !_isSelfEntityInit)
        {
            _isSelfEntityInit = true;
            SpawnEntity(ref _selfEntity, true);
            _selfEntity.entityState.isLocalized = true;
            Debug.Log($"Init self entity object: {_selfEntity.entityState.id}");
            // Both arsession server and colyseus server are connected and initialized
            // We can spawn gameobjects now!
            TryInitRemoteEntities();
        }
    }
    
    private void TryInitRemoteEntities()
    {
        int i = 0;
        while (i < _remoteEntitiesWaitForInit.Count)
        {
            EntityInfo newInfo = new EntityInfo();
            newInfo.entityState = _remoteEntitiesWaitForInit[i];
            SpawnEntity(ref newInfo, false);
            _remoteEntities.Add(_remoteEntitiesWaitForInit[i].id, newInfo);
            Debug.Log("Init remote entity object: " + _remoteEntitiesWaitForInit[i].id);
            _remoteEntitiesWaitForInit.RemoveAt(i);
        }
    }

    public void StartScan()
    {
        LeaveRoom();
        ClearEntities();

        DefaultUI.Instance.Restart();
        gameUI.SetActive(false);
    }

    public void ShowQrCode()
    {
        Status status = MirrorScene.Get().ShowMarker();
        if (!status.IsOk) 
        {
            Debug.Log($"Cannot show QR code.");
        }
    }

    public void OnMirrorSceneReady(StatusOr<SceneInfo> sceneInfo)
    {
        // Called by MirrorScene system when a scene has processed.
        if (sceneInfo.HasValue)
        {
            Debug.Log($"Scanned scene is ready to use. {sceneInfo.Value.status}");

        }
        else
        {
            Debug.Log($"Scanned scene failed to process.");
        }
    }

    private void OnMenuFinish()
    {
        Debug.Log($"Scene scan flow finished.");
        gameUI.SetActive(true);
        chooseAvatarUI.SetActive(true);
    }

    private void OnMenuCancel()
    {
        Debug.Log($"Scene scan flow cancelled.");
        gameUI.SetActive(true);
    }

    private void ConfigureServerSpec()
    {
        ColyseusSettings settings = CloneSettings();
        string[] parts = new string[2];
        if (!string.IsNullOrWhiteSpace(serverSpecInput.text))
        {
            parts = serverSpecInput.text.Split(":");
        }
        settings.colyseusServerAddress = parts[0];
        settings.colyseusServerPort = parts[1];
        OverrideSettings(settings);
    }

    public void JoinRoomAsFemale()
    {
        _selfAvatarGender = "female";
        JoinRoom();
    }

    public void JoinRoomAsMale()
    {
        _selfAvatarGender = "male";
        JoinRoom();
    }

    public void AvatarJump()
    {
        _selfEntity.entityRoot.GetComponent<AvatarManager>().OnJumpClicked();
    }

    public void AvatarSit()
    {
        _selfEntity.entityRoot.GetComponent<AvatarManager>().OnSitClicked();
    }

    private async void JoinRoom(Pose? initPose = null)
    {
        ConfigureServerSpec();

        chooseAvatarUI.SetActive(false);
        joystick.gameObject.SetActive(true);
        controlsUI.SetActive(true);

        ColyseusRoomAvailable[] rooms = await client.GetAvailableRooms("ar_room");

        if (!initPose.HasValue && _api != null)
        {
            StatusOr<Pose> cursorPose = _api.GetCurrentCursorPose();
            if (cursorPose.HasValue)
            {
                initPose = cursorPose.Value;
            }
        }
        if (!initPose.HasValue)
        {
            initPose = new Pose(new Vector3(0, 0, -0.3f), Quaternion.Euler(new Vector3(0, 45, 0)));
        }

        Pose pose = new();
        if (initPose.HasValue)
        {
            // For init pose, only allow Y-axis rotation.
            pose.position = initPose.Value.position;
            Vector3 euler = initPose.Value.rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            pose.rotation = Quaternion.Euler(euler);
        }

        Dictionary<string, double> startingPosition = new();
        startingPosition["x"] = pose.position.x;
        startingPosition["y"] = pose.position.y;
        startingPosition["z"] = pose.position.z;
        Dictionary<string, double> startingRotation = new();
        startingRotation["w"] = pose.rotation.w;
        startingRotation["x"] = pose.rotation.x;
        startingRotation["y"] = pose.rotation.y;
        startingRotation["z"] = pose.rotation.z;
        Dictionary<string, object> options = new();
        options.Add("position", startingPosition);
        options.Add("rotation", startingRotation);
        options.Add("type", _selfAvatarGender);

        if (rooms.Length == 0)
        {
            Room = await client.JoinOrCreate<MyRoomState>("ar_room", options);
        }
        else if (rooms.Length == 1)
        {
            Room = await client.Join<MyRoomState>("ar_room", options);
        }
        RegisterHandlers();
    }

    private void LeaveRoom()
    {
        Room?.Leave(true);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        LeaveRoom();
    }

    private void RegisterHandlers()
    {
        if (Room != null)
        {
            Room.State.entities.OnAdd += EntitiesOnAdd;
            Room.State.entities.OnRemove += EntitiesOnRemove;
            Room.State.entities.OnChange += EntitiesOnChange;
        }
    }

    private void EntitiesOnChange(string key, EntityState value)
    {
        if (_remoteEntities.ContainsKey(key))
        {
            Debug.Log($"Someone changed state: {key} {value.animationState} {value.GetPosition()}");
        }
    }

    private void EntitiesOnAdd(string key, EntityState value)
    {
        Debug.Log($"Entity ({key}) with type ({value.type}) is added to room ({Room.SessionId}).");
        bool isSelf = key.Equals(Room.SessionId);
        if (isSelf)
        {
            Debug.Log("My self joined.");
            _selfEntity.entityState = value;
            InitMySelf();
        }
        else
        {
            Debug.Log("Someone else joined.");
            // Store all entities here waiting for init.
            _remoteEntitiesWaitForInit.Add(value);
        }
    }

    private void EntitiesOnRemove(string key, EntityState value)
    {
        Debug.Log($"Someone ({key}) left.");
        if (_remoteEntities.ContainsKey(key))
        {
            Destroy(_remoteEntities[key].entityRoot);
            _remoteEntities.Remove(key);
        }
        else if (key.Equals(Room.SessionId))
        {
            if (_selfEntity.entityRoot != null)
            {
                Destroy(_selfEntity.entityRoot);
            }
            _selfEntity.entityState = null;
        }
    }

    private void ClearEntities()
    {
        if (_selfEntity.entityRoot != null)
        {
            Destroy(_selfEntity.entityRoot);
        }
        _selfEntity.entityState = null;
        _isSelfEntityInit = false;

        foreach (var entity in _remoteEntities)
        {
            Destroy(entity.Value.entityRoot);
        }
        _remoteEntities.Clear();
        _remoteEntitiesWaitForInit.Clear();
    }

    public void RebootSelf()
    {
        LeaveRoom();
        ClearEntities();

        chooseAvatarUI.SetActive(true);
        joystick.gameObject.SetActive(false);
        controlsUI.SetActive(false);
    }
}