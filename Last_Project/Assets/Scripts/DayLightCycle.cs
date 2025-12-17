using UnityEngine;

public class DayLightCycle : MonoBehaviour
{
    [SerializeField] float dayDuration = 20f; // 하루 동안 태양이 360도 회전하는 시간
    [SerializeField] Material skyboxMaterial; // 스카이박스의 머티리얼을 참조
    [SerializeField] float maxExposure = 1.5f; // 낮 동안 최대 노출 값
    [SerializeField] float minExposure = 0.2f; // 밤 동안 최소 노출 값
    [SerializeField] GameObject streetLightsParent; // 가로등을 포함하는 빈 오브젝트
    [SerializeField] float lightOnThreshold = 180f; // 가로등을 켤 때 태양이 지평선 아래로 얼마나 내려갔는지 설정하는 기준값
    private Light directionalLight; // 디렉셔널 라이트 참조
    float time;

    void Start()
    {
        // 디렉셔널 라이트 컴포넌트를 자기 자신에서 가져옴
        directionalLight = GetComponent<Light>();
    }

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
        }

        // 태양 오브젝트 회전 (디렉셔널 라이트 회전)
        transform.rotation = Quaternion.Euler(new Vector3(rotationX, 170f, 0f)); // 태양의 회전

        // 시간에 따라 _Exposure 값 조정 (낮에는 밝고 밤에는 어두운 효과)
        float exposure = Mathf.Lerp(minExposure, maxExposure, Mathf.Abs(Mathf.Cos(time * Mathf.PI)));

        // 밤일 때는 exposure 값을 더 낮춰 명암을 더 어둡게 설정
        if (rotationX >= lightOnThreshold)
        {
            exposure = Mathf.Lerp(minExposure, minExposure * 0.5f, Mathf.Abs(Mathf.Cos(time * Mathf.PI))); // 밤일 때 노출을 더 낮추어 어둡게 만들기
        }

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Exposure", exposure); // _Exposure 값 업데이트
        }

        // 디렉셔널 라이트의 Intensity 조정 (밤일 때 빛 세기 낮추기)
        AdjustDirectionalLightIntensity(rotationX);

        // 가로등 라이트 제어 (디렉셔널 라이트 방향에 따라 켬/끔)
        ControlStreetLights(rotationX); // rotationX를 기준으로 가로등 켜기/끄기
    }

    void AdjustDirectionalLightIntensity(float rotationX)
    {
        // 밤일 때 (rotationX >= lightOnThreshold) 빛 세기 낮추기
        if (rotationX >= lightOnThreshold)
        {
            directionalLight.intensity = Mathf.Lerp(0.1f, 1f, Mathf.Abs(Mathf.Cos(time * Mathf.PI))); // 밤에는 Intensity를 낮추고, 낮에는 기본값
        }
        else
        {
            directionalLight.intensity = 1f; // 낮에는 Intensity를 기본값 (1)으로 설정
        }
    }

    void ControlStreetLights(float rotationX)
    {
        // 디버깅 메시지 출력: exposure 값 확인
        Debug.Log("rotationX: " + rotationX);
        // 디렉셔널 라이트의 X축 회전값이 수평선 아래로 내려갔을 때 가로등 켬
        bool lightsOn = rotationX >= lightOnThreshold; // 수평선 아래로 내려가면 가로등을 켬

        if (streetLightsParent != null)
        {
            // 빈 오브젝트(가로등의 부모)에 있는 모든 라이트들을 찾음
            Light[] streetLights = streetLightsParent.GetComponentsInChildren<Light>();

            // 가로등의 라이트 상태를 업데이트
            foreach (Light streetLight in streetLights)
            {
                if (streetLight != null)
                {
                    streetLight.enabled = lightsOn; // 가로등을 켬/끔
                }
            }
        }
    }
}
