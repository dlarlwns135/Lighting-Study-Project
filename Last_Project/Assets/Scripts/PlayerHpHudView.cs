using UnityEngine;
using UnityEngine.UI;

public class PlayerHpHudView : MonoBehaviour
{
    public Image fillImage;
    private Damageable bound;

    public void Bind(Damageable d)
    {
        Unbind();
        bound = d;
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

    void HandleHpChanged(Damageable d)
    {
        if (fillImage != null)
            fillImage.fillAmount = d.GetHp01();
    }

    void HandleDied(Damageable d)
    {
        // Á×¾úÀ» ¶§ ¼û±â°í ½ÍÀ¸¸é
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Unbind();
    }
}
