using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CharacterController))]
public class CC_Control : MonoBehaviour
{
    public float turnSpeed = 12f;
    public float gravity = -9.81f;
    private float verticalVelocity = 0f;

    [Header("Refs")]
    public ThirdPersonCamera thirdPersonCamera;
    public Animator animator;
    public float animDampTime = 0.1f;
    public Damageable damageable;

    [Header("Move Speed")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    [Header("Dead Fall (No CC)")]
    public float deadFallSpeed = 4.2f;
    public float deadGroundOffset = 0.02f;
    public LayerMask groundMask = ~0;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;
    private bool isAttacking;

    private readonly int HashMoveX = Animator.StringToHash("MoveX");
    private readonly int HashMoveY = Animator.StringToHash("MoveY");
    private readonly int HashSpeed = Animator.StringToHash("Speed");
    private readonly int HashIsRun = Animator.StringToHash("IsRun");
    private readonly int HashAttack = Animator.StringToHash("Attack");

    private bool wasDead;
    private bool ccDisabledForDead;
    private float cachedBottomYOnDeath;

    private float ccCenterVelocityY;  // center.y 이동 속도
    private float deadCenterLerpSpeed = 1f;  // center.y 보정 속도

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (damageable == null) damageable = GetComponentInChildren<Damageable>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (thirdPersonCamera == null) thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();

        if (animator != null) animator.applyRootMotion = true;
    }

    public void OnMove(InputValue value)
    {
        if (IsInputLocked()) return;
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (IsInputLocked()) return;
        lookInput = value.Get<Vector2>();
    }

    bool IsGameStarted()
    {
        return GameManager.Instance == null || GameManager.Instance.IsGameStarted;
    }

    bool IsTabHeld()
    {
        return Keyboard.current != null && Keyboard.current.tabKey.isPressed;
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    bool CanAttackThisFrame()
    {
        if (!IsGameStarted()) return false;
        if (IsTabHeld()) return false;
        if (IsPointerOverUI()) return false;
        if (IsInputLocked()) return false;
        if (IsAttackOrTransitionToAttack()) return false;
        return true;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
        {
            lookInput = Vector2.zero;
        }

        if (thirdPersonCamera != null)
            thirdPersonCamera.AddLookInput(lookInput);

        bool deadNow = IsDeadState();

        if (deadNow)
        {
            //Debug.Log($"deadNow");
            //if (controller != null && controller.enabled)
               // controller.enabled = false;

            EnterDeadIfNeeded();

            MoveToGroundSlowly(dt);

            moveInput = Vector2.zero;
            isRunning = false;

            if (animator != null)
            {
                animator.SetFloat(HashMoveX, 0f, animDampTime, dt);
                animator.SetFloat(HashMoveY, 0f, animDampTime, dt);
                animator.SetFloat(HashSpeed, 0f, animDampTime, dt);
                animator.SetBool(HashIsRun, false);
            }

            lookInput = Vector2.zero;
            return;
        }
        else
        {
            ExitDeadIfNeeded();
        }

        bool inputLocked = IsInputLocked();

        if (animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            isAttacking = st.IsTag("Attack");
        }

        if (inputLocked)
        {
            moveInput = Vector2.zero;
            isRunning = false;

            if (animator != null)
            {
                animator.SetFloat(HashMoveX, 0f, animDampTime, dt);
                animator.SetFloat(HashMoveY, 0f, animDampTime, dt);

                // Speed만 조정, 이동은 하지 않음
                float inputMag = Mathf.Clamp01(moveInput.magnitude);
                Vector2 dir = (inputMag > 0.0001f) ? (moveInput / inputMag) : Vector2.zero;
                animator.SetFloat(HashSpeed, inputMag, animDampTime, dt);

                animator.SetBool(HashIsRun, false);
            }

            lookInput = Vector2.zero;
            return;
        }

        if (animator != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (CanAttackThisFrame())
                animator.SetTrigger(HashAttack);
        }

        Vector2 move = isAttacking ? Vector2.zero : moveInput;
        isRunning = !isAttacking && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

        float camYaw = (thirdPersonCamera != null) ? thirdPersonCamera.Yaw : transform.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, camYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);

        ApplyGravity(dt);

        if (animator != null)
        {
            float inputMag = Mathf.Clamp01(move.magnitude);
            Vector2 dir = (inputMag > 0.0001f) ? (move / inputMag) : Vector2.zero;

            animator.SetFloat(HashMoveX, dir.x, animDampTime, dt);
            animator.SetFloat(HashMoveY, dir.y, animDampTime, dt);
            animator.SetFloat(HashSpeed, inputMag, animDampTime, dt);
            animator.SetBool(HashIsRun, isRunning);
        }

        lookInput = Vector2.zero;
    }

    void ApplyGravity(float dt)
    {
        if (controller != null && controller.enabled && controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            if (!IsDeadState()) // 죽었을 때는 중력 적용 안 함
            {
                verticalVelocity += gravity * dt;
            }
        }
    }

    void OnAnimatorMove()
    {
        if (animator == null) return;

        // 죽었을 땐 CC를 꺼두는 전략이라 루트모션 이동 자체도 막아두는 게 안전
        if (IsDeadState()) return;

        if (controller == null || !controller.enabled) return;

        Vector3 delta = animator.deltaPosition;
        delta += Vector3.up * (verticalVelocity * Time.deltaTime);

        controller.Move(delta);
        transform.rotation = animator.rootRotation;
    }

