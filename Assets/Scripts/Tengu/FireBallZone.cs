using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vùng cảnh báo đỏ: hiện ra trước, fill lớn dần theo warningTime,
/// hết giờ thì nổ và gây sát thương cho mọi thứ khác team trong bán kính.
/// Không cần prefab sẵn: nếu không có SpriteRenderer thì tự tạo hình tròn.
/// </summary>
public class FireBallZone : MonoBehaviour
{
    [Header("Visual")]
    public Color warningColor = new Color(1f, 0.1f, 0.1f, 0.35f);
    public Color impactColor = new Color(1f, 0.6f, 0.1f, 0.9f);
    [Tooltip("Sorting Layer của vùng đỏ + cầu lửa. Phải nằm trên layer của nền (Ground/Background)")]
    public string sortingLayerName = "Player";
    public int sortingOrder = -5; // vùng đỏ nằm dưới player, cầu lửa nằm trên

    [Header("Falling fireball")]
    [Tooltip("Prefab cầu lửa (chỉ lấy sprite + animation để hiển thị)")]
    public GameObject fallingFireballPrefab;
    [Tooltip("Cầu lửa bắt đầu từ cao hơn vùng đỏ bấy nhiêu unit")]
    public float fallHeight = 6f;
    [Tooltip("Cầu lửa bắt đầu lệch sang TRÁI vùng đỏ bấy nhiêu unit (góc trên-trái)")]
    public float fallOffsetX = 6f;
    [Tooltip("Chỉnh góc nếu sprite không quay mặt sang phải (vd. 90, -90, 180)")]
    public float spriteAngleOffset = 0f;


    private float radius;
    private float warningTime;
    private int damage;
    private Health owner;
    private LayerMask targetLayers;

    private SpriteRenderer outline;
    private SpriteRenderer fill;

    private static Sprite circleSprite;

    public void Setup(float radius, float warningTime, int damage, Health owner, LayerMask targetLayers)
    {
        this.radius = radius;
        this.warningTime = Mathf.Max(0.05f, warningTime);
        this.damage = damage;
        this.owner = owner;
        this.targetLayers = targetLayers;

        BuildVisual();
        StartCoroutine(Routine());
    }

    private void BuildVisual()
    {
        // Vòng ngoài: cố định, thể hiện vùng sẽ bị trúng
        outline = GetComponent<SpriteRenderer>();
        if (outline == null) outline = gameObject.AddComponent<SpriteRenderer>();
        if (outline.sprite == null) outline.sprite = GetCircleSprite(); // giữ sprite của prefab nếu có
        outline.color = warningColor;
        outline.sortingLayerName = sortingLayerName;
        outline.sortingOrder = sortingOrder;
        float baseSize = Mathf.Max(0.0001f, outline.sprite.bounds.size.x);
        transform.localScale = Vector3.one * (radius * 2f / baseSize);

        // Phần fill: lớn dần từ 0 -> đầy, cho người chơi biết còn bao lâu nữa nổ
        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(transform, false);
        fill = fillObj.AddComponent<SpriteRenderer>();
        fill.sprite = outline.sprite;
        fill.color = new Color(warningColor.r, warningColor.g, warningColor.b, Mathf.Min(1f, warningColor.a + 0.35f));
        fill.sortingLayerName = sortingLayerName;
        fill.sortingOrder = sortingOrder + 1;
        fillObj.transform.localScale = Vector3.zero;
    }

    private IEnumerator Routine()
    {
        // Cầu lửa rơi từ trên trời xuống đúng lúc vùng đỏ đầy
        FallingFireball.Spawn(fallingFireballPrefab, transform.position, new Vector3(-fallOffsetX, fallHeight, 0f), warningTime, spriteAngleOffset, sortingLayerName, sortingOrder + 25);

        float t = 0f;
        while (t < warningTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / warningTime);
            fill.transform.localScale = Vector3.one * p;
            yield return null;
        }

        Explode();

        outline.color = impactColor;
        fill.color = impactColor;
        fill.transform.localScale = Vector3.one;
        yield return new WaitForSeconds(0.15f);
        Destroy(gameObject);
    }

    private void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var col in hits)
        {
            var victim = col.GetComponentInParent<IDamageable>();
            if (victim == null || victim.IsDead) continue;
            if (owner != null && victim.Team == owner.Team) continue;
            if (!alreadyHit.Add(victim)) continue;

            Vector2 dir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
            victim.TakeDamage(damage, dir);
        }
    }

    public static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = d <= 1f ? Mathf.Clamp01((1f - d) * 20f) : 0f; // viền mềm
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
