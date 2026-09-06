using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class MonsterAI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player root. If empty, an object with the Player tag is used.")]
    [SerializeField] private Transform player;

    [Tooltip("Point used as the monster's face or chest position.")]
    [SerializeField] private Transform aimPoint;

    [Tooltip("Animator from the monster model. It is found in children when empty.")]
    [SerializeField] private Animator animator;

    [Header("Activation")]
    [Tooltip("Enable only for testing. The tripwire will call Activate later.")]
    [SerializeField] private bool startActive;

    [Header("Navigation")]
    [Min(0.1f)]
    [SerializeField] private float chaseSpeed = 4.2f;

    [Min(0.02f)]
    [SerializeField] private float pathRefreshInterval = 0.15f;

    [Min(0.1f)]
    [SerializeField] private float navMeshSnapDistance = 2.0f;

    [Header("Attack")]
    [Min(0.1f)]
    [SerializeField] private float attackDistance = 1.6f;

    [Min(1)]
    [SerializeField] private int hitsToKill = 3;

    [Min(0.0f)]
    [Tooltip("Delay from the animation start to the damage moment.")]
    [SerializeField] private float attackHitDelay = 0.35f;

    [Min(0.05f)]
    [SerializeField] private float attackCooldown = 1.2f;

    [Tooltip("Layers that can block a close-range attack, usually Environment.")]
    [SerializeField] private LayerMask attackObstacleMask;

    [Header("Torch Repel")]
    [Min(0.1f)]
    [SerializeField] private float defaultRetreatDistance = 4.0f;

    [Min(0.1f)]
    [SerializeField] private float defaultRetreatDuration = 1.5f;

    [Header("Animator Parameters")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string chasingParameter = "IsChasing";
    [SerializeField] private string attackParameter = "Attack";
    [SerializeField] private string repelledParameter = "Repelled";

    private NavMeshAgent agent;
    private PlayerHealth playerHealth;
    private MonsterState currentState = MonsterState.Dormant;
    private Coroutine attackRoutine;
    private float nextPathRefreshTime;
    private float retreatEndTime;

    private int speedHash;
    private int chasingHash;
    private int attackHash;
    private int repelledHash;

    private bool hasSpeedParameter;
    private bool hasChasingParameter;
    private bool hasAttackParameter;
    private bool hasRepelledParameter;

    public MonsterState CurrentState => currentState;
    public bool IsActive => currentState != MonsterState.Dormant;

    public Vector3 AimPosition => aimPoint != null
        ? aimPoint.position
        : transform.position + Vector3.up;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (aimPoint == null)
            aimPoint = transform;

        if (animator != null)
        {
            animator.applyRootMotion = false;
            CacheAnimatorParameters();
        }

        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    private void Start()
    {
        ResolvePlayer();

        if (startActive)
            Activate();
        else
            SetDormant();
    }

    private void OnDisable()
    {
        if (attackRoutine == null)
            return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private void Update()
    {
        if (currentState == MonsterState.Dormant)
        {
            UpdateAnimator();
            return;
        }

        if (!ResolvePlayer() || playerHealth.IsDead)
        {
            SetDormant();
            return;
        }

        if (!EnsureAgentOnNavMesh())
        {
            UpdateAnimator();
            return;
        }

        switch (currentState)
        {
            case MonsterState.Chasing:
                UpdateChasing();
                break;

            case MonsterState.Attacking:
                FacePlayer();
                break;

            case MonsterState.Repelled:
                UpdateRepelled();
                break;
        }

        UpdateAnimator();
    }

    public void Activate()
    {
        if (!ResolvePlayer())
        {
            Debug.LogError(
                "MonsterAI: Player or PlayerHealth was not found.",
                this
            );
            return;
        }

        if (!EnsureAgentOnNavMesh())
        {
            Debug.LogError(
                "MonsterAI: The monster could not be placed on a baked NavMesh.",
                this
            );
            return;
        }

        currentState = MonsterState.Chasing;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        nextPathRefreshTime = 0.0f;
    }

    public void SetDormant()
    {
        CancelAttack();
        currentState = MonsterState.Dormant;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        UpdateAnimator();
    }

    public bool RepelFromTorch(Vector3 sourcePosition)
    {
        return RepelFromTorch(
            sourcePosition,
            defaultRetreatDistance,
            defaultRetreatDuration
        );
    }

    /// <summary>
    /// Makes the monster retreat using custom distance and duration values.
    /// </summary>
    public bool RepelFromTorch(
        Vector3 sourcePosition,
        float retreatDistance,
        float retreatDuration)
    {
        if (!IsActive || !EnsureAgentOnNavMesh())
            return false;

        Vector3 awayDirection = transform.position - sourcePosition;
        awayDirection.y = 0.0f;

        if (awayDirection.sqrMagnitude < 0.001f)
            awayDirection = -transform.forward;

        awayDirection.Normalize();

        Vector3 requestedPosition = transform.position +
                                    awayDirection * Mathf.Max(0.1f, retreatDistance);

        if (!NavMesh.SamplePosition(
                requestedPosition,
                out NavMeshHit hit,
                navMeshSnapDistance,
                agent.areaMask))
        {
            return false;
        }

        CancelAttack();
        currentState = MonsterState.Repelled;
        retreatEndTime = Time.time + Mathf.Max(0.1f, retreatDuration);

        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(hit.position);

        if (animator != null && hasRepelledParameter)
            animator.SetTrigger(repelledHash);

        return true;
    }

    private void UpdateChasing()
    {
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distanceToPlayer <= attackDistance && HasClearAttackLine())
        {
            BeginAttack();
            return;
        }

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        if (Time.time < nextPathRefreshTime)
            return;

        nextPathRefreshTime = Time.time + pathRefreshInterval;
        agent.SetDestination(player.position);
    }

    private void BeginAttack()
    {
        if (attackRoutine != null)
            return;

        attackRoutine = StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        currentState = MonsterState.Attacking;
        agent.isStopped = true;
        agent.ResetPath();

        FacePlayer();

        if (animator != null && hasAttackParameter)
            animator.SetTrigger(attackHash);

        if (attackHitDelay > 0.0f)
            yield return new WaitForSeconds(attackHitDelay);

        if (CanDamagePlayer())
        {
            float damage = playerHealth.MaximumHealth /
                           Mathf.Max(1, hitsToKill);

            damage += 0.001f;
            playerHealth.TakeDamage(damage);
        }

        float remainingCooldown = Mathf.Max(
            0.0f,
            attackCooldown - attackHitDelay
        );

        if (remainingCooldown > 0.0f)
            yield return new WaitForSeconds(remainingCooldown);

        attackRoutine = null;

        if (playerHealth == null || playerHealth.IsDead)
        {
            SetDormant();
            yield break;
        }

        currentState = MonsterState.Chasing;
        agent.isStopped = false;
    }

    private bool CanDamagePlayer()
    {
        if (player == null || playerHealth == null || playerHealth.IsDead)
            return false;

        float distance = Vector3.Distance(transform.position, player.position);

        return distance <= attackDistance + 0.35f && HasClearAttackLine();
    }

    private bool HasClearAttackLine()
    {
        if (attackObstacleMask.value == 0)
            return true;

        Vector3 target = player.position + Vector3.up;

        return !Physics.Linecast(
            AimPosition,
            target,
            attackObstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateRepelled()
    {
        if (Time.time < retreatEndTime &&
            (agent.pathPending || agent.remainingDistance > agent.stoppingDistance))
        {
            return;
        }

        currentState = MonsterState.Chasing;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        nextPathRefreshTime = 0.0f;
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private bool ResolvePlayer()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
            return false;

        if (playerHealth == null)
        {
            playerHealth = player.GetComponentInParent<PlayerHealth>();

            if (playerHealth == null)
                playerHealth = player.GetComponentInChildren<PlayerHealth>();
        }

        return playerHealth != null;
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                navMeshSnapDistance,
                NavMesh.AllAreas))
        {
            return false;
        }

        return agent.Warp(hit.position);
    }

    private void CancelAttack()
    {
        if (attackRoutine == null)
            return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private void CacheAnimatorParameters()
    {
        speedHash = Animator.StringToHash(speedParameter);
        chasingHash = Animator.StringToHash(chasingParameter);
        attackHash = Animator.StringToHash(attackParameter);
        repelledHash = Animator.StringToHash(repelledParameter);

        hasSpeedParameter = HasAnimatorParameter(
            speedParameter,
            AnimatorControllerParameterType.Float
        );

        hasChasingParameter = HasAnimatorParameter(
            chasingParameter,
            AnimatorControllerParameterType.Bool
        );

        hasAttackParameter = HasAnimatorParameter(
            attackParameter,
            AnimatorControllerParameterType.Trigger
        );

        hasRepelledParameter = HasAnimatorParameter(
            repelledParameter,
            AnimatorControllerParameterType.Trigger
        );
    }

    private bool HasAnimatorParameter(
        string parameterName,
        AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName &&
                parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        if (hasSpeedParameter)
        {
            float speed = agent != null && chaseSpeed > 0.0f
                ? Mathf.Clamp01(agent.velocity.magnitude / chaseSpeed)
                : 0.0f;

            animator.SetFloat(speedHash, speed, 0.12f, Time.deltaTime);
        }

        if (hasChasingParameter)
        {
            bool isChasing = currentState == MonsterState.Chasing ||
                              currentState == MonsterState.Attacking;

            animator.SetBool(chasingHash, isChasing);
        }
    }
}
