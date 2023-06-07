using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Bean : MonoBehaviour
{
    #region Variables
    public PlayerRig playerRig;
    public bool debugBean = false;
    public GameObject debugPoint;

    [Header("Bean Info")]
    public bool alive = true;
    public Color teamColor;
    public string teamID()
    {
        return ColorUtility.ToHtmlStringRGB( teamColor );
    }
    public float beanSpeed;
    public float beanHealth;
    private float currentHealth;

    Vector3[] offsets = new Vector3[4];


    [Header("Combat")]
    public float attackRange;
    private bool targeting = false;
    public Bean targetedBean;
    public float damage;
    public float shootForce;
    public float attackCoolDown = 1;
    public int beanAccuracy = 5;

    [Header("Group")]
    public bool grouped = false;
    public Bean groupLeader;
    public List<Bean> groupedBeans = new List<Bean>();

    [Header("Movement")]
    public Vector3 targetPostion;
    public float targetDiscretion;
    public float rotationSpeed;

    Vector3 moveToPostion;
    [SerializeField]float distanceToTarget;
    [SerializeField]private float currentVelocity;
    public bool moving = false;
    private bool reachedTarget = true;

    Vector3 direction;
    Quaternion lookRotation;

    [HideInInspector]public NavMeshAgent navMeshAgent;

    [Header("GameObjects")]
    public Transform unit;
    public Transform gun;
    public GameObject projectile;

    public Transform selectedCircle;
    public SpriteRenderer unitIcon;
    public Material SelectionCircleMaterial;
    [HideInInspector]public DrawNavPath drawNavPath;

    public bool possessed = false;
    #endregion

    #region Unity Methods
    private void Awake() 
    {
        targetPostion = transform.position;
        navMeshAgent = GetComponent<NavMeshAgent>();
        drawNavPath = GetComponent<DrawNavPath>();
        currentHealth = beanHealth;
        selectedCircle.GetComponentInChildren<SpriteRenderer>().material = SelectionCircleMaterial;
    }
    private void Start() 
    {
        targetPostion = transform.position;
        lookRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {   
        if (alive)
        {
            HandleUnitDisplay();
            HandleGrouping();
            HandleMovement();
            HandleRotation();
            HandleTargeting();
            HandleAttacking();
            HandleHealth();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Handlers
    void HandleUnitDisplay()
    {
        if (teamColor.a != 0.5f) teamColor.a = 0.5f;
        if (playerRig != null)
        {
            var sprite = selectedCircle.GetComponentInChildren<SpriteRenderer>();
            sprite.color = teamColor;

            if (!possessed) sprite.color = teamColor;
            else sprite.color = Color.yellow;

            teamColor = playerRig.teamColour;
            if (playerRig.selectedUnits.Contains(this)) 
            {
                if (grouped && groupLeader != this) selectedCircle.gameObject.SetActive(false);
                else selectedCircle.gameObject.SetActive(true);
            }
            else selectedCircle.gameObject.SetActive(false);

            float zoom = Mathf.Clamp((Mathf.Abs(playerRig.newZoom.y) / 10) / 2, 1, 10);

            if (grouped && groupLeader != this) unitIcon.enabled = false;
            else 
            {
                unitIcon.enabled = true;
                unitIcon.transform.localScale = Vector2.Lerp(unitIcon.size, new Vector2(zoom, zoom), 1);

                float transparencyPercentage = 1.0f - (1f - 1f / (1f+playerRig.newZoom.y) * beanHealth);
                //Debug.Log ( "transparencyPercentage : " + transparencyPercentage );
                
                var spriteRenderer = unitIcon.GetComponent<SpriteRenderer>();
                var iconColorWithAlpha = teamColor;
                iconColorWithAlpha.a = transparencyPercentage;

                spriteRenderer.color = iconColorWithAlpha;
            }
        }
        else 
        {
            var spriteRenderer = unitIcon.GetComponent<SpriteRenderer>();
            spriteRenderer.color = teamColor;

            selectedCircle.gameObject.SetActive(false);
        }

        unitIcon.transform.LookAt(Camera.main.transform.position, -Vector3.up);
    }

    void HandleGrouping()
    {
        if (grouped)
        {
            if (groupLeader == this)
            {
                //This is a group leader
            }
            else
            {
                if (drawNavPath.visible && !debugBean) drawNavPath.visible = false;
                var ogTargetPos = targetPostion;
                targetPostion = groupLeader.transform.position;

                //List<Vector3> positionList = GetPositionListBeside(targetPostion,10, 10);


                /*
                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(-1, 0, -1),
                    new Vector3(-1, 0, 1),
                    new Vector3(1, 0, -1),
                    new Vector3(1, 0, 1)
                };

                targetPostion = (groupLeader.transform.position + offsets[groupedBeans.IndexOf(this)]);

                
                //This a group member, follow group leader
                var _points = Formation.EvaluatePoints().ToList();

                var index = groupedBeans.FindIndex(b => b == this);
                if (index != -1) targetPostion = _points[index];
                */
            }
        }
        else
        {
            if (!drawNavPath.visible) drawNavPath.visible = true;
        }
    }

    void HandleMovement()
    {
        if (moveToPostion != targetPostion)
        {
            reachedTarget = false;
            moveToPostion = targetPostion;
            navMeshAgent.speed = beanSpeed;
            navMeshAgent.destination = moveToPostion;
        } 

        distanceToTarget = Vector3.Distance(transform.position, navMeshAgent.destination);
        distanceToTarget = Mathf.Round(distanceToTarget);

        if (distanceToTarget <= targetDiscretion && !navMeshAgent.isStopped) 
        {
            reachedTarget = true;
            if (!navMeshAgent.isStopped) navMeshAgent.isStopped = true;
            moving = false;
        }

        if (distanceToTarget > targetDiscretion && !reachedTarget)
        {
            if (navMeshAgent.isStopped) navMeshAgent.isStopped = false; 
            currentVelocity = Mathf.Abs(navMeshAgent.velocity.x);
            moving = true;
        }
    }

    void HandleRotation()
    {
        if (!possessed)
        {   
            if (targeting)
            {
                direction = (targetedBean.transform.position - transform.position).normalized;
            }   
            else
            {
                LineRenderer lineRenderer = GetComponent<LineRenderer>();

                if (moving && lineRenderer.positionCount >= 2) direction = (GetComponent<LineRenderer>().GetPosition(1) - unit.position).normalized;
                else direction = (targetPostion - transform.position).normalized;
            }     
            
        }
        else
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        
            if(Physics.Raycast(ray,out hit))
            {
                direction = (new Vector3(hit.point.x, unit.position.y, hit.point.z) - unit.position).normalized;
            }
        }

        if (direction != Vector3.zero) lookRotation = Quaternion.LookRotation(direction);
        
        lookRotation.z = 0;
        lookRotation.x = 0;

        if (GetEasyAngle(unit.rotation.eulerAngles.x) != GetEasyAngle(lookRotation.eulerAngles.y)) unit.rotation = Quaternion.Slerp(unit.rotation, lookRotation, Time.deltaTime * rotationSpeed); 
    }

    void HandleTargeting()
    {
        //Bean, distance
        List<Tuple<Bean, float>> targets = new List<Tuple<Bean, float>>();

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.transform.tag == "Bean")
            {
                Bean inRangeBean = hitCollider.GetComponentInParent<Bean>();
                if (inRangeBean.teamID() != teamID() && inRangeBean.alive)
                {
                    targets.Add(new Tuple<Bean, float>(inRangeBean, Vector3.Distance(transform.position, inRangeBean.transform.position)));
                }
            }
        }

        if (targets.Count != 0)
        {
            float minInRangeBean = targets.Min(x => x.Item2);
            Bean targetBean = targets.First(x => x.Item2 == minInRangeBean).Item1;

            var rayDirection = targetBean.transform.position - transform.position;
            RaycastHit hit;
        
            if(Physics.Raycast(transform.position, rayDirection, out hit))
            {
                if (hit.transform.tag == "Bean" && targets.Any(x => x.Item1 == hit.transform.GetComponentInParent<Bean>())) 
                {
                    Bean bean = hit.transform.GetComponentInParent<Bean>();

                    targeting = true;
                    targetedBean = bean;
                }
                else 
                {
                    targetedBean = null;
                    targeting = false;
                }
            }
        }
        else
        {
            targetedBean = null;
            targeting = false;
        }
    }
    private float timer;
    void HandleAttacking()
    {
        if (targetedBean != null)
        {
            timer += Time.deltaTime;
 
            if (timer >= attackCoolDown)  // if using ammo, add (&& ammo >= 1)
            {
                ShootProjectile();
            }
        }
    }

    public float multipler = 360;
    void HandleHealth()
    {
        if (beanHealth != currentHealth)
        {
            Debug.Log(gameObject.name + ": I'm hit!");
            currentHealth = beanHealth;

            float flippedHealth = 100 - currentHealth;
            float angleRef = (360/100);
            float angleToRotate = angleRef * flippedHealth; //or whatever you called it
            
            selectedCircle.GetComponentInChildren<SpriteRenderer>().material.SetFloat("_Arc1", angleToRotate);
        }

        if (beanHealth <= 0)
        {
            alive = false;
        }
    }
    #endregion

    #region Command Methods
    public void Halt()
    {
        targetPostion = transform.position;
    }

    public void Move(Vector3 to)
    {
        if (grouped && groupLeader != this)
        {
            groupLeader.Move(to);
        }
        else targetPostion = to;
    }
    #endregion

    #region Helpers
    List<Vector3> GetPositionListBeside(Vector3 startPosition, float distance, int positionCount)
    {
        List<Vector3> positionList = new List<Vector3>();
        for (int i = 0; i < positionCount; i++)
        {
            float angle = i * (360f / positionCount);
        }
        return positionList;
    }

    private Vector3 targetDirection;
    private float targetSpeed;
    void ShootProjectile()
    {
        float targetDistance = Vector3.Distance(transform.position, targetedBean.transform.position);
        float projectileSpeed = shootForce;
        float projectileTimeToTarget = distanceToTarget / shootForce;
        float projectedTargetTravelDistance = targetSpeed * projectileTimeToTarget;

        Vector3 projectedTarget = targetedBean.transform.position + targetDirection * projectedTargetTravelDistance;
        Vector3 inaccurateProjectedTarget = VectorSpread(projectedTarget, beanAccuracy);

        projectedTarget.y += 0.6f; //aim at center of target if 2m high
 
        GameObject go = Instantiate(projectile, gun.transform.position, Quaternion.identity);
        go.transform.LookAt(inaccurateProjectedTarget);

        Projectile proj = go.GetComponent<Projectile>();
        proj.speed = shootForce;
        proj.owner = gameObject;
        proj.damage = damage;
        proj.targetPosition = inaccurateProjectedTarget;
 
        timer = 0f;
    }

    public Vector3 VectorSpread(Vector3 origVector, int accuracy)
    {
        float myIntx = (float)UnityEngine.Random.Range(-accuracy,accuracy)/1000;
        float myInty = (float)UnityEngine.Random.Range(-accuracy,accuracy)/1000;
        float myIntz = (float)UnityEngine.Random.Range(-accuracy,accuracy)/1000;
        Vector3 newVector = new Vector3(origVector.x + myIntx, origVector.y + myInty, origVector.z + myIntz);
        if (debugBean) Instantiate(debugPoint, newVector, Quaternion.identity);
        return newVector;
    }

    float GetEasyAngle(float eularAngle)
    {
        return  Mathf.Round(eularAngle * 100f) / 100f;
    }
    #endregion
}
