using System;
using UnityEngine;

public class Damageable : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 100;
    public int hp = 100;

    [Header("Invincibility")]
    public bool isInvincible = false;

    [Header("Animator")]
    public Animator animator;

    [Header("Animator Params")]
    public string getHitTriggerName = "GetHit";
    public string deadTriggerName = "Dead";

    private int hashGetHit;
    private int hashDead;

    public bool IsDead => hp <= 0;

    public event Action<Damageable> OnHpChanged;
    public event Action<Damageable> OnDied;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        hashGetHit = Animator.StringToHash(getHitTriggerName);
        hashDead = Animator.StringToHash(deadTriggerName);

        hp = Mathf.Clamp(hp, 0, maxHP);
    }

    public void ApplyDamage(int dmg, GameObject attacker)
    {
        if (isInvincible) return;
        if (IsDead) return;

        hp -= dmg;

        bool diedNow = false;
        if (hp <= 0)
        {
            hp = 0;
            diedNow = true;
        }

        if (animator != null)
            animator.SetTrigger(diedNow ? hashDead : hashGetHit);

        OnHpChanged?.Invoke(this);

        if (diedNow)
            OnDied?.Invoke(this);
    }

    public float GetHp01()
    {
        if (maxHP <= 0) return 0f;
        return Mathf.Clamp01((float)hp / maxHP);
    }

    public void ResetHp()
    {
        hp = maxHP;
        isInvincible = false;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        OnHpChanged?.Invoke(this);
    }
}
