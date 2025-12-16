using UnityEngine;

public class HitboxEventRelay : MonoBehaviour
{
    public MeleeHitbox[] hitboxes;

    public void BeginHit()
    {
        foreach (var h in hitboxes)
            h.BeginHit();
    }

    public void EndHit()
    {
        foreach (var h in hitboxes)
            h.EndHit();
    }

    public void BeginVFX()
    {
        foreach (var h in hitboxes)
            h.BeginVFX();
    }

    public void EndVFX()
    {
        foreach (var h in hitboxes)
            h.EndVFX();
    }
}
