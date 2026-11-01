using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class TenguAttack : MonoBehaviour
{
    [Header("Phát hiện")]
    [SerializeField] private float detectionRadius = 6f;
    [SerializeField] private float loseTargetRadius = 8f;

    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Tấn công chung")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Melee;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private int damage = 1;
    [Tooltip("Thời gian nghỉ sau khi đòn đánh KẾT THÚC (giây)")]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Cận chiến")]
    [SerializeField] private float meleeReach = 0.6f;
    [SerializeField] private float meleeHitRadius = 0.6f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Tầm xa")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileLifeTime = 3f;
    [SerializeField] private float retreatDistance = 0f;

    [Header("Animator")]
    [SerializeField] private string isChasingParameter = "isChasing";
    [SerializeField] private string isAttackParameter = "isAttack";

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody2D rb;
    private Health health;
    private HitFeedback hitFeedback;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Transform target;
    private IDamageable targetDamageable;
    private float nextFindTargetTime;

    private bool hasAggro;
    private bool isAttacking;
    private float nextAttackTime;
    private Vector2 desiredVelocity;

    private int isChasingHash;
    private int isAttackHash;
    private bool animChasing;
    private bool animAttack;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        hitFeedback = GetComponent<HitFeedback>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");

        isChasingHash = Animator.StringToHash(isChasingParameter);
        isAttackHash = Animator.StringToHash(isAttackParameter);
    }

    private void OnDisable()
    {
        isAttacking = false;
        desiredVelocity = Vector2.zero;
        SetAttack(false);
    }

    private void OnValidate()
    {
        // Đứng trong attackRange là phải được phép aggro + đánh trúng
        detectionRadius = Mathf.Max(detectionRadius, attackRange);
        loseTargetRadius = Mathf.Max(loseTargetRadius, detectionRadius);

        // Vòng gây damage phải phủ tới rìa attackRange (meleeReach + meleeHitRadius >= attackRange)
        // -> player đã nằm trong tầm đánh thì Tengu không bao giờ vung hụt vì đứng quá xa
        if (attackType == EnemyAttackType.Melee)
            meleeHitRadius = Mathf.Max(meleeHitRadius, attackRange - meleeReach);
    }

    // =====================================================================
    // DEBUG: vẽ vòng tròn range
    // =====================================================================

    // Chọn Tengu trong Hierarchy là thấy các vòng tròn (sửa số trong Inspector là vòng đổi theo ngay)
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 pos = transform.position;

        Gizmos.color = Color.yellow;                 // detectionRadius: bắt đầu aggro
        Gizmos.DrawWireSphere(pos, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);      // loseTargetRadius: mất aggro
        Gizmos.DrawWireSphere(pos, loseTargetRadius);

        Gizmos.color = Color.red;                    // attackRange: vào tầm thì bắt đầu đánh
        Gizmos.DrawWireSphere(pos, attackRange);

        if (attackType == EnemyAttackType.Melee)     // vòng tròn gây sát thương thật
        {
            Vector2 dir = Vector2.right;
            if (Application.isPlaying && target != null)
            {
                dir = ((Vector2)target.position - (Vector2)pos).normalized;
            }
            else
            {
                var sr = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    bool facingRight = spriteFacesRight ? !sr.flipX : sr.flipX;
                    dir = facingRight ? Vector2.right : Vector2.left;
                }
            }

            Vector2 hitCenter = (Vector2)pos + dir * meleeReach;
            Gizmos.color = Color.magenta;            // meleeReach + meleeHitRadius
            Gizmos.DrawLine(pos, hitCenter);
            Gizmos.DrawWireSphere(hitCenter, meleeHitRadius);
        }
    }

    private void Update()
    {
        if (health.IsDead) return;

        FindTargetIfNeeded();

        if (!HasValidTarget)
        {
            hasAggro = false;
            if (!isAttacking)
            {
                desiredVelocity = Vector2.zero;
                SetChasing(false);
            }
            return;
        }

        // Đang chờ đòn đánh tung ra -> Đứng yên
        if (isAttacking)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;
        Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.right;

        FaceTowards(toTarget.x);

        if (!hasAggro)
        {
            if (distance <= detectionRadius) hasAggro = true;
        }
        else if (distance > loseTargetRadius)
        {
            hasAggro = false;
        }

        if (!hasAggro)
        {
            desiredVelocity = Vector2.zero;
            SetChasing(false);
            return;
        }

        // Rút lui nếu player quá gần (dành cho Ranged)
        if (attackType == EnemyAttackType.Ranged && retreatDistance > 0f && distance < retreatDistance)
        {
            desiredVelocity = -direction * moveSpeed;
            SetChasing(true);
            return;
        }

        // Vào tầm đánh
        if (distance <= attackRange)
        {
            desiredVelocity = Vector2.zero;
            SetChasing(false);

            // Bắt đầu đòn đánh nếu hết Cooldown
            if (Time.time >= nextAttackTime)
            {
                StartAttack();
            }
            return;
        }

        // Ngoài tầm đánh -> tiếp tục đuổi
        desiredVelocity = direction * moveSpeed;
        SetChasing(true);
    }

    private void FixedUpdate()
    {
        if (health.IsDead) return;
        if (hitFeedback != null && hitFeedback.IsKnockedBack) return;

        rb.linearVelocity = desiredVelocity;
    }

    private void StartAttack()
    {
        isAttacking = true;
        desiredVelocity = Vector2.zero;
        SetChasing(false);
        SetAttack(true); // Kích hoạt animation Attack
    }

    // =====================================================================
    // HÀM ANIMATION EVENT DUY NHẤT (GỌI TỪ ANIMATOR)
    // =====================================================================

    /// <summary>
    /// Gắn hàm này vào FRAME CUỐI CÙNG (hoặc frame bạn chỉ định) trong Animation.
    /// Nó sẽ vừa gây sát thương, vừa kết thúc đòn đánh.
    /// </summary>
    public void DealDamage()
    {
        if (health.IsDead || !isAttacking) return;

        // 1. Gây sát thương / Bắn đạn
        PerformAttack();

        // 2. Kết thúc trạng thái đánh và bắt đầu đếm thời gian hồi chiêu
        isAttacking = false;
        SetAttack(false);
        nextAttackTime = Time.time + attackCooldown;
    }

    // =====================================================================

    private void PerformAttack()
    {
        if (!HasValidTarget) return;

        if (attackType == EnemyAttackType.Melee) 
            MeleeHit();
        else 
            FireProjectile();
    }

    private void MeleeHit()
    {
        Vector2 origin = transform.position;
        Vector2 direction = ((Vector2)target.position - origin).normalized;
        Vector2 center = origin + direction * meleeReach;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, meleeHitRadius, targetLayers);
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var col in hits)
        {
            var victim = col.GetComponentInParent<IDamageable>();
            if (victim == null || victim.IsDead || victim.Team == health.Team) continue;
            if (!alreadyHit.Add(victim)) continue; 

            victim.TakeDamage(damage, direction);
        }
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[EnemyAI] '{name}' là Ranged nhưng chưa gán projectilePrefab", this);
            return;
        }

        Vector2 spawn = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 direction = ((Vector2)target.position - spawn).normalized;

        Projectile projectile = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        projectile.SetTeam(health.Team); 
        projectile.Initialize(direction, 1f, null, projectileSpeed, projectileLifeTime, damage);
    }

    private void FindTargetIfNeeded()
    {
        if (target != null || Time.time < nextFindTargetTime) return;

        nextFindTargetTime = Time.time + 0.5f;
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;

        target = playerObject.transform;
        targetDamageable = playerObject.GetComponentInParent<IDamageable>();
    }

    private bool HasValidTarget => target != null && (targetDamageable == null || !targetDamageable.IsDead);

    private void FaceTowards(float deltaX)
    {
        if (spriteRenderer == null || Mathf.Abs(deltaX) < 0.05f) return;

        bool targetIsOnRight = deltaX > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !targetIsOnRight : targetIsOnRight;
    }

    private void SetChasing(bool value)
    {
        if (animator == null || animChasing == value) return;
        animChasing = value;
        animator.SetBool(isChasingHash, value);
    }

    private void SetAttack(bool value)
    {
        if (animator == null || animAttack == value) return;
        animAttack = value;
        animator.SetBool(isAttackHash, value);
    }
}