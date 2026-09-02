using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform eyes;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Vision")]
    [SerializeField] private float visionDistance = 16.0f;
    [Range(1.0f, 180.0f)]
    [SerializeField] private float visionAngle = 95.0f;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private float loseSightDelay = 2.0f;

    [Header("Search")]
    [SerializeField] private float searchDuration = 5.0f;

    [Header("Attack")]
    [SerializeField] private float attackDistance = 1.6f;
    [SerializeField] private float attackDamage = 35.0f;
    [SerializeField] private float attackCooldown = 1.2f;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerHealth playerHealth;
    private State state = State.Dormant;
    private Vector3 lastKnownPosition;
    private int patrolIndex;
    private float lostSightTime;
    private float searchEndTime;
    private float nextAttackTime;

    public bool IsActive => state != State.Dormant;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (eyes == null)
            eyes = transform;
    }

    private void OnEnable()
    {
        MonsterNoise.Emitted += HearNoise;
    }

    private void OnDisable()
    {
        MonsterNoise.Emitted -= HearNoise;
    }

    private void Update()
    {
        if (state == State.Dormant || player == null || playerHealth == null)
            return;

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer)
        {
            state = State.Chase;
            lastKnownPosition = player.position;
            lostSightTime = 0.0f;
        }
        else if (state == State.Chase)
        {
            lostSightTime += Time.deltaTime;

            if (lostSightTime >= loseSightDelay)
                BeginInvestigation(lastKnownPosition);
        }

        switch (state)
        {
            case State.Patrol:
                UpdatePatrol();
                break;

            case State.Investigate:
                UpdateInvestigation();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Search:
                UpdateSearch();
                break;
        }
    }

    public void Activate(Transform playerTarget)
    {
        player = playerTarget;
        playerHealth = player != null
            ? player.GetComponent<PlayerHealth>()
            : null;

        if (player == null || playerHealth == null)
        {
            Debug.LogError(
                "MonsterAI: не найден Player или PlayerHealth.",
                this
            );
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "MonsterAI: враг не стоит на запечённом NavMesh.",
                this
            );
            return;
        }

        state = State.Patrol;
        agent.isStopped = false;
        GoToNextPatrolPoint();
    }

    public void SetDormant()
    {
        state = State.Dormant;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 target = player.position + Vector3.up;
        Vector3 direction = target - eyes.position;
        float distance = direction.magnitude;

        if (distance > visionDistance)
            return false;

        if (Vector3.Angle(eyes.forward, direction) > visionAngle * 0.5f)
            return false;

        if (!Physics.Raycast(
                eyes.position,
                direction.normalized,
                out RaycastHit hit,
                distance,
                visionMask,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.transform == player || hit.transform.IsChildOf(player);
    }

    private void HearNoise(Vector3 position, float radius)
    {
        if (state == State.Dormant || state == State.Chase)
            return;

        if (Vector3.Distance(transform.position, position) > radius)
            return;

        BeginInvestigation(position);
    }

    private void BeginInvestigation(Vector3 position)
    {
        state = State.Investigate;
        lastKnownPosition = position;
        agent.isStopped = false;
        agent.SetDestination(position);
    }

    private void UpdatePatrol()
    {
        if (!HasReachedDestination())
            return;

        GoToNextPatrolPoint();
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        Transform point = patrolPoints[patrolIndex];
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        if (point != null)
        {
            agent.isStopped = false;
            agent.SetDestination(point.position);
        }
    }

    private void UpdateInvestigation()
    {
        if (!HasReachedDestination())
            return;

        state = State.Search;
        searchEndTime = Time.time + searchDuration;
        agent.isStopped = true;
    }

    private void UpdateSearch()
    {
        if (Time.time < searchEndTime)
            return;

        state = State.Patrol;
        agent.isStopped = false;
        GoToNextPatrolPoint();
    }

    private void UpdateChase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);

        if (Time.time < nextAttackTime)
            return;

        if (Vector3.Distance(transform.position, player.position) > attackDistance)
            return;

        playerHealth.TakeDamage(attackDamage);
        nextAttackTime = Time.time + attackCooldown;
    }

    private bool HasReachedDestination()
    {
        if (agent.pathPending)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.2f;
    }
}
