using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 4f;
    public float sensitivity = 8f;

    public float minPitch = -20f;
    public float maxPitch = 10f;

    private float yaw;
    private float pitch;
    private bool tabHeld;

    private float initYaw;
    private float initPitch;

    public float Yaw => yaw;

    void Start()
    {
        CacheInitialView();
    }

    public void CacheInitialView()
    {
        if (target != null)
            yaw = target.eulerAngles.y;

        initYaw = yaw;
        initPitch = pitch;
    }

    public void ResetViewToInitial()
    {
        yaw = initYaw;
        pitch = initPitch;
    }

    public void AddLookInput(Vector2 lookInput)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
            return;

        tabHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
        if (tabHeld)
            return;

        float dt = Time.deltaTime;
        yaw += lookInput.x * sensitivity * dt;
        pitch -= lookInput.y * sensitivity * dt;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 pivot = target.position + pivotOffset;
        Vector3 desiredPos = pivot + rotation * (Vector3.back * distance);

        transform.position = desiredPos;
        transform.LookAt(pivot);
    }
}
