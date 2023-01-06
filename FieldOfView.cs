using System.Collections.Generic;
using FOW;
using UnityEngine;
using Shapes;

// there needs to be a capsule collider on the target with the correct layerMask like enemy or player
// for this script to "see" other targets.

public class FieldOfView : MonoBehaviour
{
    [Header("Unit type")] 
    public Unit unit;
    
    [Header("View settings")]
    public float viewRadius;
    [SerializeField] [Range(0,360)]
    public float viewAngle;
    //Avoids Fov arc missing targets that are very close by.
    public float radiusSpottedOutsideFov = 5f;
    
    [Header("Masks")]
    public LayerMask unitMask;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public LayerMask hidingSpotMask;

    [Header("Targets List")]
    public List<Transform> visibleTargets = new List<Transform>();
    
    [Header("Current Target")]
    public Transform closestTarget;
    public Vector3 targetPosition = Vector3.zero;
    
    //where the gun/sight comes from, if this is zero it raycasts from the feet
    public Vector3 headHeight;
    public Vector3 headPosition;
    
    //set in other scripts
    public int squadNumber;
    public bool isAIDisablefadeScenery = false;
    
    Disc viewRadiusDisc;
    Disc viewAngleDisc;
    //Keystone shape for units close by outside of FOV arc
    Disc closeByDisc;
    
    FogOfWarRevealer3D fogOfWar3d;

    [SerializeField] SquadManager squadManager;
    [SerializeField] Transform headLimb;
    
    [Header("Turret reference")]
    //Turret "Head height" used for raycasting for shoulder turret equipment
    [SerializeField] Transform turretBarrelPivot;
    
    [Header("Options")]
    [SerializeField] bool debugShowTargetRays = false;
    public bool enableAllFovVisuals = false;
    void Start()
    {
        // shape components
        viewRadiusDisc = transform.Find("FovCircle").GetComponent<Disc>();
        viewAngleDisc = transform.Find("FovArc").GetComponent<Disc>();
        closeByDisc = transform.Find("CloseByDisc").GetComponent<Disc>();
        
        unit = gameObject.GetComponentInParent<Unit>();
        
        if (unit.type == Unit.Type.Droid)
        {
            squadManager = transform.parent.transform.parent.transform.parent.transform.parent.GetComponent<SquadManager>();
            unitMask = squadManager.unitMask;
            targetMask = squadManager.targetMask;
            fogOfWar3d = transform.Find("FogOfWar3d").GetComponent<FogOfWarRevealer3D>();
            
            if (fogOfWar3d != null)
            {
                viewRadiusDisc.enabled = false;
                viewAngleDisc.enabled = false;
                closeByDisc.enabled = false;

                fogOfWar3d.eyeOffset = 1.6f;
                fogOfWar3d.visionHeight = 8f;
            }
            
            if (squadManager.controlledBy == SquadManager.ControlledBy.Player) 
                enableAllFovVisuals = true;
        }
        else if(unit.type == Unit.Type.Turret)
        {
            viewRadiusDisc.enabled = false;
            viewAngleDisc.enabled = false;
            closeByDisc.enabled = false;
        }
    }

    void Update()
    {
        if (enableAllFovVisuals)
        {
            ViewRadiusSetDisc();
            ViewAngleSetDisc();
            CloseBySetDisc();
            
            if(unit.type == Unit.Type.Droid) 
                fogOfWar3d.enabled = true;
        }
        else
        {
            DisableAllFovGameObjects(unit);
        }

        FindVisibleTargets();
    }

    private void DisableAllFovGameObjects(Unit unit)
    {
        viewRadiusDisc.enabled = false;
        viewAngleDisc.enabled = false;
        closeByDisc.enabled = false;
        
        if(unit.type == Unit.Type.Droid) 
            fogOfWar3d.enabled = false;
    }

    private void LateUpdate()
    {
        //these positions must be set in late UPDATE due to animator overriding positions in normal
        //update, otherwise we raycast from two positions
        if (unit.type == Unit.Type.Droid)
        {
            //we add a tiny offset so we're not casting from within the collider or mesh
            headHeight = new Vector3(0, headLimb.position.y + 0.35f, 0);
            headPosition = headLimb.position;    
        }
        else if(unit.type == Unit.Type.Turret)
        {
            headHeight = new Vector3(0,turretBarrelPivot.localPosition.y - 0.3f,0);
            headPosition = turretBarrelPivot.position;
        }
    }


