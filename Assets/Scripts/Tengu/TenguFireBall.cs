using System.Collections;
using UnityEngine;

/// <summary>
/// Skill của Tengu: gọi nhiều quả cầu lửa rơi xuống vị trí (dự đoán) của player.
/// Mỗi quả hiện vùng đỏ cảnh báo (FireBallZone) trước khi nổ.
/// Gắn script này lên Tengu. Có thể tự cast theo cooldown (autoCast)
/// hoặc gọi StartCall() / TriggerAttack() từ EnemyAI / Animation Event.
/// </summary>
public class TenguFireBall : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public Rigidbody2D playerRb;
    [Tooltip("Prefab vùng đỏ (có thể để trống: sẽ tự tạo hình tròn đỏ)")]
    public GameObject fireBallZonePrefab;
    [Tooltip("Prefab cầu lửa hiển thị khi rơi (vd. Prefabs/Projectile/Fireball). Chỉ lấy sprite + animation, không gây damage")]
    public GameObject fallingFireballPrefab;
    [Tooltip("Cầu lửa bắt đầu rơi từ độ cao này (unit) phía trên vùng đỏ")]
    public float fallHeight = 6f;
    [Tooltip("Cầu lửa bắt đầu lệch sang TRÁI vùng đỏ bấy nhiêu unit (góc trên-trái)")]
    public float fallOffsetX = 6f;
    [Tooltip("Chỉnh góc nếu sprite không quay mặt sang phải (vd. 90, -90, 180)")]
    public float spriteAngleOffset = 0f;

    [Header("Cooldown")]
    public float skillCoolDown = 5f;
    private float skillCounter;
    [Tooltip("Tự cast khi hết cooldown và player trong tầm")]
    public bool autoCast = true;
    public float castRange = 8f;

    [Header("Skill Balance")]
    [Tooltip("Dự đoán vị trí player sau bao nhiêu giây (0 = đúng chỗ player đang đứng)")]
    public float predictionTime = 1.0f;
    public int fireCount = 3;
    public float timeBetweenFires = 0.4f;
    [Tooltip("Bán kính lệch ngẫu nhiên cho mỗi quả")]
    public float randomSpread = 0.5f;

    [Header("Zone")]
    public float zoneRadius = 1.2f;
    [Tooltip("Thời gian vùng đỏ hiện trước khi cầu lửa rơi trúng (giây)")]
    public float warningTime = 1.0f;
    public int damage = 1;
    [Tooltip("Layer bị trúng. Để trống = layer Player")]
    public LayerMask targetLayers;

    private bool isCalling;
    private bool strikeStarted;
    private Health ownerHealth;

    private void Awake()
    {
        // Bắt đầu với cooldown đầy để không cast ngay khi vừa spawn
        skillCounter = skillCoolDown;
        ownerHealth = GetComponent<Health>();
        if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
    }

    private void Start()
    {
        FindPlayerIfNeeded();
    }

    private void Update()
    {
        if (ownerHealth != null && ownerHealth.IsDead) return;

        if (TryGetComponent<EnemySkillStatus>(out var status) && status.Immobilized && isCalling && !strikeStarted)
        {
            isCalling = false;
            skillCounter = skillCoolDown;
        }

        if (!isCalling && skillCounter > 0f)
            skillCounter -= Time.deltaTime;

        if (!autoCast || isCalling || skillCounter > 0f) return;

        FindPlayerIfNeeded();
        if (playerTransform == null) return;

        if (Vector2.Distance(transform.position, playerTransform.position) <= castRange)
        {
            StartCall();
            TriggerAttack();
        }
    }

    private void OnDisable()
    {
        // Tengu chết / bị tắt: dừng đợt bắn đang chạy dở
        StopAllCoroutines();
        isCalling = false;
        strikeStarted = false;
    }

    /// <summary>Bắt đầu skill (đặt cooldown). Gọi ở đầu animation cast nếu muốn.</summary>
    public void StartCall()
    {
        if(!isActiveAndEnabled||(ownerHealth!=null&&ownerHealth.IsDead)||isCalling||skillCounter>0)return;
        if(TryGetComponent<EnemySkillStatus>(out var status)&&status.Immobilized)return;
        isCalling = true;
        strikeStarted = false;
        skillCounter = skillCoolDown;
    }

    /// <summary>Thả cầu lửa. Gọi từ Animation Event hoặc ngay sau StartCall().</summary>
    public void TriggerAttack()
    {
        if(!isActiveAndEnabled||(ownerHealth!=null&&ownerHealth.IsDead)||!isCalling||strikeStarted)return;
        if(TryGetComponent<EnemySkillStatus>(out var status)&&status.Immobilized)return;

        strikeStarted = true;
        StartCoroutine(FireBallRoutine());
    }

    private IEnumerator FireBallRoutine()
    {
        for (int i = 0; i < fireCount; i++)
        {
            SpawnPredictedZone();
            yield return new WaitForSeconds(timeBetweenFires);
        }

        isCalling = false;
        strikeStarted = false;
        skillCounter = skillCoolDown; // cooldown tính từ lúc kết thúc đợt bắn
    }

    private void SpawnPredictedZone()
    {
        FindPlayerIfNeeded();
        if (playerTransform == null) return;

        Vector2 predicted = playerTransform.position;
        if (playerRb != null) predicted += playerRb.linearVelocity * predictionTime;
        predicted += Random.insideUnitCircle * randomSpread;

        GameObject zoneObj = fireBallZonePrefab != null
            ? Instantiate(fireBallZonePrefab, predicted, Quaternion.identity)
            : new GameObject("FireBallZone");
        zoneObj.transform.position = predicted;

        var zone = zoneObj.GetComponent<FireBallZone>();
        if (zone == null) zone = zoneObj.AddComponent<FireBallZone>();
        if (fallingFireballPrefab != null) zone.fallingFireballPrefab = fallingFireballPrefab;
        zone.fallHeight = fallHeight;
        zone.fallOffsetX = fallOffsetX;
        zone.spriteAngleOffset = spriteAngleOffset;
        zone.Setup(zoneRadius, warningTime, damage, ownerHealth, targetLayers);
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTransform != null) return;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;

        playerTransform = p.transform;
        if (playerRb == null) playerRb = p.GetComponent<Rigidbody2D>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, castRange);
    }
}

