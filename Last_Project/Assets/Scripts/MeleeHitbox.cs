using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MeleeHitbox : MonoBehaviour
{
    [Header("Owner")]
    public MonoBehaviour owner;
    public int damage = 10;

    [Header("Target Filter")]
    public LayerMask targetLayers;

    [Header("Runtime")]
    public bool enableOnlyDuringAttack = true;

    [Header("VFX - Swing (follow)")]
    public Transform vfxRoot;
    public bool followWhileActive = true;
    public bool useColliderCenter = true;

    [Header("VFX - Hit (impact, one-shot)")]
    public Transform hitVfxRoot;
    public bool hitUseColliderCenter = true;
    public bool hitFollowWhileActive = false;
    public float hitAutoStopAfter = 0.2f;

    private Collider col;
    private readonly HashSet<int> hitThisSwing = new HashSet<int>();

    private ParticleSystem[] swingVfxSystems;
    private ParticleSystem[] hitVfxSystems;

    private bool swingVfxActive;
    private bool hitVfxActive;
    private float hitVfxStopTime;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;

        if (enableOnlyDuringAttack)
            col.enabled = false;

        if (vfxRoot != null)
            swingVfxSystems = vfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        if (hitVfxRoot != null)
            hitVfxSystems = hitVfxRoot.GetComponentsInChildren<ParticleSystem>(true);

        swingVfxActive = false;
        hitVfxActive = false;

        StopSystems(swingVfxSystems, true);
        StopSystems(hitVfxSystems, true);
    }

    void LateUpdate()
    {
        // Swing VFX follow
        if (followWhileActive && swingVfxActive && vfxRoot != null && swingVfxSystems != null)
        {
            Vector3 pos = useColliderCenter ? col.bounds.center : transform.position;
            vfxRoot.position = pos;
            vfxRoot.rotation = transform.rotation;
        }

        // Hit VFX follow (원하면)
        if (hitFollowWhileActive && hitVfxActive && hitVfxRoot != null && hitVfxSystems != null)
        {
            Vector3 pos = hitUseColliderCenter ? col.bounds.center : transform.position;
            hitVfxRoot.position = pos;
            hitVfxRoot.rotation = transform.rotation;
        }

        // Hit VFX 자동 Stop (이미 생성된 파티클은 lifetime 끝날 때까지 남게 됨)
        if (hitVfxActive && Time.time >= hitVfxStopTime)
        {
            EndHitVFX();
        }
    }

    // =========================
    // HIT (판정)
    // =========================

    public void BeginHit()
    {
        hitThisSwing.Clear();
        col.enabled = true;
    }

    public void EndHit()
    {
        col.enabled = false;
    }

    // =========================
    // VFX - Swing (follow)
    // =========================

    public void BeginVFX()
    {
        if (vfxRoot == null || swingVfxSystems == null) return;

        Vector3 pos = useColliderCenter ? col.bounds.center : transform.position;
        vfxRoot.position = pos;
        vfxRoot.rotation = transform.rotation;

        PlaySystems(swingVfxSystems);
        swingVfxActive = true;
    }

    public void EndVFX()
    {
        if (swingVfxSystems == null) return;

        StopSystems(swingVfxSystems, false);
        swingVfxActive = false;
    }

    // =========================
    // VFX - Hit (impact)
    // =========================

    public void PlayHitVFXAt(Vector3 worldPos, Quaternion worldRot)
    {
        if (hitVfxRoot == null || hitVfxSystems == null) return;

        hitVfxRoot.position = worldPos;
        hitVfxRoot.rotation = worldRot;

        PlaySystems(hitVfxSystems);
        hitVfxActive = true;
        hitVfxStopTime = Time.time + Mathf.Max(0f, hitAutoStopAfter);
    }

    public void EndHitVFX()
    {
        if (hitVfxSystems == null) return;

        StopSystems(hitVfxSystems, false);
        hitVfxActive = false;
    }

    // =========================
    // DAMAGE
    // =========================

    void OnTriggerEnter(Collider other)
    {
        // 데미지를 주는 충돌 판정이 공격 중일 때만 발생
        if (!col.enabled && enableOnlyDuringAttack)
            return;

        // 대상이 targetLayers에 포함되어 있는지 체크
        if (((1 << other.gameObject.layer) & targetLayers.value) == 0)
            return;

        Transform myRoot = transform;
        Transform otherRoot = other.transform;

        // 자신과 충돌한 것이 아닌 다른 객체만 처리
        if (otherRoot == myRoot)
            return;

        // 충돌한 적이 이미 hitThisSwing에 포함되었는지 확인
        int id = otherRoot.GetInstanceID();
        if (hitThisSwing.Contains(id))
        {
            Debug.Log($"Already hit {other.gameObject.name} - Skipping damage.");
            return;  // 이미 데미지를 입은 적은 무시
        }

        // 충돌한 오브젝트의 Damageable을 직접 찾기
        var dmg = otherRoot.GetComponentInChildren<Damageable>(); // GetComponentInChildren -> GetComponent로 변경
        if (dmg == null)
            return;

        // 충돌 지점 계산
        Vector3 refPos = useColliderCenter ? col.bounds.center : transform.position;
        Vector3 hitPoint = other.ClosestPoint(refPos);

        // 데미지를 받을 때 적용할 방향
        Quaternion hitRot = Quaternion.LookRotation(transform.forward, Vector3.up);

        // 맞은 지점에서 효과를 발생
        PlayHitVFXAt(hitPoint, hitRot);

        // 데미지 적용 (해당 오브젝트에 데미지 적용)
        dmg.ApplyDamage(damage, myRoot.gameObject);

        // 해당 적을 hitThisSwing 집합에 추가
        hitThisSwing.Add(id);
        Debug.Log($"Applied damage to {other.gameObject.name}");
    }


    // =========================
    // Internal
    // =========================

    static void PlaySystems(ParticleSystem[] systems)
    {
        if (systems == null) return;
        for (int i = 0; i < systems.Length; i++)
            if (systems[i] != null)
                systems[i].Play(true);
    }

    static void StopSystems(ParticleSystem[] systems, bool clear)
    {
        if (systems == null) return;

        var stopMode = ParticleSystemStopBehavior.StopEmitting;
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null) continue;

            ps.Stop(true, stopMode);
            if (clear) ps.Clear(true);
        }
    }
}
