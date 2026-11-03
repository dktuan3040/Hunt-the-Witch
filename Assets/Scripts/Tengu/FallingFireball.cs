using UnityEngine;

/// <summary>
/// Cầu lửa chỉ để hiển thị: xuất hiện phía trên mục tiêu rồi rơi xuống, đáp đúng lúc hết duration.
/// Lấy sprite + animator từ prefab nguồn (vd. Fireball.prefab) nhưng KHÔNG copy collider/Projectile,
/// nên không gây sát thương (sát thương do FireBallZone xử lý).
/// </summary>
public class FallingFireball : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 endPos;
    private float duration;
    private float elapsed;

    public static FallingFireball Spawn(GameObject sourcePrefab, Vector3 target, Vector3 startOffset,
                                        float duration, float angleOffset, string sortingLayerName, int sortingOrder)
    {
        var go = new GameObject("FallingFireball");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        if (sourcePrefab != null)
        {
            var srcSr = sourcePrefab.GetComponentInChildren<SpriteRenderer>();
            if (srcSr != null)
            {
                sr.sprite = srcSr.sprite;
                sr.color = srcSr.color;
                sr.sharedMaterial = srcSr.sharedMaterial;
                go.transform.localScale = srcSr.transform.lossyScale;
            }

            var srcAnim = sourcePrefab.GetComponentInChildren<Animator>();
            if (srcAnim != null && srcAnim.runtimeAnimatorController != null)
            {
                var anim = go.AddComponent<Animator>();
                anim.runtimeAnimatorController = srcAnim.runtimeAnimatorController;
            }
        }

        // Không có prefab/sprite: dùng hình tròn cam để vẫn nhìn thấy
        if (sr.sprite == null)
        {
            sr.sprite = FireBallZone.GetCircleSprite();
            sr.color = new Color(1f, 0.5f, 0.1f, 1f);
            go.transform.localScale = Vector3.one * 0.8f;
        }

        // Xoay sprite theo hướng bay (sprite gốc quay mặt sang phải). angleOffset dùng để chỉnh nếu sprite quay hướng khác.
        Vector2 flyDir = ((Vector2)(-startOffset)).normalized;
        float angle = Mathf.Atan2(flyDir.y, flyDir.x) * Mathf.Rad2Deg + angleOffset;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var fb = go.AddComponent<FallingFireball>();
        fb.endPos = target;
        fb.startPos = target + startOffset;
        fb.duration = Mathf.Max(0.05f, duration);
        go.transform.position = fb.startPos;
        return fb;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float p = Mathf.Clamp01(elapsed / duration);
        float eased = p * p; // rơi nhanh dần
        transform.position = Vector3.Lerp(startPos, endPos, eased);

        if (p >= 1f) Destroy(gameObject);
    }
}
