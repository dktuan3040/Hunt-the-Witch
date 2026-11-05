using System.Collections;
using UnityEngine;

/// <summary>
/// Chiêu "Spray Fireball" của EvilWitch:
/// Phóng <numberOfFireballs> cầu lửa tản ra trong cung <spreadAngle>°, căn giữa hướng Player.
///
/// Cách kích hoạt:
///   - Gọi FireSpreadAttack() từ EvilWitchSkillSequencer (qua Animation Event).
///   - Hoặc gọi trực tiếp từ Animation Event khác nếu muốn dùng riêng.
/// </summary>
public class EvilWitchSpreadFireball : MonoBehaviour
{
    // ─── Cấu hình đạn ──────────────────────────────────────────────────────────
    [Header("Cầu lửa")]
    [Tooltip("Prefab đạn có component Projectile")]
    [SerializeField] private Projectile fireballPrefab;

    [Tooltip("Điểm phóng đạn. Để trống = tâm Witch")]
    [SerializeField] private Transform firePoint;

    // ─── Số lượng & góc ────────────────────────────────────────────────────────
    [Header("Số lượng & Góc")]
    [Tooltip("Tổng số cầu lửa phóng ra mỗi lần")]
    [SerializeField, Range(1, 20)] private int numberOfFireballs = 5;

    [Tooltip("Góc cung tổng (°). Các viên được chia đều, căn giữa hướng Player.\n" +
             "Ví dụ: 60° → viên ngoài cùng bẻ ±30° so với hướng thẳng.\n" +
             "0° = tất cả bay thẳng vào Player.")]
    [SerializeField, Range(0f, 360f)] private float spreadAngle = 60f;

    [Tooltip("Góc quay ngẫu nhiên thêm vào mỗi viên (± nửa giá trị này). 0 = không ngẫu nhiên")]
    [SerializeField, Range(0f, 45f)] private float randomAngleJitter = 0f;

    // ─── Thông số đạn ──────────────────────────────────────────────────────────
    [Header("Thông số đạn")]
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float projectileLifeTime = 3f;
    [SerializeField] private int damage = 1;

    // ─── Delay giữa các viên ───────────────────────────────────────────────────
    [Header("Delay giữa các viên (tùy chọn)")]
    [Tooltip("0 = phóng tất cả cùng lúc. >0 = delay giây giữa mỗi viên (burst effect)")]
    [SerializeField, Min(0f)] private float delayBetweenShots = 0f;

    // ─── References nội bộ ─────────────────────────────────────────────────────
    private EvelWitchMoverment movement;
    private Health health;

    // ───────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        movement = GetComponent<EvelWitchMoverment>();
        health   = GetComponent<Health>();
    }

    private void OnValidate()
    {
        numberOfFireballs  = Mathf.Max(1, numberOfFireballs);
        projectileSpeed    = Mathf.Max(0.1f, projectileSpeed);
        projectileLifeTime = Mathf.Max(0.1f, projectileLifeTime);
        damage             = Mathf.Max(0, damage);
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phóng toàn bộ cầu lửa hướng Player.
    /// Gọi từ EvilWitchSkillSequencer hoặc Animation Event.
    /// </summary>
    public void FireSpreadAttack()
    {
        if (fireballPrefab == null)
        {
            Debug.LogWarning($"[EvilWitchSpreadFireball] '{name}': chưa gán fireballPrefab!", this);
            return;
        }

        if (movement == null || movement.Target == null) return;

        if (delayBetweenShots > 0f)
            StartCoroutine(BurstRoutine());
        else
            SpawnAllFireballs();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Nội bộ
    // ───────────────────────────────────────────────────────────────────────────

    private IEnumerator BurstRoutine()
    {
        Vector2 spawnPos = GetSpawnPos();
        Vector2 baseDir  = GetDirectionToPlayer(spawnPos);
        float[] angles   = CalculateAngles(baseDir);

        foreach (float angle in angles)
        {
            SpawnOneBall(spawnPos, angle);
            yield return new WaitForSeconds(delayBetweenShots);
        }
    }

    private void SpawnAllFireballs()
    {
        Vector2 spawnPos = GetSpawnPos();
        Vector2 baseDir  = GetDirectionToPlayer(spawnPos);
        float[] angles   = CalculateAngles(baseDir);

        foreach (float angle in angles)
            SpawnOneBall(spawnPos, angle);
    }

    private float[] CalculateAngles(Vector2 baseDir)
    {
        float baseDeg = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float[] angles = new float[numberOfFireballs];

        if (numberOfFireballs == 1)
        {
            angles[0] = baseDeg + Random.Range(-randomAngleJitter * 0.5f, randomAngleJitter * 0.5f);
            return angles;
        }

        float halfSpread = spreadAngle * 0.5f;
        float step       = spreadAngle / (numberOfFireballs - 1);

        for (int i = 0; i < numberOfFireballs; i++)
        {
            float offset = -halfSpread + step * i;
            float jitter = Random.Range(-randomAngleJitter * 0.5f, randomAngleJitter * 0.5f);
            angles[i]    = baseDeg + offset + jitter;
        }

        return angles;
    }

    private void SpawnOneBall(Vector2 spawnPos, float angleDeg)
    {
        float rad   = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        Projectile proj = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        proj.SetTeam(health != null ? health.Team : Team.Enemy);
        proj.Initialize(dir, 1f, null, projectileSpeed, projectileLifeTime, damage);
    }

    private Vector2 GetSpawnPos()
        => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

    private Vector2 GetDirectionToPlayer(Vector2 from)
    {
        if (movement == null || movement.Target == null) return Vector2.right;
        Vector2 toPlayer = (Vector2)movement.Target.position - from;
        return toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;
    }

    // ─── Gizmos ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (movement == null || movement.Target == null) return;

        Vector2 spawnPos = GetSpawnPos();
        Vector2 baseDir  = GetDirectionToPlayer(spawnPos);
        float baseDeg    = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float previewLen = 2.5f;

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.85f);

        if (numberOfFireballs == 1)
        {
            DrawGizmoRay(spawnPos, baseDeg, previewLen);
            return;
        }

        float halfSpread = spreadAngle * 0.5f;
        float step       = spreadAngle / (numberOfFireballs - 1);
        for (int i = 0; i < numberOfFireballs; i++)
        {
            float deg = baseDeg - halfSpread + step * i;
            DrawGizmoRay(spawnPos, deg, previewLen);
        }
    }

    private static void DrawGizmoRay(Vector2 origin, float angleDeg, float length)
    {
        float rad   = angleDeg * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        Gizmos.DrawRay(origin, dir * length);
    }
}
