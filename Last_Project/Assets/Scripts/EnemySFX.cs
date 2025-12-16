using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySFX : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;

    [Header("Footstep")]
    public AudioClip leftFootSd;
    public AudioClip rightFootSd;

    [Header("Action")]
    public AudioClip attackSd;
    public AudioClip getHitSd;
    public AudioClip deadSd;

    [Header("Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    public float minInterval = 0.03f;
    public int animatorLayerIndex = 0;

    private AudioSource audioSource;
    private float lastTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // =========================
    // Animation Event용
    // =========================

    public void PlayLeftFoot()
    {
        PlayConditional(leftFootSd);
    }

    public void PlayRightFoot()
    {
        PlayConditional(rightFootSd);
    }

    public void PlayAttack()
    {
        PlayConditional(attackSd);
    }

    // ↓↓↓ 무조건 재생 ↓↓↓

    public void PlayGetHit()
    {
        PlayForce(getHitSd);
    }

    public void PlayDead()
    {
        PlayForce(deadSd);
    }

    // =========================
    // 내부 처리
    // =========================

    // 전이 중이면 차단되는 사운드
    void PlayConditional(AudioClip clip)
    {
        if (clip == null) return;

        if (animator != null && animator.IsInTransition(animatorLayerIndex))
            return;

        PlayInternal(clip);
    }

    // 전이 여부 무시하고 무조건 재생
    void PlayForce(AudioClip clip)
    {
        if (clip == null) return;
        PlayInternal(clip);
    }

    void PlayInternal(AudioClip clip)
    {
        if (Time.time - lastTime < minInterval) return;
        lastTime = Time.time;

        audioSource.PlayOneShot(clip, volume);
    }
}
