using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using zFrame.UI;

// Manages an avatar game object's moving and syncing.
public class AvatarManager : MonoBehaviour
{
    public GameObject cameraObj;
    public Joystick joystick;
    public float speed = 0.2f;
    public bool syncEnabled = true;
    public TextMesh nameTag;
    public float realSize = 0.1f;
    public float offNavMaxDistance = 0.05f;

    private NavMeshAgent _navMeshAgent;
    private CharacterController _controller;
    private Animator _animator;
    private EntityState _entityState = new();
    private readonly List<string> _animations = new() { "idle", "run", "walk", "jump", "sit_down", "sit_idle", "stand_up"};

    enum AvatarState
    {
        Stand,
        Sit,
        Jump,
        Move,
        Nav,
        Nav_Sit,
        Nav_Offmesh,
    }

    private AvatarState _avatarState;


    public void SetEntityState(ref EntityState entityState)
    {
        _entityState = entityState;
    }

    private void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        if (_navMeshAgent != null)
        {
            _navMeshAgent.updatePosition = true;
            _navMeshAgent.updateRotation = false;

        }
        _controller = GetComponent<CharacterController>();
        _controller.detectCollisions = true;
        _controller.enableOverlapRecovery = true;

        _animator = GetComponent<Animator>();

        // Override all values.
        if (realSize > 0)
        {
            gameObject.transform.localScale = Vector3.one * realSize;
            _controller.skinWidth = 0.2f * realSize;
            this.speed = 2f * realSize;
            if (_navMeshAgent != null)
            {
                _navMeshAgent.stoppingDistance = 0.1f * realSize;
                _navMeshAgent.speed = 1f * realSize;
            }
        }

