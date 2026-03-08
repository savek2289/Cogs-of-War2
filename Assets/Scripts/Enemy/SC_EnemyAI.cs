using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SC_EnemyAI : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Vision")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float closeDetectDistance = 2.5f;
    [SerializeField] private LayerMask visionMask;

    [Header("Movement")]
    [SerializeField] private float patrolRadius = 12f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private float attackDistance = 1.8f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private Transform hitPoint;
    [SerializeField] private LayerMask targetLayer;

    [Header("Search")]
    [SerializeField] private float searchTime = 4f;

    private bool canAttack = true;
    private bool getAttack = false;
    private bool hasSeenPlayer;
     
    private Vector3 patrolPoint;
    private Vector3 lastPlayerPosition;

    private float searchTimer;

    private Animator anim;
    private NavMeshAgent agent;
    
    private enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Search
    }

    [SerializeField] private EnemyState currentState;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.stoppingDistance = attackDistance;
        agent.autoBraking = true;
        agent.updateRotation = false;

        SetNewPatrolPoint();

        currentState = EnemyState.Patrol;
    }

    private void Update()
    {
        HandleVision();
        HandleState();
        HandleRotation();
        GetAttack();
    }

    // ==============================
    // STATE MACHINE
    // ==============================

    private void HandleState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Search:
                HandleSearch();
                break;
        }
        if (currentState == EnemyState.Attack)
        {
            HandleAttack();
        }
    }

    // ==============================
    // VISION
    // ==============================

    private void HandleVision()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        float distance = Vector3.Distance(transform.position, player.position);

        RaycastHit hit;

        if (!Physics.Raycast(transform.position + Vector3.up, dir, out hit, viewDistance, visionMask))
            return;

        if (hit.transform != player)
            return;

        float angle = Vector3.Angle(transform.forward, dir);

        if (distance < closeDetectDistance || angle < viewAngle * 0.5f)
        {
             
            lastPlayerPosition = player.position;
            currentState = EnemyState.Chase;
        }
    }

    // ==============================
    // PATROL
    // ==============================

    private void HandlePatrol()
    {
        agent.speed = walkSpeed;

        agent.SetDestination(patrolPoint);

        anim.SetFloat("Speed", 1f);
        anim.SetBool("IsRunning", true);

        
        if (agent.remainingDistance < 1.7f)
            SetNewPatrolPoint();
    }

    // ==============================
    // CHASE
    // ==============================

    private void HandleChase()
    {
        if (player == null) return;

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(player.position);

        // если достиг дистанции атаки
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = EnemyState.Attack;
            return;
        }
        anim.SetFloat("Speed", 1.5f);
        anim.SetBool("IsRunning", true);
        // если игрок потерян
        if (!CanSeePlayer())
        {
            lastPlayerPosition = player.position;
            currentState = EnemyState.Search;
            searchTimer = searchTime;
            agent.SetDestination(lastPlayerPosition);
        }
    }

    // ==============================
    // SEARCH
    // ==============================

    private void HandleSearch()
    {
        searchTimer -= Time.deltaTime;
        anim.SetBool("IsRunning", false);
        if (agent.remainingDistance < 1.7f)
        {
            if (searchTimer <= 0)
            {
                currentState = EnemyState.Patrol;
                SetNewPatrolPoint();
            }
        }
    }

    // ==============================
    // ATTACK
    // ==============================

    private void HandleAttack()
    {
         
        if (player == null) return;
        
        // остановка
        agent.isStopped = true;

        // поворот к игроку
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);
        }

        // запуск атаки один раз
        if (canAttack)
            StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        canAttack = false;

        // небольшая пауза перед ударом
        yield return new WaitForSeconds(0.35f);
        getAttack = true;

        anim.SetBool("IsRunning", false); 
        anim.Play("Attack");
 
        yield return new WaitForSeconds(attackCooldown);        
        getAttack = false;
        canAttack = true;

        // после атаки проверяем игрока
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackDistance + 0.2f)
            currentState = EnemyState.Chase;
    }

    private void GetAttack()
    {
        if (getAttack == true) 
        {
           
            Ray ray = new Ray(hitPoint.position, hitPoint.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 0.3f, targetLayer))
            {
                hit.collider.gameObject.SetActive(false);
                getAttack = false;
                return;
            }

            
            Debug.DrawRay(hitPoint.position, hitPoint.forward * 0.3f, Color.green);
        }

    }
    // ==============================
    // ROTATION
    // ==============================

    private void HandleRotation()
    {
        if (currentState == EnemyState.Attack)
            return; // поворот делаем в HandleAttack

        Vector3 direction = agent.velocity;
        if (direction.magnitude < 0.1f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
    }

    // ==============================
    // UTIL
    // ==============================

    private bool CanSeePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;

        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up, dir, out hit, viewDistance, visionMask))
        {
            if (hit.transform == player)
                return true;
        }

        return false;
    }

    private void SetNewPatrolPoint()
    {
        Vector3 randomPoint = Random.insideUnitSphere * patrolRadius;

        randomPoint += transform.position;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolPoint = hit.position;
        }
    }

    // ==============================
    // DEBUG
    // ==============================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeDetectDistance);
    }
}