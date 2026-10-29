using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAttackType
{
    Melee,  // chạy lại gần rồi đánh cận chiến
    Ranged  // đứng cách xa và bắn đạn
}

/// <summary>
/// AI enemy dùng chung: phát hiện player trong bán kính -> đuổi theo -> dừng ở tầm đánh -> tấn công.
/// Mỗi loại enemy chỉ cần chỉnh thông số (cận chiến / tầm xa) trên Inspector.
///
/// Animator (EnemyBase) dùng 3 bool:
///   isChasing : đang di chuyển (đuổi theo / lùi lại)
///   isAttack  : đang trong animation đánh (bật suốt attackDuration)
///   isDead    : do component Health bật khi hết máu
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Phát hiện")]
    [Tooltip("Player vào bán kính này thì enemy bắt đầu đuổi")]
    [SerializeField] private float detectionRadius = 6f;
    [Tooltip("Player chạy ra xa hơn bán kính này thì enemy bỏ cuộc (nên lớn hơn detectionRadius)")]
    [SerializeField] private float loseTargetRadius = 8f;

    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 2.5f;
    [Tooltip("Sprite gốc đang quay mặt sang phải? Bỏ tick nếu sprite quay mặt sang trái")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Tấn công chung")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Melee;
    [Tooltip("Cận chiến: khoảng cách dừng lại để đánh. Tầm xa: khoảng cách đứng bắn.")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private int damage = 1;
    [Tooltip("Thời gian nghỉ giữa 2 đòn (giây)")]
    [SerializeField] private float attackCooldown = 1f;
    [Tooltip("Tổng thời gian animation Attack (giây). Bool isAttack được bật suốt khoảng này.")]
    [SerializeField] private float attackDuration = 0.85f;
    [Tooltip("Thời điểm đòn đánh trúng / đạn bay ra, tính từ lúc bắt đầu animation Attack (giây)")]
    [SerializeField] private float attackHitTime = 0.4f;
    [Tooltip("Bật = đòn đánh gây sát thương qua Animation Event gọi DealDamage() thay vì tự tính theo attackHitTime")]
    [SerializeField] private bool damageByAnimationEvent = false;


    [Header("Cận chiến")]
    [Tooltip("Tâm vùng đánh cách enemy bao xa (về phía player)")]
    [SerializeField] private float meleeReach = 0.6f;
    [SerializeField] private float meleeHitRadius = 0.6f;
    [Tooltip("Layer của mục tiêu bị đánh. Để trống = layer Player")]
    [SerializeField] private LayerMask targetLayers;

    [Header("Tầm xa")]
    [Tooltip("Prefab đạn có component Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Vị trí đạn bay ra. Để trống = tâm enemy")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float projectileLifeTime = 3f;
    [Tooltip("Player lại gần hơn khoảng này thì enemy lùi ra (0 = không lùi)")]
    [SerializeField] private float retreatDistance = 0f;

    [Header("Animator")]
    [SerializeField] private string isChasingParameter = "isChasing";
    [SerializeField] private string isAttackParameter = "isAttack";

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
    // (không kiểm tra tham số Animator lúc Awake vì Animator có thể chưa initialize)
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
        // Tên tham số sai sẽ được Unity cảnh báo khi SetBool
    }

    private void OnDisable()
    {
        // Health tắt script này khi chết: dừng đòn đánh đang chạy dở
        StopAllCoroutines();
        isAttacking = false;
        desiredVelocity = Vector2.zero;
    }

    private void OnValidate()
    {
        loseTargetRadius = Mathf.Max(loseTargetRadius, detectionRadius);
        attackDuration = Mathf.Max(0.05f, attackDuration);
        attackHitTime = Mathf.Clamp(attackHitTime, 0f, attackDuration);
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

        // Đang đánh: đứng yên, giữ nguyên hướng nhìn
        if (isAttacking)
        {
            desiredVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        float distance = toTarget.magnitude;
        Vector2 direction = distance > 0.0001f ? toTarget / distance : Vector2.right;

        FaceTowards(toTarget.x);

        // Bán kính phát hiện (có độ trễ để không bị chớp khi player đứng sát ranh giới)
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

        // Tầm xa: player quá gần thì lùi ra
        if (attackType == EnemyAttackType.Ranged && retreatDistance > 0f && distance < retreatDistance)
        {
            desiredVelocity = -direction * moveSpeed;
            SetChasing(true);
            return;
        }

        // Trong tầm đánh: dừng lại, đánh khi hết cooldown
        if (distance <= attackRange)
        {
            desiredVelocity = Vector2.zero;
            SetChasing(false);

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
            return;
        }

        // Ngoài tầm đánh: đuổi theo
        desiredVelocity = direction * moveSpeed;
        SetChasing(true);
    }

    private void FixedUpdate()
    {
        if (health.IsDead) return;

        // Đang bị knockback thì để HitFeedback điều khiển velocity
        if (hitFeedback != null && hitFeedback.IsKnockedBack) return;

        rb.linearVelocity = desiredVelocity;
    }

private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        desiredVelocity = Vector2.zero;
        SetChasing(false);
        SetAttack(true);

        yield return new WaitForSeconds(attackHitTime);

        // Nếu dùng Animation Event thì sát thương do DealDamage() xử lý
        if (!damageByAnimationEvent && !health.IsDead) PerformAttack();

        yield return new WaitForSeconds(Mathf.Max(0f, attackDuration - attackHitTime));

        SetAttack(false);
        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

/// <summary>Gọi từ Animation Event trong clip Attack (chỉ có tác dụng khi damageByAnimationEvent = true).</summary>
    public void DealDamage()
    {
        if (!damageByAnimationEvent || !isAttacking || health.IsDead) return;
        PerformAttack();
    }


    private void PerformAttack()
    {
        if (!HasValidTarget) return;

        if (attackType == EnemyAttackType.Melee) MeleeHit();
        // else FireProjectile();
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
            if (!alreadyHit.Add(victim)) continue; // 1 nạn nhân chỉ ăn 1 đòn dù có nhiều collider

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
        projectile.SetTeam(health.Team); // đạn của enemy chỉ trúng Player
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



    private void OnDrawGizmosSelected()
    {
        Vector3 p = transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(p, loseTargetRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(p, attackRange);

        if (attackType == EnemyAttackType.Ranged && retreatDistance > 0f)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(p, retreatDistance);
        }

        if (attackType == EnemyAttackType.Melee)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.6f);
            Gizmos.DrawWireSphere(p + Vector3.right * meleeReach, meleeHitRadius);
        }
    }
}
