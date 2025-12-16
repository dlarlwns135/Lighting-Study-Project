using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
public class CC_NavAI : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public Transform player;
    public Damageable damageable;

    [Header("Patrol")]
    public Vector3 patrolCenter;
    public float patrolRadius = 8f;

    [Header("Random Idle/Walk")]
    public float idleMin = 0.8f;
    public float idleMax = 2.0f;
    public float walkMin = 1.2f;
    public float walkMax = 3.0f;

    [Header("Detect / Chase")]
    public float detectRange = 10f;
    public float loseRange = 14f;
    public float repathInterval = 0.2f;
    public float stopDistance = 1.7f;

    [Header("Look / Rotate")]
    public float rotateSpeed = 10f;
    public float lookChangeIntervalMin = 0.4f;
    public float lookChangeIntervalMax = 1.2f;

    [Header("Ground")]
    public float gravity = -9.81f;

    [Header("Attack")]
    public float attackRange = 2.0f;
    public float attackCooldown = 1.2f;

    [Header("Dead CC Adjust")]
    public float deadCenterY = 1.8f;
    public float deadCenterLerpSpeed = 5f;

    private CharacterController cc;
    private NavMeshAgent agent;

    private float verticalVel;
    private float nextLookTime;
    private float nextRepathTime;
    private float nextAttackTime;
    private float stateEndTime;

    private Quaternion desiredLookRot;
    private Vector3 ccCenterVelocity;

    private bool isWalking;
    private bool isRunning;

    private enum State { Idle, Walk, Chase, Attack }
    private State state;

    private readonly int HashSpeed = Animator.StringToHash("Speed");
    private readonly int HashAttack = Animator.StringToHash("Attack");

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        agent = GetComponent<NavMeshAgent>();

        agent.updatePosition = false;
        agent.updateRotation = false;

        patrolCenter = transform.position;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (damageable == null) damageable = GetComponentInChildren<Damageable>();

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

        // =========================
        // DEAD 처리
        // =========================
        if (damageable != null && damageable.IsDead)
        {
            isWalking = false;
            isRunning = false;

            if (agent.enabled)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            // CC center.y를 부드럽게 보정
            Vector3 center = cc.center;
            center.y = Mathf.SmoothDamp(
                center.y,
                deadCenterY,
                ref ccCenterVelocity.y,
                1f / deadCenterLerpSpeed
            );
            cc.center = center;

            return;
        }

        // GET HIT
        if (IsGetHit())
        {
            StopAI();
            ApplyGravity(dt);   // 공중에서 맞을 수도 있으니까
            UpdateAnimator(0f); // Speed 강제 0
            return;
        }

        ApplyGravity(dt);

        if (state != State.Chase && player != null && DistToPlayer() <= detectRange)
            EnterChase();

        if (state == State.Chase)
        {
            float dist = DistToPlayer();

            if (dist <= attackRange && Time.time >= nextAttackTime)
                EnterAttack();
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

    void UpdateAnimator(float dt)
    {
        float targetSpeed = isRunning ? 1f : isWalking ? 0.5f : 0f;
        animator.SetFloat(HashSpeed, targetSpeed, 0.15f, dt);
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;

        Vector3 delta = animator.deltaPosition;
        delta.y += verticalVel * Time.deltaTime;

        // GetHit / Dead → 루트모션 그대로
        if (damageable.IsDead || IsGetHit())
        {
            cc.Move(delta);
            return;
        }

        // Walk / Chase만 Nav 방향 보정
        if (state == State.Walk || state == State.Chase)
        {
            Vector3 dir = agent.steeringTarget - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                dir.Normalize();
                float step = new Vector3(delta.x, 0f, delta.z).magnitude;
                delta.x = dir.x * step;
                delta.z = dir.z * step;
            }
        }

        cc.Move(delta);
    }


    // =========================
    // 상태 전환
    // =========================

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

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void EnterAttack()
    {
        state = State.Attack;
        isWalking = false;
        isRunning = false;

        nextAttackTime = Time.time + attackCooldown;

        animator.ResetTrigger(HashAttack);
        animator.SetTrigger(HashAttack);

        agent.ResetPath();
        agent.isStopped = true;
    }

    void UpdateAttack(float dt)
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                rotateSpeed * dt
            );
        }

        var st = animator.GetCurrentAnimatorStateInfo(0);
        if (!st.IsTag("Attack"))
        {
            if (DistToPlayer() <= attackRange && Time.time >= nextAttackTime)
                EnterAttack();
            else
                EnterChase();
        }
    }

    // =========================
    // 유틸
    // =========================

    float DistToPlayer()
    {
        Vector3 a = transform.position;
        Vector3 b = player.position;
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void PickNewPatrolPoint()
    {
        Vector3 target = patrolCenter + Random.insideUnitSphere * patrolRadius;
        target.y = patrolCenter.y;

        if (NavMesh.SamplePosition(target, out var hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    bool ReachedDestination()
    {
        if (!agent.hasPath) return true;
        Vector3 d = agent.destination - transform.position;
        d.y = 0f;
        return d.magnitude <= 0.8f;
    }

    void UpdateDesiredLookPatrol()
    {
        Vector3 dir = agent.steeringTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f)
        {
            desiredLookRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            return;
        }

        dir.Normalize();
        dir = Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * dir;
        desiredLookRot = Quaternion.LookRotation(dir);
    }

    void UpdateChase(float dt)
    {
        if (player == null) return;

        // 주기적으로 경로 갱신
        if (Time.time >= nextRepathTime)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            nextRepathTime = Time.time + repathInterval;
        }

        // 이동 방향
        Vector3 dir = agent.steeringTarget - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.0001f)
        {
            dir.Normalize();

            // 회전만 여기서 처리 (이동은 OnAnimatorMove의 루트모션)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir, Vector3.up),
                rotateSpeed * dt
            );
        }

        // 달릴지 멈출지 판단
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

    bool IsGetHit()
    {
        if (animator == null) return false;

        var cur = animator.GetCurrentAnimatorStateInfo(0);
        if (cur.IsTag("GetHit")) return true;

        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag("GetHit")) return true;
        }

        return false;
    }

    void StopAI()
    {
        isWalking = false;
        isRunning = false;

        if (agent.enabled)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    void AdjustDeadCC()
    {
        Vector3 center = cc.center;
        center.y = Mathf.SmoothDamp(
            center.y,
            deadCenterY,
            ref ccCenterVelocity.y,
            1f / deadCenterLerpSpeed
        );
        cc.center = center;
    }
    public void Revive()
    {
        // Damageable 먼저
        if (damageable != null)
            damageable.ResetHp();

        // Animator 리셋
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // CharacterController center 복구
        Vector3 center = cc.center;
        center.y = 1.05731f;
        cc.center = center;
        ccCenterVelocity = Vector3.zero;

        // NavMeshAgent 복구
        if (agent.enabled)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.Warp(transform.position);
        }

        // 내부 상태 초기화
        isWalking = false;
        isRunning = false;
        verticalVel = 0f;

        // AI 상태 강제 Idle
        state = State.Idle;
        stateEndTime = Time.time + Random.Range(idleMin, idleMax);

        desiredLookRot = transform.rotation;
    }

}
