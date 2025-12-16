using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class CC_NavAI_RootPatrol : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform player;

    [Header("Patrol")]
    public Vector3 patrolCenter;
    public float patrolRadius = 8f;

    [Header("Random Idle/Walk")]
    public float idleMin = 0.8f;
    public float idleMax = 2.0f;
    public float walkMin = 1.2f;
    public float walkMax = 3.0f;

    [Header("Detect/Chase")]
    public float detectRange = 10f;
    public float loseRange = 14f;
    public float repathInterval = 0.2f;
    public float stopDistance = 1.7f;

    [Header("Look/Rotate")]
    public float rotateSpeed = 10f;
    public float lookChangeIntervalMin = 0.4f;
    public float lookChangeIntervalMax = 1.2f;

    [Header("Ground")]
    public float gravity = -9.81f;

    private CharacterController cc;
    private NavMeshAgent agent;

    private float verticalVel;

    private enum State { Idle, Walk, Chase, Attack } // Attack 추가
    private State state;

    private float stateEndTime;
    private float nextLookTime;
    private float nextRepathTime;

    private Quaternion desiredLookRot;
    private bool isWalking;
    private bool isRunning;

    private readonly int HashSpeed = Animator.StringToHash("Speed");

    [Header("Attack")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.2f;

    private float nextAttackTime;

    private readonly int HashAttack = Animator.StringToHash("Attack"); // Trigger
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        patrolCenter = transform.position;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }

        desiredLookRot = transform.rotation;
    }

    void Start()
    {
        PickNewPatrolPoint();
        EnterIdle();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        ApplyGravity(dt);

        if (state != State.Chase && player != null && DistToPlayer() <= detectRange)
            EnterChase();

        if (state == State.Chase)
        {
            float dist = DistToPlayer();

            if (dist <= attackRange && Time.time >= nextAttackTime)
            {
                EnterAttack();
            }
            else
            {
                UpdateChase(dt);

                if (player == null || dist >= loseRange)
                    EnterIdle();
            }
        }
        else if (state == State.Attack)
        {
            UpdateAttack(dt);
        }
        else
        {
            if (Time.time >= stateEndTime)
            {
                if (state == State.Idle) EnterWalk();
                else EnterIdle();
            }

            if (state == State.Walk)
            {
                if (ReachedDestination())
                    PickNewPatrolPoint();

                if (Time.time >= nextLookTime)
                {
                    UpdateDesiredLookPatrol();
                    nextLookTime = Time.time + Random.Range(lookChangeIntervalMin, lookChangeIntervalMax);
                }
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredLookRot, rotateSpeed * dt);
        }

        UpdateAnimator(dt);

        agent.nextPosition = transform.position;
    }

    void UpdateChase(float dt)
    {
        if (player == null) return;

        if (Time.time >= nextRepathTime)
        {
            agent.SetDestination(player.position);
            nextRepathTime = Time.time + repathInterval;
        }

        Vector3 dir = agent.steeringTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir, Vector3.up),
                rotateSpeed * dt
            );
        }

        float dist = DistToPlayer();
        if (dist <= stopDistance)
        {
            isRunning = false;
            isWalking = false;
        }
        else
        {
            isRunning = true;
            isWalking = false;
        }
    }

    void ApplyGravity(float dt)
    {
        if (cc.isGrounded)
        {
            if (verticalVel < 0f) verticalVel = -2f;
        }
        else
        {
            verticalVel += gravity * dt;
        }
    }

    float DistToPlayer()
    {
        if (player == null) return float.PositiveInfinity;
        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void UpdateAnimator(float dt)
    {
        if (animator == null) return;

        float targetSpeed;
        if (isRunning) targetSpeed = 1f;       // Run
        else if (isWalking) targetSpeed = 0.5f; // Walk
        else targetSpeed = 0f;                 // Idle

        animator.SetFloat(HashSpeed, targetSpeed, 0.15f, dt);
    }


    void EnterIdle()
    {
        state = State.Idle;
        isWalking = false;
        isRunning = false;
        stateEndTime = Time.time + Random.Range(idleMin, idleMax);
    }

    void EnterWalk()
    {
        state = State.Walk;
        isWalking = true;
        isRunning = false;
        stateEndTime = Time.time + Random.Range(walkMin, walkMax);

        PickNewPatrolPoint();
        UpdateDesiredLookPatrol();
        nextLookTime = Time.time + Random.Range(lookChangeIntervalMin, lookChangeIntervalMax);
    }

    void EnterChase()
    {
        state = State.Chase;
        isWalking = false;
        isRunning = true;
        nextRepathTime = 0f;

        if (player != null)
            agent.SetDestination(player.position);
    }

    void PickNewPatrolPoint()
    {
        Vector3 target = patrolCenter + Random.insideUnitSphere * patrolRadius;
        target.y = patrolCenter.y;

        if (NavMesh.SamplePosition(target, out var hit, patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    bool ReachedDestination()
    {
        if (!agent.hasPath) return true;

        Vector3 toEnd = agent.destination - transform.position;
        toEnd.y = 0f;
        return toEnd.magnitude <= 0.8f;
    }

    void UpdateDesiredLookPatrol()
    {
        Vector3 forward = agent.steeringTarget - transform.position;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            float yaw = Random.Range(0f, 360f);
            desiredLookRot = Quaternion.Euler(0f, yaw, 0f);
            return;
        }

        forward.Normalize();

        float yawOffset = Random.Range(-60f, 60f);
        Quaternion jitter = Quaternion.Euler(0f, yawOffset, 0f);
        Vector3 dir = jitter * forward;

        desiredLookRot = Quaternion.LookRotation(dir, Vector3.up);
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;

        bool moveByRoot = (state == State.Walk) || (state == State.Chase);
        if (!moveByRoot)
        {
            cc.Move(Vector3.up * (verticalVel * Time.deltaTime));
            return;
        }

        Vector3 delta = animator.deltaPosition;

        Vector3 dir = agent.steeringTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();

            float step = new Vector3(delta.x, 0f, delta.z).magnitude;
            Vector3 planar = dir * step;

            delta = planar;
        }
        else
        {
            delta = Vector3.zero;
        }

        delta.y += verticalVel * Time.deltaTime;
        cc.Move(delta);
    }

    void EnterAttack()
    {
        state = State.Attack;

        isWalking = false;
        isRunning = false;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
        {
            animator.ResetTrigger(HashAttack);
            animator.SetTrigger(HashAttack);
        }

        // 공격 중에는 경로 갱신/이동 멈춤
        agent.ResetPath();
    }

    void UpdateAttack(float dt)
    {
        if (player == null)
        {
            EnterIdle();
            return;
        }

        // 공격 중에도 플레이어를 바라보게만 하고 싶으면
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir, Vector3.up),
                rotateSpeed * dt
            );
        }

        // 애니메이션이 끝났으면 다시 추적/대기로 복귀
        if (animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            bool attackingNow = st.IsTag("Attack"); // Attack 태그 달아두기 추천

            // 전이 중일 때는 상태 판단이 흔들릴 수 있어서 NextState도 같이 봄
            if (!attackingNow && animator.IsInTransition(0))
            {
                var next = animator.GetNextAnimatorStateInfo(0);
                attackingNow = next.IsTag("Attack");
            }

            if (!attackingNow)
            {
                // 공격 끝났는데 아직 사거리면 쿨타임 후 재공격, 아니면 추적
                float dist = DistToPlayer();
                if (dist <= attackRange)
                {
                    if (Time.time >= nextAttackTime)
                        EnterAttack();
                    else
                        EnterChase(); // 쿨타임 동안엔 추적으로 두고 가까이 붙어있게
                }
                else
                {
                    EnterChase();
                }
            }
        }
    }
}
