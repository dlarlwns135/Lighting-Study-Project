using UnityEngine;

public class FootstepSFX : MonoBehaviour
{
    public AudioClip leftFootSd;
    public AudioClip rightFootSd;

    [Range(0f, 1f)] public float volume = 0.8f;
    public float minInterval = 0.03f;

    private AudioSource camSource;
    private float lastTime;

    void Awake()
    {
        var cam = Camera.main;
        if (cam != null)
            camSource = cam.GetComponent<AudioSource>();
    }

    public void PlayLeftFoot()
    {
        Play(leftFootSd);
    }

    public void PlayRightFoot()
    {
        Play(rightFootSd);
    }

    private void Play(AudioClip clip)
    {
        if (camSource == null || clip == null) return;
        if (Time.time - lastTime < minInterval) return;
        lastTime = Time.time;

        camSource.PlayOneShot(clip, volume);
    }
}
