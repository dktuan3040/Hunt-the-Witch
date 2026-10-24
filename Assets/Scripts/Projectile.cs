using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Data (Tùy chọn - ScriptableObject)")]
    [Tooltip("Gán ProjectileData để dùng các thông số định nghĩa sẵn, hoặc để trống để dùng thông số bên dưới.")]
    [SerializeField] private ProjectileData projectileData;

    [Header("Phe")]
    [Tooltip("Phe của người bắn. Đạn chỉ gây sát thương cho phe khác và bay xuyên qua phe mình. Đạn của enemy: để Enemy (hoặc EnemyAI sẽ tự set).")]
    [SerializeField] private Team team = Team.Player;

    
[Header("Visual References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Fallback Settings (Dùng khi không gắn ProjectileData)")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private int pierceCount = 1;
    [SerializeField] private float spriteOffset = -90f;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private AudioClip impactSound;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private int currentPierce;
    private int finalDamage;
    private bool isInitialized = false;
    private bool consumed = false; // đã hết xuyên thấu / đã nổ, bỏ qua va chạm thừa trong cùng frame

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Đặt phe cho đạn (EnemyAI gọi khi bắn để đạn chỉ trúng Player).</summary>
    public void SetTeam(Team newTeam)
    {
        team = newTeam;
    }

    
void Start()
    {
        if (!isInitialized)
        {
            float currentLifeTime = projectileData != null ? projectileData.LifeTime : lifeTime;
            Destroy(gameObject, currentLifeTime);
        }
    }

    /// <summary>
    /// Khởi tạo đạn theo hướng bay, nhận tốc độ, tầm bay (lifetime), damage từ gậy hoặc ProjectileData từ skill Ultimate
    /// </summary>
    public void Initialize(
        Vector2 direction, 
        float damageMultiplier = 1f, 
        ProjectileData customData = null, 
        float customSpeed = -1f, 
        float customLifeTime = -1f, 
        int customDamage = -1,
        Sprite customSprite = null)
    {
        isInitialized = true;
        projectileData = customData;

        // Cập nhật Sprite và Animator
        ApplyVisualData(customSprite);

        moveDirection = direction.normalized;

        // Tốc độ: ưu tiên customSpeed từ gậy, sau đó ProjectileData, sau đó fallback speed
        float currentSpeed = customSpeed > 0f ? customSpeed 
            : (projectileData != null ? projectileData.Speed : speed);

        float currentOffset = projectileData != null ? projectileData.SpriteOffset : spriteOffset;

        // Sát thương: ưu tiên customDamage từ gậy, sau đó ProjectileData, sau đó baseDamage
        int rawDamage = customDamage > 0 ? customDamage 
            : (projectileData != null ? projectileData.Damage : baseDamage);

        currentPierce = projectileData != null ? projectileData.PierceCount : pierceCount;
        finalDamage = Mathf.RoundToInt(rawDamage * damageMultiplier);

        // Đặt vận tốc
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = moveDirection * currentSpeed;

        // Xoay hướng đạn theo chiều di chuyển
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + currentOffset);

        // Thời gian tồn tại theo tầm bay (range) của gậy hoặc lifetime của đạn
        float currentLifeTime = customLifeTime > 0f ? customLifeTime 
            : (projectileData != null ? projectileData.LifeTime : lifeTime);
        Destroy(gameObject, currentLifeTime);
    }

    private void ApplyVisualData(Sprite customSprite = null)
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (customSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = customSprite;
        }

        // Nếu là chiêu Ultimate có ProjectileData riêng
        if (projectileData != null)
        {
            if (spriteRenderer != null)
            {
                if (projectileData.ProjectileSprite != null)
                {
                    spriteRenderer.sprite = projectileData.ProjectileSprite;
                }
                spriteRenderer.color = projectileData.SpriteColor;
            }

            if (animator != null && projectileData.AnimatorController != null)
            {
                animator.enabled = true;
                animator.runtimeAnimatorController = projectileData.AnimatorController;
            }

            if (projectileData.ProjectileScale != Vector3.one)
            {
                transform.localScale = projectileData.ProjectileScale;
            }
        }
        else
        {
            // Bắn thường (Primary): không dùng skill visual
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }

            // Nếu gán sprite tĩnh cho đòn thường thì tắt animator để không bị đè sprite
            if (customSprite != null && animator != null)
            {
                animator.enabled = false;
            }
        }
    }

private void OnTriggerEnter2D(Collider2D collision)
    {
        // Đạn đã hết xuyên thấu (đang chờ bị Destroy cuối frame)
        if (consumed) return;

        // Đối tượng nhận sát thương (Player, Enemy, Boss, Thùng vỡ...)
        var damageable = collision.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            // Cùng phe hoặc đã chết thì đạn bay xuyên qua
            if (damageable.Team == team || damageable.IsDead) return;

            // Health lo trừ máu, hiệu ứng dính đòn (nháy đỏ + knockback) và animation chết
            damageable.TakeDamage(finalDamage, moveDirection);

            SpawnImpactEffects();
            currentPierce--;

            if (currentPierce <= 0)
            {
                consumed = true;
                Destroy(gameObject);
            }
            return;
        }

        // Chạm tường (layer Wall)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            consumed = true;
            SpawnImpactEffects();
            Destroy(gameObject);
        }
    }

    private void SpawnImpactEffects()
    {
        GameObject vfx = projectileData != null ? projectileData.ImpactVfxPrefab : impactVfxPrefab;
        if (vfx != null)
        {
            Instantiate(vfx, transform.position, Quaternion.identity);
        }

        AudioClip sound = projectileData != null ? projectileData.ImpactSound : impactSound;
        if (sound != null)
        {
            AudioSource.PlayClipAtPoint(sound, transform.position);
        }
    }
}
