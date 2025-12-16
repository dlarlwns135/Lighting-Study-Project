using UnityEngine;

public class DayLightCycle : MonoBehaviour
{
    [SerializeField] float dayDuration = 20f; // 하루 동안 태양이 360도 회전하는 시간
    [SerializeField] Material skyboxMaterial; // 스카이박스의 머티리얼을 참조
    float time;

    void Update()
    {
        // 하루 동안 회전하는 시간을 dayDuration을 기준으로 조정
        time += Time.deltaTime / dayDuration;
        if (time >= 1f) time = 0f; // 1이 넘어가면 다시 0으로 리셋

        // X, Y 회전값 설정 (time을 기반으로 각도를 설정)
        float rotationX = time * 360f; // X축 회전 (하루 동안 360도 회전)
        float rotationY = time * 180f; // Y축 회전 (예시로 Y축 회전값을 180도로 설정)

        // 스카이박스 회전 적용
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_RotationX", rotationX); // X축 회전값 전달
            //skyboxMaterial.SetFloat("_RotationY", rotationY); // Y축 회전값 전달
        }

        // 태양 오브젝트 회전 (디렉셔널 라이트 회전)
        transform.rotation = Quaternion.Euler(new Vector3(rotationX, 170f, 0f)); // 태양의 회전
    }
}
