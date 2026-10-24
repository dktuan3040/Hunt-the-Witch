using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Máu dùng chung cho Player và Enemy.
/// - Trừ máu khi dính đòn, kích hoạt HitFeedback (nháy đỏ + knockback)
/// - Hết máu: bật bool isDead trên Animator, tắt collider + các script khác (AI, di chuyển, bắn...)
/// - Enemy: chờ hết animation Dead rồi mờ dần và biến mất. Player: giữ xác, bắn event OnDied (game over...)
/// </summary>
[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable
{
    [Header("Phe")]
    [SerializeField] private Team team = Team.Enemy;

    [Header("Máu")]
    [SerializeField] private int maxHealth = 5;
    [Tooltip("Thời gian bất tử ngắn sau khi dính đòn (giây). Player nên ~0.5, enemy để 0.")]
    [SerializeField] private float invincibleTime = 0f;

    [Header("Animator")]
    [Tooltip("Để trống sẽ tự tìm Animator trên object này")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isDeadParameter = "isDead";
    [Tooltip("Tên state Dead trong Animator Controller (dùng để lấy độ dài animation chết)")]
    [SerializeField] private string deadStateName = "Dead";

    [Header("Khi chết")]
    [Tooltip("Tắt mọi script khác trên object (AI, di chuyển, bắn...) và collider khi chết")]
    [SerializeField] private bool disableOtherBehavioursOnDeath = true;
    [Tooltip("Enemy: bật. Player: tắt (giữ lại xác).")]
    [SerializeField] private bool destroyOnDeath = true;
    [Tooltip("Thời gian chờ trước khi mờ dần nếu state Dead không có animation (giây)")]
    [SerializeField] private float fallbackDeathDelay = 1f;
    [SerializeField] private float fadeOutTime = 0.5f;
    [Tooltip("Các renderer cần ẩn ngay khi chết (vd: vũ khí cầm tay của Player)")]
    [SerializeField] private Renderer[] hideOnDeath;


    [Header("Events")]
    public UnityEvent onDied;

    /// <summary>(máu hiện tại, máu tối đa)</summary>
    public event Action<int, int> OnHealthChanged;
    /// <summary>Số sát thương vừa nhận</summary>
    public event Action<int> OnDamaged;
    public event Action OnDied;

    public Team Team => team;
    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public float HealthPercent => maxHealth > 0 ? (float)CurrentHealth / maxHealth : 0f;

    private HitFeedback hitFeedback;
    private Rigidbody2D rb;
    private float invincibleUntil;
    // (isDead được set trực tiếp trong Die())

    private void Awake()
    {
        CurrentHealth = maxHealth;
        hitFeedback = GetComponent<HitFeedback>();
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        // Không kiểm tra tham số Animator ở Awake: Animator (nhất là Override Controller) có thể chưa initialize.
        // Nếu tên tham số sai, Unity sẽ tự cảnh báo trong Console khi SetBool.
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (IsDead || damage <= 0) return;
        if (Time.time < invincibleUntil) return;

        if (invincibleTime > 0f) invincibleUntil = Time.time + invincibleTime;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        // Hiệu ứng dính đòn: nháy đỏ + knockback (hướng bằng 0 thì chỉ nháy đỏ)
        if (hitFeedback != null) hitFeedback.OnHit(hitDirection);

        OnDamaged?.Invoke(damage);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (animator != null)
        {
            animator.SetBool(isDeadParameter, true);
        }

        // Vô hiệu hóa va chạm: xác không chặn đạn, không nhận thêm sát thương
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }

        // Tắt các script khác (AI, di chuyển, bắn...). Giữ lại Health và HitFeedback
        if (disableOtherBehavioursOnDeath)
        {
            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>())
            {
                if (behaviour == this || behaviour is HitFeedback) continue;
                behaviour.enabled = false;
            }
        }

        // Ẩn các renderer chỉ định (vd: cây gậy của Player)
        if (hideOnDeath != null)
        {
            foreach (var r in hideOnDeath)
            {
                if (r != null) r.enabled = false;
            }
        }

        
// Đứng yên (nếu đang bị knockback thì để HitFeedback tự dừng khi hết knockback)
        bool knockedBack = hitFeedback != null && hitFeedback.IsKnockedBack;
        if (rb != null && !knockedBack) rb.linearVelocity = Vector2.zero;

        OnDied?.Invoke();
        onDied?.Invoke();

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float wait = fallbackDeathDelay;

        // Chờ Animator chuyển sang state Dead rồi lấy độ dài animation
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            yield return null;
            yield return null;

            AnimatorStateInfo info = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName(deadStateName) && info.length > 0.05f && !float.IsInfinity(info.length))
            {
                wait = info.length;
            }
        }

        if (!destroyOnDeath) yield break;

        yield return new WaitForSeconds(wait);

        // Mờ dần rồi xóa
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        var startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) startColors[i] = renderers[i].color;

        float t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOutTime));
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = startColors[i];
                c.a = startColors[i].a * a;
                renderers[i].color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }


}
