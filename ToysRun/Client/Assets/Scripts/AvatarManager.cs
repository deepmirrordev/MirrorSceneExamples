using System.Collections.Generic;
using UnityEngine;
using zFrame.UI;

// Manages an avatar game object's moving and syncing.
public class AvatarManager : MonoBehaviour
{
    private static bool _syncStarted = false;
    public GameObject cameraObj;
    public Joystick joystick;
    public float speed = 0.2f;
    public bool syncEnabled = true;

    private CharacterController _controller;
    private Animator _animator;
    private bool _isIdle = true;
    private bool _isSitting = false;
    private bool _isJumping = false;
    private EntityState _entityState = new();
    private readonly List<string> _animations = new() { "idle", "run", "walk", "jump", "sit_down", "sit_idle", "stand_up"};

    public void SetEntityState(ref EntityState entityState)
    {
        _entityState = entityState;
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _controller.detectCollisions = true;
        _controller.enableOverlapRecovery = true;

        _animator = GetComponent<Animator>();

        if (joystick != null)
        {
            joystick.OnValueChanged.AddListener(OnJoystickChanged);
        }

        _syncStarted = true;
    }

    private void OnJoystickChanged(Vector2 newValue)
    {
        if (newValue.magnitude != 0)
        {
            // Move self entity.
            Vector3 direction = new(newValue.x, 0, newValue.y);
            Vector3 newDirection = FollowCameraDirection(direction);
            _controller.Move(speed * Time.deltaTime * newDirection);
            //controller.SimpleMove(direction * speed * Time.deltaTime);
            gameObject.transform.rotation = Quaternion.LookRotation(newDirection);

            AnimateMovement(newValue);
        }
    }

    public void OnSitClicked()
    {
        _isIdle = true;
        if (!_isSitting)
        {
            SyncAnimationToRemote("sit_down");
            _animator.Play("sit_down");
            _isSitting = true;
        }
        else
        {
            SyncAnimationToRemote("stand_up");
            _animator.Play("stand_up");
            _isSitting = false;
        }
    }

    public void OnAnimatorSitIdle()
    {
        if (!IsRemote())
        {
            SyncAnimationToRemote("sit_idle");
        }
        _isSitting = true;
        _isJumping = false;
        _isIdle = true;
    }

    public void OnAnimatorStandIdle()
    {
        if (!IsRemote())
        {
            SyncAnimationToRemote("idle");
        }
        _isSitting = false;
        _isJumping = false;
        _isIdle = true;
    }

    public void OnJumpClicked()
    {
        SyncAnimationToRemote("jump");
        _animator.Play("jump");
        _isSitting = false;
        _isJumping = true;
        _isIdle = true;
    }

    public bool IsRemote()
    {
        return joystick == null;
    }

    private void AnimateMovement(Vector2 velocity)
    {
        _isIdle = false;
        _isSitting = false;

        if (velocity.magnitude > 0.9)
        {
            SyncAnimationToRemote("run");
            _animator.Play("run");
        }
        else
        {
            SyncAnimationToRemote("walk");
            _animator.Play("walk");
        }
    }

    private void FixedUpdate()
    {
        if (_syncStarted)
        {
            if (!IsRemote() && !joystick.IsDraging && !_isIdle)
            {
                // Self entity and become idle
                _isIdle = true;
                if (!_isSitting)
                {
                    _animator.Play("idle");
                    SyncAnimationToRemote("idle");
                }
                SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                _controller.Move(new Vector3(0, -0.1f, 0)); // gravitational pull
            }
            else if (!IsRemote() && !_isIdle)
            {
                // Self entity and not idle
                SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                _controller.Move(new Vector3(0, -0.1f, 0)); // gravitational pull
            }
            else if (IsRemote())
            {
                // Remote entities
                SyncFromRemote();
            }
        }
    }

    private void SyncFromRemote()
    {
        if (!syncEnabled)
        {
            return;
        }

        // Move remote entity.
        Vector3 newPose = _entityState.GetPosition();
        Quaternion newQuat = _entityState.GetRotation();
        gameObject.transform.SetPositionAndRotation(
            Vector3.Lerp(gameObject.transform.position, newPose, 0.3f),
            Quaternion.Lerp(gameObject.transform.rotation, newQuat, 0.7f));

        Vector3 remoteSpeed = (newPose - gameObject.transform.position) / Time.fixedDeltaTime;

        string remoteAnimation = _entityState.animationState;
        // Play remote animation.
        if (_animations.Contains(remoteAnimation))
        {

            switch(remoteAnimation)
            {
                case "idle":
                    if (remoteSpeed.magnitude > 0.01f)
                    {
                        // If idle but speed is not close to zero, continue previous animation state: run or walk.
                    }
                    else
                    {
                        _animator.Play(remoteAnimation);
                        _isJumping = false;
                        _isSitting = false;
                        _isIdle = true;
                    }
                    break;
                case "run":
                case "walk":
                    _isJumping = false;
                    _isSitting = false;
                    _isIdle = false;
                    _animator.Play(remoteAnimation);
                    break;
                case "sit_idle":
                    _isJumping = false;
                    _isSitting = true;
                    _isIdle = true;
                    _animator.Play(remoteAnimation);
                    break;
                case "sit_down":
                    if (!_isSitting)
                    {
                        _isSitting = true;
                        _animator.Play("sit_down");
                    }
                    break;
                case "stand_up":
                    if (_isSitting)
                    {
                        _isSitting = false;
                        _animator.Play("stand_up");
                    }
                    break;
                case "jump":
                    if (!_isJumping)
                    {
                        _isJumping = true;
                        _animator.Play("jump");
                    }
                    break;
            }
         }
        else
        {
            _animator.Play("idle");
            _isJumping = false;
            _isSitting = false;
        }
    }

    private void SyncPoseToRemote(Vector3 position, Quaternion rotation)
    {
        Dictionary<string, double> targetPose = new();
        targetPose["transx"] = position.x;
        targetPose["transy"] = position.y;
        targetPose["transz"] = position.z;
        targetPose["rotw"] = rotation.w;
        targetPose["rotx"] = rotation.x;
        targetPose["roty"] = rotation.y;
        targetPose["rotz"] = rotation.z;
        if (syncEnabled)
        {
            RoomManager.Instance.Room.Send("MoveAvatar", targetPose);
        }
    }

    private void SyncAnimationToRemote(string animation)
    {
        if (syncEnabled)
        {
            RoomManager.Instance.Room.Send("ChangeAnimation", animation);
        }
    }

    public void ResetState()
    {
        _syncStarted = false;
        _animator.Play("idle");
        gameObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        SyncAnimationToRemote("idle");
        SyncPoseToRemote(Vector3.zero, Quaternion.identity);
    }

    private Vector3 FollowCameraDirection(Vector3 moveDirection)
    {
        if (cameraObj != null)
        {
            Vector3 moveV3 = cameraObj.transform.rotation * moveDirection.normalized;
            moveV3.y = 0;
            return moveV3.normalized * moveDirection.magnitude;
        }
        else
        {
            return moveDirection;
        }
    }
}
