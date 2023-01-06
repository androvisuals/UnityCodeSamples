using UnityEngine;
using UnityEngine.AI;

public class MoveUnit : MonoBehaviour
{
    [Header("Speed settings")]
    public float navAgentSpeed;
    public float navAgentSpeedModifier;
    public float navAgentAcceleration;

    [Header("Components")]
    public FieldOfView fov;
    public Transform parent;
    public Animator _animator;
    [SerializeField] StateManager stateManager;
    
    NavMeshAgent navMeshAgent;

    [Header("Movement")]
    public Vector3 movement;
    public float velocityZ;
    public float velocityX;
    public float velocityMagnitude;
    
    //use by 2D blend trees in the Animator controller
    float horizontalVelocity;
    float verticalVelocity;

    public void Awake()
    {
        _animator = GetComponent<Animator>();

    }
    // don't delete empty functions as they're called in inherited classes
    public void Start()
    {
        parent = transform.parent;
        navMeshAgent = parent.GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        navMeshAgent.radius = 0.25f;
        if (navMeshAgent.velocity == Vector3.zero) navMeshAgent.radius = 0.05f;
        
        //change avoidance priority of agent based upon velocity
        //this makes navAgents walk around agents that stand still
        if (navMeshAgent.velocity.magnitude <= 0)
            navMeshAgent.avoidancePriority = 10;
        else
            navMeshAgent.avoidancePriority = 0;

        SetMovementSpeed();
    }
    
    void SetMovementSpeed()
    {
        horizontalVelocity = navMeshAgent.velocity.x;
        verticalVelocity = navMeshAgent.velocity.z;
        
        movement = new Vector3(horizontalVelocity, 0f, verticalVelocity);

        //moving
        if (movement.magnitude > 0.0f)
        {
            movement.Normalize();
            
            navMeshAgent.speed = navAgentSpeed * navAgentSpeedModifier;
            navMeshAgent.acceleration = navAgentAcceleration;
        }

        //Used by StateManager to control 2D Blend trees in Animator component
        velocityZ = Vector3.Dot(movement.normalized, transform.forward) * navAgentSpeedModifier;
        velocityX = Vector3.Dot(movement.normalized, transform.right) * navAgentSpeedModifier;
        velocityMagnitude = movement.magnitude;
    }
}
