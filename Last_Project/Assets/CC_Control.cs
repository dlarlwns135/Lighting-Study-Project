using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Move Speed")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isRunning;

    private readonly int HashMoveX = Animator.StringToHash("MoveX");
    private readonly int HashMoveY = Animator.StringToHash("MoveY");
    private readonly int HashSpeed = Animator.StringToHash("Speed");
    private readonly int HashIsRun = Animator.StringToHash("IsRun");
    private readonly int HashAttack = Animator.StringToHash("Attack");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (thirdPersonCamera == null) thirdPersonCamera = FindFirstObjectByType<ThirdPersonCamera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>();

    void Update()
    {
        float dt = Time.deltaTime;

        if (animator != null && Mouse.current.leftButton.wasPressedThisFrame)
            animator.SetTrigger(HashAttack);

        if (thirdPersonCamera != null)
            thirdPersonCamera.AddLookInput(lookInput);

        bool isAttacking = false;
        if (animator != null)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            isAttacking = st.IsTag("Attack");
        }

        Vector2 move = isAttacking ? Vector2.zero : moveInput;

        isRunning = !isAttacking && Keyboard.current.leftShiftKey.isPressed;

        float camYaw = (thirdPersonCamera != null) ? thirdPersonCamera.Yaw : transform.eulerAngles.y;
        Quaternion targetRot = Quaternion.Euler(0f, camYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * dt);

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * dt;
        }

        Vector3 moveDir;
        float inputMag = Mathf.Clamp01(move.magnitude);

        if (thirdPersonCamera != null)
        {
            Vector3 f = thirdPersonCamera.transform.forward;
            Vector3 r = thirdPersonCamera.transform.right;
            f.y = 0f;
            r.y = 0f;
            f.Normalize();
            r.Normalize();

            moveDir = r * move.x + f * move.y;
        }
        else
        {
            moveDir = transform.right * move.x + transform.forward * move.y;
        }

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 planar = moveDir * (currentSpeed * dt);
        Vector3 vertical = Vector3.up * (verticalVelocity * dt);
        controller.Move(planar + vertical);

        if (animator != null)
        {
            Vector2 dir = move;
            if (inputMag > 0.0001f) dir /= inputMag;

            animator.SetFloat(HashMoveX, dir.x, animDampTime, dt);
            animator.SetFloat(HashMoveY, dir.y, animDampTime, dt);
            animator.SetFloat(HashSpeed, inputMag, animDampTime, dt);
            animator.SetBool(HashIsRun, isRunning);
        }

        lookInput = Vector2.zero;
    }
}
