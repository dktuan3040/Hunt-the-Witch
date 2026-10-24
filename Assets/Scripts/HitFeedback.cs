using UnityEngine;

/// <summary>
/// Hiệu ứng bị đánh trúng bằng code (không cần animation):
/// - Nhấp nháy đỏ toàn bộ SpriteRenderer rồi mờ dần về màu gốc
/// - Knockback nhẹ theo hướng đòn đánh (cần Rigidbody2D)
/// Gắn cho cả Player và Enemy. Gọi OnHit(...) khi bị trúng đòn.
/// </summary>
[DisallowMultipleComponent]
public class HitFeedback : MonoBehaviour
{
    [Header("Flash đỏ")]
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [Tooltip("Thời gian giữ nguyên màu đỏ")]
    [SerializeField] private float flashHoldTime = 0.06f;
    [Tooltip("Thời gian mờ dần từ đỏ về màu gốc")]
    [SerializeField] private float flashFadeTime = 0.14f;

    [Header("Knockback")]
    [Tooltip("Quãng đường bị đẩy lùi (đơn vị Unity)")]
    [SerializeField] private float knockbackDistance = 0.6f;
    [SerializeField] private float knockbackDuration = 0.15f;
    [Tooltip("0 = bị đẩy bình thường, 1 = miễn nhiễm knockback (dùng cho boss)")]
    [Range(0f, 1f)]
    [SerializeField] private float knockbackResistance = 0f;

    private SpriteRenderer[] renderers;
    private Color[] originalColors;
    private Rigidbody2D rb;

    private float flashTimer;
    private bool isFlashing;

    private float knockbackTimer;
    private Vector2 knockbackStartVelocity;

    /// <summary>
    /// Script di chuyển (Player/Enemy) cần kiểm tra cờ này và KHÔNG ghi đè velocity khi đang bị đẩy lùi.
    /// </summary>
    public bool IsKnockedBack { get; private set; }

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnDisable()
    {
        RestoreColors();
        isFlashing = false;
        IsKnockedBack = false;
        knockbackTimer = 0f;
    }

    /// <summary>
    /// Gọi khi bị đánh. pushDirection là hướng đẩy (từ người đánh về phía nạn nhân).
    /// </summary>
    public void OnHit(Vector2 pushDirection)
    {
        Flash();
        Knockback(pushDirection);
    }

    /// <summary>
    /// Gọi khi bị đánh, truyền vị trí nguồn gây sát thương (quái, bẫy, vụ nổ...).
    /// </summary>
    public void OnHitFromPoint(Vector2 sourcePosition)
    {
        Vector2 dir = (Vector2)transform.position - sourcePosition;
        OnHit(dir);
    }

    public void Flash()
    {
        if (renderers == null) return;

        // Nếu đang flash dở thì trả màu gốc trước để không lưu nhầm màu đỏ làm "màu gốc"
        if (isFlashing) RestoreColors();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            originalColors[i] = renderers[i].color;
            renderers[i].color = flashColor;
        }

        flashTimer = 0f;
        isFlashing = true;
    }

    public void Knockback(Vector2 pushDirection)
    {
        if (rb == null || knockbackResistance >= 1f) return;
        if (pushDirection.sqrMagnitude < 0.0001f) return;

        float duration = Mathf.Max(0.01f, knockbackDuration);
        float distance = knockbackDistance * (1f - knockbackResistance);

        // Vận tốc giảm tuyến tính về 0 => quãng đường = v0 * duration / 2
        float startSpeed = 2f * distance / duration;
        knockbackStartVelocity = pushDirection.normalized * startSpeed;
        knockbackTimer = duration;
        IsKnockedBack = true;

        rb.linearVelocity = knockbackStartVelocity;
    }

    private void Update()
    {
        if (!isFlashing) return;

        flashTimer += Time.deltaTime;

        if (flashTimer <= flashHoldTime)
        {
            return; // đang giữ màu đỏ
        }

        float t = (flashTimer - flashHoldTime) / Mathf.Max(0.01f, flashFadeTime);
        if (t >= 1f)
        {
            RestoreColors();
            isFlashing = false;
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = Color.Lerp(flashColor, originalColors[i], t);
        }
    }

    private void FixedUpdate()
    {
        if (!IsKnockedBack) return;

        knockbackTimer -= Time.fixedDeltaTime;
        if (knockbackTimer <= 0f)
        {
            IsKnockedBack = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float t = knockbackTimer / Mathf.Max(0.01f, knockbackDuration);
        if (rb != null) rb.linearVelocity = knockbackStartVelocity * t;
    }

    private void RestoreColors()
    {
        if (renderers == null || !isFlashing) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = originalColors[i];
        }
    }
}
