using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    public float distance = 3f;
    public float sensitivity = 150f;

    public float minPitch = -30f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    public float Yaw => yaw;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (target != null)
            yaw = target.eulerAngles.y;
    }

    public void AddLookInput(Vector2 lookInput)
    {
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
