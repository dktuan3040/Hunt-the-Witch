using UnityEngine;

/// <summary>
/// Cầu lửa bám đuổi (homing) của EvilWitch.
/// Liên tục xoay hướng về Player cho tới khi chạm Wall (hoặc trúng Player / hết maxLifeTime).
///
/// Prefab cần có: Rigidbody2D (Gravity Scale = 0) + Collider2D (Is Trigger) + SpriteRenderer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HomingFireball : MonoBehaviour
{
    [Header("Di chuyển")]
    [SerializeField] private float speed = 4f;
    [Tooltip("Tốc độ xoay hướng về Player (độ/giây). Thấp = dễ né, cao = bám rất sát.")]
    [SerializeField] private float turnSpeed = 150f;
    [Tooltip("Bù góc sprite (nếu sprite mặc định hướng lên thì để -90).")]
    [SerializeField] private float spriteOffset = 0f;

    [Header("Va chạm")]
    [Tooltip("Layer khiến cầu lửa nổ. Để trống = tự dùng layer 'Wall'.")]
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private Team team = Team.Enemy;
    [SerializeField] private int damage = 1;
    [Tooltip("Chốt an toàn: tự hủy sau số giây này nếu chưa chạm gì.")]
    [SerializeField] private float maxLifeTime = 12f;

    [Header("Hiệu ứng nổ")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private AudioClip impactSound;

    private Rigidbody2D rb;
    private Transform target;
    private Vector2 direction = Vector2.right;
    private bool consumed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        if (wallLayers.value == 0)
            wallLayers = LayerMask.GetMask("Wall");
    }

    /// <summary>Khởi tạo cầu lửa. Gọi ngay sau Instantiate.</summary>
    public void Initialize(Transform homingTarget, Vector2 startDirection, Team ownerTeam,
                           float customSpeed = -1f, float customTurnSpeed = -1f,
                           int customDamage = -1, float customLifeTime = -1f)
    {
        target = homingTarget;
        team = ownerTeam;

        if (customSpeed > 0f) speed = customSpeed;
        if (customTurnSpeed >= 0f) turnSpeed = customTurnSpeed;
        if (customDamage >= 0) damage = customDamage;
        if (customLifeTime > 0f) maxLifeTime = customLifeTime;

        direction = startDirection.sqrMagnitude > 0.0001f ? startDirection.normalized : Vector2.right;
        ApplyMovement();

        Destroy(gameObject, maxLifeTime);
    }

    private void FixedUpdate()
    {
        if (consumed) return;

        if (target != null)
        {
            Vector2 toTarget = (Vector2)target.position - rb.position;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float current = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float desired = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
                float next    = Mathf.MoveTowardsAngle(current, desired, turnSpeed * Time.fixedDeltaTime);
                float rad     = next * Mathf.Deg2Rad;
                direction     = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            }
        }
        // target == null (Player chết/biến mất): bay thẳng theo hướng cuối cùng cho tới khi chạm Wall.

        ApplyMovement();
    }

    private void ApplyMovement()
    {
        rb.linearVelocity = direction * speed;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteOffset);
    }

    // ─── Va chạm ───────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other) => HandleHit(other);
    private void OnCollisionEnter2D(Collision2D collision) => HandleHit(collision.collider);

    private void HandleHit(Collider2D other)
    {
        if (consumed) return;

        // 1) Trúng đối tượng nhận sát thương (khác phe)
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            if (damageable.Team == team || damageable.IsDead) return;

            damageable.TakeDamage(damage, direction);
            Explode();
            return;
        }

        // 2) Chạm Wall
        if (((1 << other.gameObject.layer) & wallLayers.value) != 0)
            Explode();
    }

    private void Explode()
    {
        consumed = true;
        rb.linearVelocity = Vector2.zero;

        if (impactVfxPrefab != null)
            Instantiate(impactVfxPrefab, transform.position, Quaternion.identity);

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position);

        Destroy(gameObject);
    }
}
