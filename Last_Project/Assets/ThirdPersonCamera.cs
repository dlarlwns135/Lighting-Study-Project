using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;        // 따라갈 캐릭터
    public Vector3 offset = new Vector3(0f, 1.6f, -3f);
    public float distance = 3f;     // 카메라 거리
    public float sensitivity = 150f;

    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw;              // 좌우 회전
    private float pitch;            // 상하 회전

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetLookInput(Vector2 lookInput)
    {
        yaw += lookInput.x * sensitivity * Time.deltaTime;
        pitch -= lookInput.y * sensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 타겟의 Y축 회전을 기준 yaw에 포함
        float targetYaw = target.eulerAngles.y;

        // 최종 yaw = 타겟 회전 + 사용자의 마우스 입력
        Quaternion rotation = Quaternion.Euler(pitch, yaw + targetYaw, 0);

        // 위치 계산
        Vector3 desiredPos = target.position
                           + rotation * (Vector3.back * distance + Vector3.up * offset.y);

        transform.position = desiredPos;

        transform.LookAt(target.position + Vector3.up * offset.y);
    }
}
