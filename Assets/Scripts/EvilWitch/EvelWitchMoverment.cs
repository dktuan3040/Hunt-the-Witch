using UnityEngine;

/// <summary>
/// Di chuyển của EvilWitch: phát hiện Player -> đuổi theo -> dừng khi vào tầm đánh (attackRange của EvilWitchAttack).
/// Animator: isWalking (bool).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(EvilWitchAttack))]
public class EvelWitchMoverment : MonoBehaviour
{
    [Header("Phát hiện")]
    [Tooltip("Player vào bán kính này thì Witch bắt đầu đuổi")]
    [SerializeField] private float detectionRadius = 7f;
    [Tooltip("Player chạy ra xa hơn bán kính này thì Witch bỏ cuộc (nên lớn hơn detectionRadius)")]
    [SerializeField] private float loseTargetRadius = 9f;

    [Header("Di chuyển")]
    [SerializeField] private float moveSpeed = 2.2f;
    [Tooltip("Sprite gốc đang quay mặt sang phải? Bỏ tick nếu sprite quay mặt sang trái")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Animator")]
    [SerializeField] private string isWalkingParameter = "isWalking";

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    // EvilWitchAttack đọc các giá trị này
    public Transform Target { get; private set; }
    public bool HasAggro { get; private set; }
    public float DistanceToTarget { get; private set; }
    public Vector2 DirectionToTarget { get; private set; } = Vector2.right;

    private Rigidbody2D rb;
    private Health health;
    private HitFeedback hitFeedback;
    private EvilWitchAttack attack;
    private EnemySkillStatus skillStatus;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private IDamageable targetDamageable;
    private float nextFindTargetTime;
    private Vector2 desiredVelocity;

    private int isWalkingHash;
    private bool animWalking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        hitFeedback = GetComponent<HitFeedback>();
        attack = GetComponent<EvilWitchAttack>();
        TryGetComponent(out skillStatus);
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        isWalkingHash = Animator.StringToHash(isWalkingParameter);
    }

    private void OnDisable()
    {
        // Health tắt script này khi Witch chết
        desiredVelocity = Vector2.zero;
        HasAggro = false;
        SetWalking(false);
    }

    private void OnValidate()
    {
        loseTargetRadius = Mathf.Max(loseTargetRadius, detectionRadius);
    }

    private void Update()
    {
        if (health.IsDead) return;

        FindTargetIfNeeded();

        if (!HasValidTarget)
        {
            HasAggro = false;
            Stop();
            return;
        }

        Vector2 toTarget = (Vector2)Target.position - (Vector2)transform.position;
        DistanceToTarget = toTarget.magnitude;
        DirectionToTarget = DistanceToTarget > 0.0001f ? toTarget / DistanceToTarget : Vector2.right;

        // Bán kính phát hiện có độ trễ để Witch không bị chớp khi Player đứng sát ranh giới
        if (!HasAggro)
        {
            if (DistanceToTarget <= detectionRadius) HasAggro = true;
        }
        else if (DistanceToTarget > loseTargetRadius)
        {
            HasAggro = false;
        }

        // Đang bị đóng băng / giữ chân bởi skill của Player
        if (skillStatus != null && skillStatus.Immobilized)
        {
            Stop();
            return;
        }

        // Đang đánh: đứng yên, giữ nguyên hướng nhìn
        if (attack.IsAttacking)
        {
            Stop();
            return;
        }

        if (!HasAggro)
        {
            Stop();
            return;
        }

        FaceTowards(toTarget.x);

        // Vào tầm đánh: dừng lại, EvilWitchAttack sẽ ra đòn
        if (DistanceToTarget <= attack.AttackRange)
        {
            Stop();
            return;
        }

        // Ngoài tầm đánh: đuổi theo
        desiredVelocity = DirectionToTarget * moveSpeed;
        SetWalking(true);
    }

    private void FixedUpdate()
    {
        if (health.IsDead) return;

        // Đang bị knockback thì để HitFeedback điều khiển velocity
        if (hitFeedback != null && hitFeedback.IsKnockedBack) return;

        float speedFactor = skillStatus != null ? skillStatus.MovementMultiplier : 1f;
        rb.linearVelocity = desiredVelocity * speedFactor;
    }

    private void Stop()
    {
        desiredVelocity = Vector2.zero;
        SetWalking(false);
    }

    private void FindTargetIfNeeded()
    {
        if (Target != null || Time.time < nextFindTargetTime) return;

        nextFindTargetTime = Time.time + 0.5f;
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;

        Target = playerObject.transform;
        targetDamageable = playerObject.GetComponentInParent<IDamageable>();
    }

    private bool HasValidTarget => Target != null && (targetDamageable == null || !targetDamageable.IsDead);

    private void FaceTowards(float deltaX)
    {
        if (spriteRenderer == null || Mathf.Abs(deltaX) < 0.05f) return;

        bool targetIsOnRight = deltaX > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !targetIsOnRight : targetIsOnRight;
    }

    private void SetWalking(bool value)
    {
        if (animator == null || animWalking == value) return;
        animWalking = value;
        animator.SetBool(isWalkingHash, value);
    }

    // Chọn Witch trong Hierarchy là thấy vòng tròn range
    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        Vector3 pos = transform.position;

        Gizmos.color = Color.yellow; // detectionRadius: bắt đầu đuổi
        Gizmos.DrawWireSphere(pos, detectionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f); // loseTargetRadius: bỏ cuộc
        Gizmos.DrawWireSphere(pos, loseTargetRadius);
    }
}