    void FindVisibleTargets()
    {
        // clear list of available targets
        visibleTargets.Clear();

        // create array of ALL enemies in radius that match the target mask which is NOT this units squad
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        // now we only have an array of matching targets not friendlies
        Transform target = null;
        Vector3 dirToTarget = Vector3.zero;
        float dstToTarget = 0f;
        bool isTargetinFovArc = false;
        
        //what unit type is the enemy
        Unit unitTarget;
        
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            // set transform of current target
            target = targetsInViewRadius[i].transform;
            unitTarget = target.GetComponent<Unit>();

            if (unitTarget.type == Unit.Type.Droid)
            {
                if (target.GetComponent<Soldier>() != null)
                {
                    if (target.GetComponent<Soldier>().health.isDead)
                    {
                        target.transform.GetComponent<RenderIfSpotted>().isSpotted = true;
                        continue;    
                    }
                }    
            }
            else if (unitTarget.type == Unit.Type.Seeker)
            {
                if (target.GetComponent<ESeeker>() != null)
                {
                    if (target.transform.GetChild(0).GetComponent<Health>().isDead)
                        continue;    
                }
            }
            
            // calculate direction to current target
            dirToTarget = (target.position - transform.position).normalized;
            
            // calculate distance to current target
            dstToTarget = Vector3.Distance(transform.position, target.position);
            
            //if its a droid we use the fov arc, else its a turret and it has all enemies in a sphere
            if (unit.type == Unit.Type.Droid)
            {
                // check if target is outside of FOV arc or outside the close spotted radius
                if (Vector3.Angle(transform.forward, dirToTarget) >= viewAngle / 2
                    && dstToTarget > radiusSpottedOutsideFov)
                    continue;    
            }
            
            // if raycast to target doesn't hit an obstacle then we have a clear line of sight and can see the target
            if (Physics.Raycast(transform.position + headHeight, dirToTarget, dstToTarget, obstacleMask))
                continue;

            //if it hits a friendly we also continue
            if (Physics.Raycast(transform.position + headHeight, dirToTarget, dstToTarget, unitMask))
                continue;

            //add visible target to list
            visibleTargets.Add(target);
                    
            if (targetMask != unitMask)
            {
                //this is set to false at the end of late update in the renderIfSpottedScript
                target.transform.GetComponent<RenderIfSpotted>().isSpotted = true;
            }

            if (debugShowTargetRays)
            {
                if (targetMask != unitMask)
                {
                    if (unitTarget.type == Unit.Type.Droid)
                    {
                        Debug.DrawRay(transform.position + headHeight, 
                            target.GetComponent<CapsuleCollider>().transform.position - 
                            transform.position, Color.green);    
                    }
                    else if (unitTarget.type == Unit.Type.Turret)
                    {
                        Debug.DrawRay(transform.position + headHeight, 
                            target.GetComponent<CapsuleCollider>().transform.position - 
                            transform.position, Color.blue);
                    }
                }
            }
        }
        
        // if no enemies are visible then set closest target to null and return 
        if (visibleTargets.Count <= 0)
        {
            closestTarget = null;
            return;
        }

        // GET THE CLOSEST TARGET , this allows us to shoot at the closest target.
        // will be replaced later by new priority class which picks a target based upon a new scoring system
        float distanceToClosestTarget = Mathf.Infinity;
        
        // loop through only visible targets from the created list
        for (int i = 0; i < visibleTargets.Count; i++)
        {
            float distanceToTarget = (visibleTargets[i].transform.position - transform.position).sqrMagnitude;
            // here we loop through all visible targets until we have the closest one only
            if (distanceToTarget < distanceToClosestTarget)
            {
                distanceToClosestTarget = distanceToTarget;
                //set the closest target from all visible targets
                closestTarget = visibleTargets[i];
            }
        }
        // NEW targetting system
        //this is used to set the aimpoint based upon the velocity of the target and where they will be
        //in the future, check health script for more details
        if(closestTarget != null)
        {
            //lets cast a ray to every visible limb and see which ones we can see
            if(closestTarget.GetComponent<Health>())
            {
                Health targetHealth = closestTarget.GetComponent<Health>();
                Unit targetUnit = closestTarget.GetComponent<Unit>();
                if (targetUnit.type == Unit.Type.Droid)
                {
                    targetPosition = targetHealth.targetPositionToAimAt;    
                }
                else if (targetUnit.type == Unit.Type.Seeker)
                {
                    targetPosition = targetHealth.targetPositionToAimAt;
                }
            }
        }
    }

    void ViewRadiusSetDisc()
    {
        viewRadiusDisc.Radius = viewRadius;
        
        if (unit.type == Unit.Type.Droid)
            fogOfWar3d.viewRadius = viewRadius;

    }
    void ViewAngleSetDisc()
    {
        viewAngleDisc.Radius = viewRadius;
        viewAngleDisc.AngRadiansStart = (-viewAngle / 2) * Mathf.Deg2Rad;
        viewAngleDisc.AngRadiansEnd = (viewAngle / 2) * Mathf.Deg2Rad;
        
        if (unit.type == Unit.Type.Droid)
            fogOfWar3d.viewAngle = viewAngle;
    }
    
    private void CloseBySetDisc()
    {
        closeByDisc.Radius = radiusSpottedOutsideFov;
        
        if (unit.type == Unit.Type.Droid)
            fogOfWar3d.unobscuredRadius = radiusSpottedOutsideFov;
    }
    //will be used later for larger units so we don't only fire at the center of the transform
    //of the mesh
    public Vector3 GetRandomPointInsideMeshCollider(MeshCollider meshCollider)
    {

        Vector3 extents = meshCollider.bounds.size;
        Vector3 point = new Vector3(
        Random.Range(-extents.x, extents.x),
        Random.Range(-extents.y, extents.y),
        Random.Range(-extents.z, extents.z)
        );

        return meshCollider.transform.TransformPoint(point);
    }
    public Vector3 GetRandomPointInsideBoxCollider(BoxCollider boxCollider)
    {
        Vector3 extents = boxCollider.size / 2f;
        Vector3 point = new Vector3(
            Random.Range(-extents.x, extents.x),
            Random.Range(-extents.y, extents.y),
            Random.Range(-extents.z, extents.z)
        );
        point += boxCollider.center;

        return boxCollider.transform.TransformPoint(point);
    }
}
