using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tấn công của EvilWitch. Chạy cùng EvelWitchMoverment (script đó lo phát hiện + đuổi theo).
/// Khi Player vào attackRange và hết cooldown: bật bool isAttack, sau attackHitTime thì gây sát thương
/// (cận chiến hoặc bắn đạn), hết attackDuration thì tắt isAttack.
/// Có thể gây sát thương bằng Animation Event DealDamage() thay vì tính giờ (damageByAnimationEvent).
/// </summary>
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EvelWitchMoverment))]
public class EvilWitchAttack : MonoBehaviour
{
    [Header("Tấn công chung")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Melee;
    [Tooltip("Player vào khoảng cách này thì Witch dừng lại và đánh")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int damage = 1;
    [Tooltip("Thời gian nghỉ giữa 2 đòn (giây), tính sau khi đòn trước kết thúc")]
    [SerializeField] private float attackCooldown = 1.5f;
    [Tooltip("Độ dài animation EvilWitchAttack (giây). isAttack được bật suốt khoảng này. Clip hiện tại ~1.02s")]
    [SerializeField] private float attackDuration = 1.02f;
    [Tooltip("Thời điểm đòn trúng / đạn bay ra, tính từ lúc bắt đầu animation (giây)")]
    [SerializeField] private float attackHitTime = 0.5f;
    [Tooltip("Bật = sát thương do Animation Event gọi DealDamage() trong clip Attack, không tính theo attackHitTime")]
    [SerializeField] private bool damageByAnimationEvent = false;

    [Header("Cận chiến")]
    [Tooltip("Tâm vùng đánh cách Witch bao xa (về phía Player)")]
    [SerializeField] private float meleeReach = 0.8f;
    [SerializeField] private float meleeHitRadius = 0.8f;
    [Tooltip("Layer của mục tiêu bị đánh. Để trống = layer Player")]
    [SerializeField] private LayerMask targetLayers;

    [Header("Tầm xa")]
    [Tooltip("Prefab đạn có component Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Vị trí đạn bay ra. Để trống = tâm Witch")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileLifeTime = 3f;

    [Header("Animator")]
    [SerializeField] private string isAttackParameter = "isAttack";

    public float AttackRange => attackRange;
    public bool IsAttacking { get; private set; }

    private Health health;
    private EvelWitchMoverment movement;
    private Animator animator;
    private EnemySkillStatus skillStatus;

    private int isAttackHash;
    private bool animAttack;
    private bool hasDealtDamage;
    private float nextAttackTime;

    private void Awake()
    {
        health = GetComponent<Health>();
        movement = GetComponent<EvelWitchMoverment>();
        animator = GetComponent<Animator>();
        TryGetComponent(out skillStatus);

        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
        isAttackHash = Animator.StringToHash(isAttackParameter);
    }

    private void OnDisable()
    {
        // Health tắt script này khi Witch chết: dừng đòn đánh đang chạy dở
        StopAllCoroutines();
        IsAttacking = false;
        SetAttack(false);
    }

    private void OnValidate()
    {
        attackDuration = Mathf.Max(0.05f, attackDuration);
        attackHitTime = Mathf.Clamp(attackHitTime, 0f, attackDuration);
        // Vòng gây sát thương phải phủ tới rìa attackRange để Player đứng trong tầm là luôn trúng
        if (attackType == EnemyAttackType.Melee)
            meleeHitRadius = Mathf.Max(meleeHitRadius, attackRange - meleeReach);
    }

    private void Update()
    {
        if (health.IsDead) return;
        if (skillStatus == null) TryGetComponent(out skillStatus);
        if (skillStatus != null && skillStatus.Immobilized)
        {
            if (IsAttacking)
            {
                StopAllCoroutines();
                IsAttacking = false;
                SetAttack(false);
                nextAttackTime = Time.time + .5f;
            }
            return;
        }
        if (IsAttacking) return;
        if (!movement.HasAggro || movement.Target == null) return;

        if (movement.DistanceToTarget <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        IsAttacking = true;
        hasDealtDamage = false;
        SetAttack(true);

        yield return new WaitForSeconds(attackHitTime);

        // Nếu dùng Animation Event thì DealDamage() sẽ lo phần sát thương
        if (!damageByAnimationEvent) PerformAttack();

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitTime));

        SetAttack(false);
        IsAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    /// <summary>Gọi từ Animation Event trong clip EvilWitchAttack (chỉ có tác dụng khi damageByAnimationEvent = true).</summary>
    public void DealDamage()
    {
        if (!damageByAnimationEvent || !IsAttacking || health.IsDead) return;
        PerformAttack();
    }

    private void PerformAttack()
    {
        if (hasDealtDamage) return; // mỗi đòn chỉ gây sát thương 1 lần
        if (movement.Target == null) return;
        hasDealtDamage = true;

        if (attackType == EnemyAttackType.Melee) MeleeHit();
        else FireProjectile();
    }

    private void MeleeHit()
    {
        Vector2 origin = transform.position;
        Vector2 direction = movement.DirectionToTarget;
        Vector2 center = origin + direction * meleeReach;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeHitRadius, targetLayers);
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var col in hits)
        {
            var victim = col.GetComponentInParent<IDamageable>();
            if (victim == null || victim.IsDead || victim.Team == health.Team) continue;
            if (!alreadyHit.Add(victim)) continue; // 1 nạn nhân chỉ ăn 1 đòn dù có nhiều collider

            victim.TakeDamage(damage, direction);
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[EvilWitchAttack] '{name}' là Ranged nhưng chưa gán projectilePrefab", this);
            return;
        }

        Vector2 spawn = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 direction = ((Vector2)movement.Target.position - spawn).normalized;

        Projectile projectile = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        projectile.SetTeam(health.Team); // đạn của enemy chỉ trúng Player
        projectile.Initialize(direction, 1f, null, projectileSpeed, projectileLifeTime, damage);
    }

    private void SetAttack(bool value)
    {
        if (animator == null || animAttack == value) return;
        animAttack = value;
        animator.SetBool(isAttackHash, value);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = transform.position;

        Gizmos.color = Color.red; // attackRange: vào tầm thì bắt đầu đánh
        Gizmos.DrawWireSphere(pos, attackRange);

        if (attackType == EnemyAttackType.Melee)
        {
            Vector2 dir = Vector2.right;
            if (Application.isPlaying && movement != null && movement.Target != null)
                dir = movement.DirectionToTarget;

            Vector2 hitCenter = (Vector2)pos + dir * meleeReach;
            Gizmos.color = Color.magenta; // vùng gây sát thương thật
            Gizmos.DrawLine(pos, hitCenter);
            Gizmos.DrawWireSphere(hitCenter, meleeHitRadius);
        }
    }
}
