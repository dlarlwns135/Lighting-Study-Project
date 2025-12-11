using UnityEngine;
using UnityEngine.InputSystem;

public class CC_Control : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSensitivity = 150f;
    public Transform cameraPivot;

    public float gravity = -9.81f;
    public float jumpPower = 5f;         // 점프 기능까지 쓸 수 있게 옵션 추가
    private float verticalVelocity = 0f; // y 속도 저장

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --------------------
        // 1) 마우스 Yaw/Pitch 회전
        // --------------------
        float yaw = lookInput.x * lookSensitivity * dt;
        transform.Rotate(Vector3.up * yaw);

        float pitchDelta = -lookInput.y * lookSensitivity * dt;
        pitch += pitchDelta;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --------------------
        // 2) 이동 입력
        // --------------------
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move *= moveSpeed;

        // --------------------
        // 3) 중력 적용
        // --------------------
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f; // 지면 붙여놓기

            // 점프 키를 쓸 경우 ↓
            // if (Keyboard.current.spaceKey.wasPressedThisFrame)
            //     verticalVelocity = jumpPower;
        }
        else
        {
            verticalVelocity += gravity * dt;
        }

        move.y = verticalVelocity;

        // --------------------
        // 4) 캐릭터 이동
        // --------------------
        controller.Move(move * dt);
    }
}
