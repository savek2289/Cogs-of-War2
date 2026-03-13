using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AdvancedEnemyAI : MonoBehaviour
{
    [System.Serializable]
    
    public enum EnemyType
    {
        Melee,
        Ranged
    }

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Search,
        Investigate
    }

    [Header("Type")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private EnemyState currentState;

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Vision")]
    [SerializeField] private float viewDistance = 18f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float closeDetectDistance = 2.5f;
    [SerializeField] private LayerMask visionMask;

    [Header("Movement")]
    [SerializeField] private float patrolRadius = 15f;
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float baseOffset = 0;

    [Header("Attack References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 20f;
    [SerializeField] private float projectileLifeTime = 5f;
    [SerializeField] private float fireColldown = 0.4f;

    [Header("Melee Attack")]
    [SerializeField] private LayerMask meleeTargetLayer;
    [SerializeField] private float meleeRange = 1.8f;      
   

    [Header("Attack")]
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float minRangedDistance = 5f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Search")]
    [SerializeField] private float searchTime = 5f;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Memory")]
    [SerializeField] private float losePlayerTime = 3f;

    [Header("Team AI")]
    [SerializeField] private float alertRadius = 12f;

     
    private NavMeshAgent agent;
    private Animator anim;


    private float patrolTimer = 0f;       
    
    private Vector3 patrolPoint;
    private Vector3 lastPlayerPosition;

    private float searchTimer;
    private float loseTimer;
    private float patrolWaitTimer;
    
 
    private bool canAttack = true;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        agent.baseOffset = baseOffset;
        agent.acceleration = 25f;
        agent.angularSpeed = 720f;
        
        agent.updateRotation = false;

        SetNewPatrolPoint();

        currentState = EnemyState.Patrol;
    }

    void Update()
    {
        VisionCheck();
        HandleState();
        HandleRotation();
         
    }

    //=====================
    // STATE MACHINE
    //=====================

    void HandleState()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                Chase();
                break;

            case EnemyState.Search:
                Search();
                break;

            case EnemyState.Attack:
                Attack();
                break;

            case EnemyState.Investigate:
                Investigate();
                break;
        }
    }

    //=====================
    // VISION
    //=====================

    void VisionCheck()
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
            loseTimer = losePlayerTime;

            if (currentState != EnemyState.Attack)
                currentState = EnemyState.Chase;

            AlertNearbyEnemies();
        }
    }

    //=====================
    // PATROL
    //=====================

    void Patrol()
    {
        agent.speed = walkSpeed;
        anim.SetFloat("Speed", 1f);  // анимация ходьбы
        anim.SetBool("IsRunning", true);

        // если агент ещё не достиг точки, двигаемся к ней
        if (Vector3.Distance(agent.destination, patrolPoint) > 0.1f)
            agent.SetDestination(patrolPoint);

        // проверяем, достиг ли агент точки
        if (HasReachedDestination())
        {
            anim.SetBool("IsRunning",false);
            patrolTimer += Time.deltaTime;

            if (patrolTimer >= patrolWaitTime)
            {
                SetNewPatrolPoint();
                patrolTimer = 0;
            }
        }
    }

    //=====================
    // CHASE
    //=====================

    void Chase()
    {
        agent.speed = runSpeed;
        agent.isStopped = false;

        if (Vector3.Distance(agent.destination, player.position) > 1f)
            agent.SetDestination(player.position);

        anim.SetFloat("Speed", 1.5f);
        anim.SetBool("IsRunning", true);

        float distance = Vector3.Distance(transform.position, player.position);

        if (enemyType == EnemyType.Melee)
        {
            if (distance <= attackDistance)
            {
                currentState = EnemyState.Attack;
                anim.SetBool("IsRunning", false);
                return;
            }
        }
        else
        {
            if (distance <= attackDistance )
            {
                currentState = EnemyState.Attack;
                anim.SetBool("IsRunning", false);
                return;
            }
        }

        if (CanSeePlayer())
        {
            loseTimer = losePlayerTime;
            lastPlayerPosition = player.position;
        }
        else
        {
            loseTimer -= Time.deltaTime;

            if (loseTimer <= 0)
            {
                currentState = EnemyState.Search;
                searchTimer = searchTime;
                agent.SetDestination(lastPlayerPosition);
            }
        }
    }

    //=====================
    // SEARCH
    //=====================

    void Search()
    {
        agent.speed = runSpeed;
        anim.SetFloat("Speed", 1.5f);  // анимация бега
        anim.SetBool("IsRunning", true);

        // идем к последней известной позиции игрока
        if (Vector3.Distance(agent.destination, lastPlayerPosition) > 0.1f)
            agent.SetDestination(lastPlayerPosition);

        // уменьшаем таймер поиска
        searchTimer -= Time.deltaTime;

        // если достигли позиции
        if (HasReachedDestination())
        {
            // если таймер истек, возвращаемся к патрулю
            if (searchTimer <= 0)
            {
                currentState = EnemyState.Patrol;
                SetNewPatrolPoint();
            }
        }
    }
    bool HasReachedDestination()
    {
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    return true;
            }
        }
        return false;
    }
    //=====================
    // INVESTIGATE
    //=====================

    void Investigate()
    {
        agent.SetDestination(lastPlayerPosition);

        if (agent.remainingDistance < 1.5f)
        {
            currentState = EnemyState.Search;
        }
    }

    //=====================
    // ATTACK
    //=====================

    void Attack()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        // проверка выхода из атаки (для дальника и ближнего)
        if ((enemyType == EnemyType.Ranged && (distance > attackDistance || !CanSeePlayer())) ||
            (enemyType == EnemyType.Melee && distance > attackDistance))
        {
            StopAllCoroutines();
            canAttack = true;
            agent.isStopped = false;
            currentState = EnemyState.Chase;
            return;
        }

        // остановка агента и поворот к игроку
        agent.isStopped = true;
        
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

        // запуск атаки
        if (canAttack)
        {
            if (enemyType == EnemyType.Ranged)
                StartCoroutine(AttackRoutine());
            else
                StartCoroutine(AttackRoutine());
        }
    }
    

    IEnumerator AttackRoutine()
    {
        canAttack = false;

        if (enemyType == EnemyType.Melee)
        {
            // Анимация атаки
            anim.Play("Attack");

            // ждем небольшой тайминг удара
            yield return new WaitForSeconds(0.3f);
            HitEnemy bullet = GetComponentInChildren<HitEnemy>();
            bullet.col.enabled = true;
            // Ждем остаток cooldown
            yield return new WaitForSeconds(attackCooldown - 0.3f);
            bullet.col.enabled = false;
        }
        else // Ranged
        {
            anim.Play("Attack");

            // небольшая пауза перед выстрелом для анимации
            yield return new WaitForSeconds(fireColldown);

            if (firePoint != null && projectilePrefab != null)
            {
                GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                
                // направление на игрока
                Vector3 dir = (player.position + Vector3.up * 1f - firePoint.position).normalized;

                if (proj.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.velocity = dir * projectileSpeed;
                }
                 
            }

            // Ждем остаток cooldown
            yield return new WaitForSeconds(attackCooldown - fireColldown);
        }

        canAttack = true;

        // Проверяем дистанцию, чтобы вернуться к Chase
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackDistance + 0.5f)
            currentState = EnemyState.Chase;
    }

    //=====================
    // TEAM AI
    //=====================

    void AlertNearbyEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, alertRadius);

        foreach (Collider col in enemies)
        {
            AdvancedEnemyAI ai = col.GetComponent<AdvancedEnemyAI>();

            if (ai != null && ai != this)
            {
                ai.ReceiveAlert(player.position);
            }
        }
    }

    public void ReceiveAlert(Vector3 playerPos)
    {
        lastPlayerPosition = playerPos;
        currentState = EnemyState.Investigate;
    }

    //=====================
    // UTIL
    //=====================

    bool CanSeePlayer()
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

    void HandleRotation()
    {
        if (currentState == EnemyState.Attack)
            return;

        Vector3 dir = agent.velocity;

        if (dir.magnitude < 0.1f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 10 * Time.deltaTime);
    }

    private void SetNewPatrolPoint()
    {
        const int maxAttempts = 20; // максимум попыток найти точку
        for (int i = 0; i < maxAttempts; i++)
        {
            // случайная точка в радиусе
            Vector3 randomPoint = Random.insideUnitSphere * patrolRadius + transform.position;

            NavMeshHit hit;
            // проверяем, что точка на NavMesh
            if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
            {
                Vector3 candidatePoint = hit.position;

                // проверяем, что агент реально сможет пройти
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(candidatePoint, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    patrolPoint = candidatePoint;
                    return; // нашли корректную точку
                }
            }
        }

        // если не нашли подходящую точку
        patrolPoint = transform.position;
        Debug.LogWarning("Не удалось найти доступную точку патруля на NavMesh!");
    }
    void OnDrawGizmosSelected()
    {
        // дистанция зрения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        // близкое обнаружение
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, closeDetectDistance);

        // радиус помощи союзников
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, alertRadius);

        // дистанция атаки
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // угол зрения
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up, left * viewDistance);
        Gizmos.DrawRay(transform.position + Vector3.up, right * viewDistance);

        // направление взгляда
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * viewDistance);

        // точка патруля
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(patrolPoint, 0.4f);
    }
}