        if (joystick != null)
        {
            joystick.OnValueChanged.AddListener(OnJoystickChanged);
        }
        _avatarState = AvatarState.Stand;
    }

    private void OnJoystickChanged(Vector2 newValue)
    {
        if (newValue.magnitude != 0)
        {
            // Move self entity.
            Vector3 direction = new(newValue.x, 0, newValue.y);
            Vector3 newDirection = FollowCameraDirection(direction);
            float steeredSpeed = Math.Max(newValue.magnitude * speed, speed * 0.75f);
            Vector3 velocity = steeredSpeed * Time.deltaTime * newDirection;
            _controller.Move(velocity);
            //controller.SimpleMove(direction * speed * Time.deltaTime);
            gameObject.transform.rotation = Quaternion.LookRotation(newDirection);
            
            AnimateMovement(steeredSpeed);
            _avatarState = AvatarState.Move;
            _navMeshAgent.enabled = false;
        }
    }

    public void OnSitClicked()
    {
        if (_avatarState != AvatarState.Sit)
        {
            SyncAnimationToRemote("sit_down");
            _animator.Play("sit_down");
            _avatarState = AvatarState.Sit;
        }
        else
        {
            SyncAnimationToRemote("stand_up");
            _animator.Play("stand_up");
            _avatarState = AvatarState.Stand;
        }
    }

    public void OnJumpClicked()
    {
        SyncAnimationToRemote("jump");
        _animator.Play("jump");
        _avatarState = AvatarState.Jump;
    }

    public void OnNavigateClicked(Pose destination)
    {
        if (_navMeshAgent != null)
        {
            _navMeshAgent.enabled = true;

            if (_navMeshAgent.isOnNavMesh)
            {
                // Turn first.
                StartCoroutine(TurnToDestination(0, destination.position));
                // Then start nav.
                StartCoroutine(NavAfterTurn(0.05f, destination.position, AvatarState.Nav));
            }
            else
            {
                // Not on nav mesh, means user used joystick before.
                NavMeshHit navHit;
                // Search with the offNavMaxDistance radius to find closest navitable position and try to move to the that position before navigation.
                if (NavMesh.SamplePosition(gameObject.transform.position, out navHit, offNavMaxDistance, -1))
                {
                    // Turn to hit point first.
                    StartCoroutine(TurnToDestination(0, navHit.position));
                    // Start fake nav to the closest nav mesh first.
                    _avatarState = AvatarState.Nav_Offmesh;
                    float gap = (navHit.position - transform.position).magnitude;
                    float duration = gap / speed;
                    // Then start real nav to destination.
                    StartCoroutine(NavAfterTurn(duration + 0.05f, destination.position, AvatarState.Nav));
                }
                else
                {
                    Debug.Log($"Avatar is far from nav mesh. Skip.");
                }
            }
        }
        else
        { 
            Debug.Log($"NavMeshAgent not available. Skip.");
        }
    }

    public void OnNavigateSitClicked(Pose destination, float chairRadius)
    {
        // Similar to OnNavigateClicked, but with chair considered.
        if (_navMeshAgent != null)
        {
            // Do not walk into chair. Calculate the desitation with chair radius.
            float distance = (destination.position - gameObject.transform.position).magnitude;
            Vector3 adjustedDestination = Vector3.Lerp(gameObject.transform.position, destination.position, 1 - chairRadius / distance);

            _navMeshAgent.enabled = true;

            if (_navMeshAgent.isOnNavMesh)
            {
                // Turn first.
                StartCoroutine(TurnToDestination(0, destination.position));
                // Nav_Sit state will first do Nav then sit.
                StartCoroutine(NavAfterTurn(0.05f, adjustedDestination, AvatarState.Nav_Sit));
            }
            else
            {
                NavMeshHit navHit;
                // Search with the offNavMaxDistance radius to find closest navitable position and try to move to the that position before navigation.
                if (NavMesh.SamplePosition(gameObject.transform.position, out navHit, offNavMaxDistance, -1))
                {
                    // Turn to hit point first.
                    StartCoroutine(TurnToDestination(0, navHit.position));
                    // Start fake nav to the closest nav mesh first.
                    _avatarState = AvatarState.Nav_Offmesh;
                    float gap = (navHit.position - gameObject.transform.position).magnitude;
                    float duration = gap / _navMeshAgent.speed;
                    // Then start real nav to destination.
                    StartCoroutine(NavAfterTurn(duration + 0.05f, adjustedDestination, AvatarState.Nav_Sit));
                }
                else
                {
                    Debug.Log($"Avatar is far from nav mesh. Skip.");
                }
            }
        }
        else
        {
            Debug.Log($"NavMeshAgent not available. Skip.");
        }
    }

    public void OnAnimatorTransitioned(string animationState)
    {
        if (!IsRemote())
        {
            SyncAnimationToRemote(animationState);
        }

        if (animationState == "sit_idle")
        {
            _avatarState = AvatarState.Sit;
        }
        else if (animationState == "idle")
        {
            _avatarState = AvatarState.Stand;
        }
    }

    public bool IsRemote()
    {
        return joystick == null;
    }

    private void AnimateMovement(float steeredSpeed)
    {
        if (steeredSpeed / speed > 0.9)
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

    private IEnumerator TurnToDestination(float waitSec, Vector3 destination)
    {
        yield return new WaitForSeconds(waitSec);
        Vector3 direction = destination - gameObject.transform.position;
        float angle = Vector3.Angle(transform.forward, direction);
        gameObject.transform.Rotate(Vector3.up, angle);
        SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
    }

    private IEnumerator NavAfterTurn(float waitSec, Vector3 destination, AvatarState avatarState)
    {
        yield return new WaitForSeconds(waitSec);
        _avatarState = avatarState;
        _navMeshAgent.SetDestination(destination);
        _navMeshAgent.isStopped = false;
        SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
    }


    private IEnumerator TurnBackAfterNav(float waitSec)
    {
        yield return new WaitForSeconds(waitSec);
        // Turn back. No turn around animation.
        gameObject.transform.Rotate(Vector3.up, 180);
        SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
    }

    private IEnumerator SitAfterTurnBack(float waitSec)
    {
        yield return new WaitForSeconds(waitSec);
        OnSitClicked();
    }

    private void FixedUpdate()
    {
        if (!IsRemote())
        {
            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh && !_navMeshAgent.isStopped)
            {
                // Navigating
                if (_avatarState == AvatarState.Nav || _avatarState == AvatarState.Nav_Sit)
                {
                    // Keep navigating.
                    float remainingDistance = (_navMeshAgent.destination - transform.position).magnitude;
                    if (remainingDistance < Math.Max(realSize * 0.2f, 0.05f))
                    {
                        // Navigating but very close to destination. Stop.
                        _navMeshAgent.isStopped = true;

                        if (_avatarState == AvatarState.Nav_Sit)
                        {
                            // Transition to turn back and sit.
                            StartCoroutine(TurnBackAfterNav(0.1f));
                            StartCoroutine(SitAfterTurnBack(0.3f));
                        }
                        else
                        {
                            // Otherwise stand idle.
                            _avatarState = AvatarState.Stand;
                        }
                        _animator.Play("idle");
                        SyncAnimationToRemote("idle");
                        SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                    }
                    else
                    {
                        // Navigating
                        NavigationRotation();
                        AnimateMovement(_navMeshAgent.velocity.magnitude);
                        SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                    }
                }
                else
                {
                    // Navigating, but starts doing somthing else. Stop nav.
                    _navMeshAgent.isStopped = true;
                }
            }
            else
            {
                // No nav.
            }

            // Self entity is moving
            if (_avatarState == AvatarState.Move)
            {
                // User is not draging
                if (!joystick.IsDraging)
                {
                    // If not dragging any more, stop local and sync to remote.
                    _avatarState = AvatarState.Stand;
                    _animator.Play("idle");
                    SyncAnimationToRemote("idle");
                    SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                    _controller.Move(new Vector3(0, -0.1f, 0)); // gravitational pull
                }
                else
                {
                    // Otherwise keep moving, and sync to remote.
                    SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                    _controller.Move(new Vector3(0, -0.1f, 0)); // gravitational pull
                }
            }

            // Self entity is nav off mesh, moving it to nav mesh with nav speed.
            if (_avatarState == AvatarState.Nav_Offmesh)
            {
                Vector3 direction = gameObject.transform.rotation.eulerAngles;
                float steeredSpeed = _navMeshAgent.speed;
                Vector3 velocity = steeredSpeed * Time.deltaTime * direction;
                _controller.Move(velocity);
                //controller.SimpleMove(direction * speed * Time.deltaTime);
                AnimateMovement(steeredSpeed);
                SyncPoseToRemote(gameObject.transform.position, gameObject.transform.rotation);
                _controller.Move(new Vector3(0, -0.1f, 0)); // gravitational pull
            }
        }
        else
        {
            // Remote entities, sync from remote side.
            SyncFromRemote();
        }


        // Name tag billboard.
        if (nameTag != null && Camera.main != null)
        {
            nameTag.transform.LookAt(Camera.main.transform, Vector3.up);
            nameTag.transform.Rotate(new Vector3(0, 180, 0));
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

        // Play remote animation.
        Vector3 remoteSpeed = (newPose - gameObject.transform.position) / Time.fixedDeltaTime;
        string remoteAnimation = _entityState.animationState;

        if (_animations.Contains(remoteAnimation))
        {
            switch(remoteAnimation)
            {
                case "idle":
                    // TODO: If speed is still not close to zero, there maybe a slightly "sliding" if directly set to idle.
                    _avatarState = AvatarState.Stand;
                    _animator.Play("idle");
                    break;
                case "run":
                    _avatarState = AvatarState.Move;
                    _animator.Play("run");
                    break;
                case "walk":
                    _avatarState = AvatarState.Move;
                    _animator.Play("walk");
                    break;
                case "sit_idle":
                    _avatarState = AvatarState.Sit;
                    _animator.Play("sit_idle");
                    break;
                case "sit_down":
                    if (_avatarState != AvatarState.Sit)
                    {
                        _avatarState = AvatarState.Sit;
                        _animator.Play("sit_down");
                    }
                    break;
                case "stand_up":
                    if (_avatarState == AvatarState.Sit)
                    {
                        _avatarState = AvatarState.Stand;
                        _animator.Play("stand_up");
                    }
                    break;
                case "jump":
                    if (_avatarState != AvatarState.Jump)
                    {
                        _avatarState = AvatarState.Jump;
                        _animator.Play("jump");
                    }
                    break;
            }
         }
        else
        {
            // Unknown animation, treat it as idle.
            _avatarState = AvatarState.Stand;
            _animator.Play("idle");
        }
    }

    private void NavigationRotation()
    {
        _animator.SetFloat("speed", _navMeshAgent.velocity.magnitude);
        if (_navMeshAgent.velocity.magnitude / _navMeshAgent.desiredVelocity.magnitude > realSize * 1f)
        {
            gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation,
                Quaternion.LookRotation(_navMeshAgent.velocity), 1f);
        }
    }

    private void SyncPoseToRemote(Vector3 position, Quaternion rotation)
    {
        if (!syncEnabled)
        {
            return;
        }
        Dictionary<string, double> targetPose = new();
        targetPose["transx"] = position.x;
        targetPose["transy"] = position.y;
        targetPose["transz"] = position.z;
        targetPose["rotw"] = rotation.w;
        targetPose["rotx"] = rotation.x;
        targetPose["roty"] = rotation.y;
        targetPose["rotz"] = rotation.z;
        RoomManager.Instance.Room.Send("MoveAvatar", targetPose);
    }

    private void SyncAnimationToRemote(string animation)
    {
        if (!syncEnabled)
        {
            return;
        }
        RoomManager.Instance.Room.Send("ChangeAnimation", animation);
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
