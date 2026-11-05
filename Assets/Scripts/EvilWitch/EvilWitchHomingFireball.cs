using System.Collections;
using UnityEngine;

/// <summary>
/// Chiêu "Homing Fireball" của EvilWitch:
/// Phóng cầu lửa từ Witch, bám đuổi Player cho tới khi chạm layer Wall.
///
/// Cách kích hoạt:
///   - Gọi CastHomingFireball() từ EvilWitchSkillSequencer (qua Animation Event).
/// </summary>
public class EvilWitchHomingFireball : MonoBehaviour
{
    [Header("Cầu lửa")]
    [Tooltip("Prefab có component HomingFireball")]
    [SerializeField] private HomingFireball fireballPrefab;

    [Tooltip("Điểm phóng. Để trống = tâm Witch")]
    [SerializeField] private Transform firePoint;

    [Header("Số lượng")]
    [SerializeField, Range(1, 5)] private int numberOfFireballs = 1;
    [Tooltip("Delay giữa các quả (giây) khi bắn nhiều quả")]
    [SerializeField, Min(0f)] private float delayBetweenShots = 0.4f;

    [Header("Thông số cầu lửa")]
    [SerializeField] private float projectileSpeed = 4f;
    [Tooltip("Độ/giây. Thấp = dễ né, cao = bám sát")]
    [SerializeField] private float turnSpeed = 150f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float maxLifeTime = 12f;

    [Header("Tự động dùng skill")]
    [Tooltip("Bật: Witch tự phóng cầu lửa bám đuổi theo cooldown (không cần Animation Event / Sequencer).")]
    [SerializeField] private bool autoCast = true;

    [Tooltip("Thời gian hồi chiêu (giây) giữa 2 lần phóng. Tính từ lúc lần phóng trước kết thúc.")]
    [SerializeField, Min(0.1f)] private float skillCooldown = 6f;

    [Tooltip("Delay trước lần phóng đầu tiên khi Witch xuất hiện (giây).")]
    [SerializeField, Min(0f)] private float initialDelay = 1.5f;

    private float cooldownTimer;
    private bool isCasting;

    /// <summary>True nếu skill tự chạy theo cooldown (Sequencer sẽ bỏ qua phase này).</summary>
    public bool AutoCast => autoCast;

    /// <summary>Thời gian còn lại tới lần phóng kế tiếp (debug/UI).</summary>
    public float CooldownRemaining => Mathf.Max(0f, cooldownTimer);

    private EvelWitchMoverment movement;
    private Health health;

    private void Awake()
    {
        movement = GetComponent<EvelWitchMoverment>();
        health   = GetComponent<Health>();
        cooldownTimer = initialDelay;
    }

    private void OnValidate()
    {
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        turnSpeed       = Mathf.Max(0f, turnSpeed);
        damage          = Mathf.Max(0, damage);
        maxLifeTime     = Mathf.Max(1f, maxLifeTime);
        skillCooldown   = Mathf.Max(0.1f, skillCooldown);
    }

    private void Update()
    {
        if (!autoCast || isCasting) return;
        if (health != null && health.IsDead) return;
        if (movement == null || movement.Target == null) return;

        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
            CastHomingFireball();
    }


    /// <summary>Gọi từ EvilWitchSkillSequencer hoặc Animation Event.</summary>
    public void CastHomingFireball()
    {
        if (!isActiveAndEnabled || isCasting || (health != null && health.IsDead)) return;
        if (TryGetComponent<EnemySkillStatus>(out var status) && status.Immobilized) return;
        if (fireballPrefab == null)
        {
            Debug.LogWarning($"[EvilWitchHomingFireball] '{name}': chưa gán fireballPrefab!", this);
            return;
        }

        if (movement == null || movement.Target == null) return;

        StartCoroutine(CastRoutine());
    }

    private IEnumerator CastRoutine()
    {
        isCasting = true;

        for (int i = 0; i < numberOfFireballs; i++)
        {
            if (movement == null || movement.Target == null) break;
            if (health != null && health.IsDead) break; // Witch chết giữa chừng: dừng bắn

            SpawnOne();

            if (i < numberOfFireballs - 1 && delayBetweenShots > 0f)
                yield return new WaitForSeconds(delayBetweenShots);
        }

        isCasting = false;
        cooldownTimer = skillCooldown; // bắt đầu hồi chiêu sau khi phóng xong
    }

    private void SpawnOne()
    {
        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 dir      = ((Vector2)movement.Target.position - spawnPos);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        HomingFireball ball = Instantiate(fireballPrefab, spawnPos, Quaternion.identity);
        ball.Initialize(movement.Target, dir, health != null ? health.Team : Team.Enemy,
                        projectileSpeed, turnSpeed, damage, maxLifeTime);
    }
}
