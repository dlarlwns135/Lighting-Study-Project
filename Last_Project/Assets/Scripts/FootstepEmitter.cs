using UnityEngine;

public class FootstepSFX : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;
    public int animatorLayerIndex = 0;

    [Header("Footstep")]
    public AudioClip leftFootSd;
    public AudioClip rightFootSd;

    [Header("Action")]
    public AudioClip attackSd;
    public AudioClip getHitSd;
    public AudioClip deadSd;

    [Range(0f, 1f)] public float volume = 0.8f;
    public float minInterval = 0.03f;

    private AudioSource camSource;
    private float lastTime;

    void Awake()
    {
        var cam = Camera.main;
        if (cam != null)
            camSource = cam.GetComponent<AudioSource>();

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
        if (clip == null || camSource == null) return;

        if (animator != null && animator.IsInTransition(animatorLayerIndex))
            return;

        PlayInternal(clip);
    }

    // 전이 여부 무시하고 무조건 재생
    void PlayForce(AudioClip clip)
    {
        if (clip == null || camSource == null) return;
        PlayInternal(clip);
    }

    void PlayInternal(AudioClip clip)
    {
        if (Time.time - lastTime < minInterval) return;
        lastTime = Time.time;

        camSource.PlayOneShot(clip, volume);
    }
}
