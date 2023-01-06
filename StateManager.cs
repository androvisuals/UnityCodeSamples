using UnityEngine;
using UnityEngine.AI;

public class StateManager : MonoBehaviour
{
    [Header("Components")] 
    [SerializeField] InputControls inputControls;
    [SerializeField] Soldier soldier;
    [SerializeField] NavMeshAgent _navMeshAgent;
    [SerializeField] MoveUnit moveUnit;
    [SerializeField] Animator _animator;
    [SerializeField] FieldOfView _fov;
    [SerializeField] ProjectileGun gun;

    [Header("States")]
    public MovementState movementState;

    public bool isAiming = false;
    public bool allowedToAim = true;
    public bool isReloading = false;

    [Header("Aiming")]
    [SerializeField] float viewAngle = 120f;
    [SerializeField] float aimDistance = 60f;
    [SerializeField] float aimVelocity = 0.0f;
    [SerializeField] float aimSmoothTime = 0.25f;
    [SerializeField] float aimDistanceVelocity = 0f;

    CapsuleCollider colliderOfSoldier;
    void Start()
    {
        Setup();
    }

    void Update()
    {
        //reset animator controller to default layer and weight if not altered in the following functions
        _animator.SetLayerWeight(1, 1);

        GetAimMode();
        SetMovementState();
        SetAimingState();
        GetReloadingState();
        UpdateMovementAnimations();
    }
    void Setup()
    {
        _navMeshAgent.autoBraking = false;
        _navMeshAgent.enabled = true;
        moveUnit.navAgentSpeedModifier = 1f;
        moveUnit.navAgentAcceleration = 10f;

        movementState = MovementState.Walking;
        isAiming = false;
        colliderOfSoldier = GetComponent<CapsuleCollider>();
    }

    private void GetReloadingState()
    {
        isReloading = gun.reloading;
    }

    private void SetAimingState()
    {
        if (movementState == MovementState.Running)
        {
            allowedToAim = false;
        }
        else if (movementState == MovementState.Prone && moveUnit.velocityMagnitude > 0)
        {
            allowedToAim = false;
        }
        else
        {
            allowedToAim = true;
        }

        if (allowedToAim && isAiming)
        {
            _animator.SetBool("IsAiming", true);

            viewAngle   = gun.fovAimingAngle;
            aimDistance = gun.fovAimingDistance;
            moveUnit.navAgentSpeedModifier *= 0.75f;
        }
        else
        {
            _animator.SetBool("IsAiming", false);
            viewAngle  = 120f;
            aimDistance = 40f;
            moveUnit.navAgentSpeedModifier *= 1f;
        }

        //Smooth out jumps in Fov view angle and distance when going from aiming to not aiming
        //will be altered later by weapon attachments
        viewAngle =   Mathf.SmoothDamp(_fov.viewAngle, viewAngle, ref aimVelocity, aimSmoothTime);
        aimDistance = Mathf.SmoothDamp(_fov.viewRadius, aimDistance, ref aimDistanceVelocity, aimSmoothTime * 4f);

        _fov.viewAngle  = viewAngle;
        _fov.viewRadius = aimDistance;
    }
    private void SetMovementState()
    {
        if (movementState == MovementState.Running)
        {
            _navMeshAgent.autoBraking = true;
            moveUnit.navAgentSpeedModifier = 3.0f;
            
            _animator.SetLayerWeight(1, 0);
            
            colliderOfSoldier.center = new Vector3(0f,0.75f,0f);
            colliderOfSoldier.height = 1.75f;
            
            _animator.SetInteger("AnimStates", 2);
        }
        else if (movementState == MovementState.Walking)
        {
            _navMeshAgent.autoBraking = false;
            moveUnit.navAgentSpeedModifier = 1.0f;

            colliderOfSoldier.center = new Vector3(0f, 0.75f, 0f);
            colliderOfSoldier.height = 1.75f;

            _animator.SetInteger("AnimStates", 1);
        }
        else if (movementState == MovementState.Crouching)
        {
            _navMeshAgent.autoBraking = false;
            moveUnit.navAgentSpeedModifier = 0.5f;

            colliderOfSoldier.center = new Vector3(0f, 0.6f, 0f);
            
            colliderOfSoldier.height = 1.25f;
            _animator.SetInteger("AnimStates", 0);
        }
        else if (movementState == MovementState.Stopped)
        {
            _navMeshAgent.SetDestination(gameObject.transform.position);
            _navMeshAgent.autoBraking = true;
        }
    }
    private void GetAimMode()
    {
        if (inputControls.aimModeEnabled || _fov.closestTarget)
            isAiming = true;
        else
            isAiming = false;
    }
    private void UpdateMovementAnimations()
    {
        //these two parameters control the blend trees in the animator component
        _animator.SetFloat("VelocityZ", moveUnit.velocityZ, 0.1f, Time.deltaTime);
        _animator.SetFloat("VelocityX", moveUnit.velocityX, 0.1f, Time.deltaTime);
        // for 1d blend trees like prone backwards, idle, prone forwards
        _animator.SetFloat("VelocityMagnitude", moveUnit.velocityMagnitude, 0.1f, Time.deltaTime);
    }
}