    void EnterDeadIfNeeded()
    {
        if (wasDead) return;

        wasDead = true;
        verticalVelocity = 0f;
        moveInput = Vector2.zero;

        cachedBottomYOnDeath = SampleGroundY(transform.position);

        if (controller != null)
        {
            if (controller.enabled)
            {
                //controller.enabled = false;
                ccDisabledForDead = true;
                Debug.Log($"[CC_Control] CC disabled on death: {controller.gameObject.name}", controller);
            }
            else
            {
                ccDisabledForDead = true;
                Debug.Log($"[CC_Control] CC already disabled: {controller.gameObject.name}", controller);
            }
        }
        else
        {
            ccDisabledForDead = false;
            Debug.LogWarning("[CC_Control] controller is null (CC on parent?)", this);
        }
    }

    void ExitDeadIfNeeded()
    {
        if (!wasDead) return;

        wasDead = false;
        verticalVelocity = -2f;

        // CC 다시 켜기
        if (controller != null && ccDisabledForDead)
        {
            controller.enabled = true;
            ccDisabledForDead = false;
        }
    }

    void MoveToGroundSlowly(float dt)
    {
        if (!IsDeadState()) return; // 죽지 않았을 때는 이동하지 않음

        // 목표 y값은 1.77
        float targetCenterY = 1.77f;  // 죽었을 때 목표 y값

        // 현재 캐릭터의 center.y 값을 부드럽게 목표값인 targetCenterY로 올려줍니다.
        Vector3 center = controller.center;

        // Mathf.MoveTowards 사용으로 더 빠르게 목표값으로 이동
        center.y = Mathf.MoveTowards(center.y, targetCenterY, deadCenterLerpSpeed * dt);

        // controller.center의 값을 수정하여 y를 올려줍니다.
        controller.center = center;

        // 중력처럼 아래로 떨어지게 만듦
        if (controller.isGrounded)
        {
            // 이미 땅에 있는 경우 속도를 0으로 설정
            verticalVelocity = 0f;
        }
        else
        {
            // 중력 처리: 더 빠르게 떨어지도록 gravity에 deadFallSpeed를 반영
            verticalVelocity += gravity * dt;  // gravity를 강하게 반영
        }

        // controller가 비활성화되어 있지 않을 때만 Move() 호출
        if (controller.enabled)
        {
            // 중력을 반영하여 캐릭터 이동
            Vector3 move = new Vector3(0, verticalVelocity, 0);
            controller.Move(move * dt); // gravity가 적용된 y값으로 캐릭터 이동
        }
        else
        {
            Debug.LogWarning("[MoveToGroundSlowly] CharacterController is disabled. Move not called.");
        }
    }


    float SampleGroundY(Vector3 fromPos)
    {
        Vector3 origin = fromPos + Vector3.up * 2.0f;
        float maxDist = 10.0f;

        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDist, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;

        // 못 찾으면 죽는 순간 캐싱값(또는 현재 y) 사용
        return wasDead ? cachedBottomYOnDeath : fromPos.y;
    }

    bool IsAttackOrTransitionToAttack()
    {
        if (animator == null) return false;

        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag("Attack")) return true;
        }

        var cur = animator.GetCurrentAnimatorStateInfo(0);
        return cur.IsTag("Attack");
    }

    bool IsInputLocked()
    {
        if (animator == null) return false;

        var cur = animator.GetCurrentAnimatorStateInfo(0);
        if (cur.IsTag("GetHit") || cur.IsTag("Dead"))
            return true;

        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag("GetHit") || next.IsTag("Dead"))
                return true;
        }

        return false;
    }

    bool IsDeadState()
    {
        // damageable이 null인지, IsDead가 제대로 작동하는지 확인하는 디버그
        if (damageable != null && damageable.IsDead) return true;

        if (animator == null) return false;

        var cur = animator.GetCurrentAnimatorStateInfo(0);
        if (cur.IsTag("Dead")) return true;

        if (animator.IsInTransition(0))
        {
            var next = animator.GetNextAnimatorStateInfo(0);
            if (next.IsTag("Dead"))
                return true;
        }

        return false;
    }

    // 리스폰 시 CharacterController 초기화
    void ResetCharacterController()
    {
        if (controller != null)
        {
            // center 초기화
            controller.center = new Vector3(0f, 1.05731f, 0f); // 리스폰 시 Y값을 설정

            // CC 다시 활성화
            controller.enabled = true;

            // 필요한 경우 다른 CharacterController 속성도 초기화할 수 있습니다.
        }
    }

    // 리스폰 후 캐릭터가 죽었을 때 CC 속성 초기화하는 함수
    public void Revive()
    {
        if (damageable != null) damageable.ResetHp();

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // 리스폰 시 캐릭터 컨트롤러 초기화
        ResetCharacterController();

        // CC 상태 초기화
        verticalVelocity = 0f; // 낙하 초기화
        moveInput = Vector2.zero;
        wasDead = false;  // 죽지 않은 상태로 복구
        cachedBottomYOnDeath = 0f;  // 바닥 캐시 값 초기화

        // 애니메이션 상태 초기화
        if (animator != null)
        {
            animator.SetFloat(HashSpeed, 0f, animDampTime, Time.deltaTime);
            animator.SetBool(HashIsRun, false);
        }

        // 원하는 초기화 로직을 여기 추가
    }
}
