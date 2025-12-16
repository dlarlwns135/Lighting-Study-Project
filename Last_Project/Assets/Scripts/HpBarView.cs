using UnityEngine;
using UnityEngine.UI;

public class HpBarView : MonoBehaviour
{
    [Header("UI")]
    public Image fillImage;

    [Header("Follow")]
    public Transform target;
    public Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);
    public bool faceCamera = true;

    Camera cam;
    Damageable bound;

    void Awake()
    {
        cam = Camera.main;
    }

    public void Bind(Damageable d, Transform followTarget)
    {
        Unbind();

        bound = d;
        target = followTarget;

        if (bound != null)
        {
            bound.OnHpChanged += HandleHpChanged;
            bound.OnDied += HandleDied;
            HandleHpChanged(bound);
        }
    }

    public void Unbind()
    {
        if (bound != null)
        {
            bound.OnHpChanged -= HandleHpChanged;
            bound.OnDied -= HandleDied;
            bound = null;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + worldOffset;

        if (faceCamera)
        {
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }
    }

    void HandleHpChanged(Damageable d)
    {
        if (fillImage != null)
            fillImage.fillAmount = d.GetHp01();
    }

    void HandleDied(Damageable d)
    {
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Unbind();
    }
}
