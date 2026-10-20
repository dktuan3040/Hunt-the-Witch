using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Hunt the Witch/Combat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Visual & Identity")]
    [SerializeField] private string weaponName = "Emerald Staff";
    [TextArea(2, 4)]
    [SerializeField] private string description = "A mystical staff channeling raw magical energy.";
    [Tooltip("Sprite của vũ khí hiển thị trên tay nhân vật.")]
    [SerializeField] private Sprite weaponSprite;
    [Tooltip("Icon hiển thị trên UI inventory/chọn vũ khí.")]
    [SerializeField] private Sprite weaponIcon;

    [Header("Primary Attack - Đòn bắn thường")]
    [Tooltip("Prefab đạn mà gậy này bắn ra.")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Sprite đạn dùng cho đòn bắn thường. Nếu để trống sẽ dùng sprite chung mặc định.")]
    [SerializeField] private Sprite primaryBulletSprite;
    [Tooltip("Sát thương cơ bản của viên đạn bắn ra từ gậy.")]
    [SerializeField] private int damage = 10;
    [Tooltip("Tốc độ bay của viên đạn (speed).")]
    [SerializeField] private float projectileSpeed = 12f;
    [Tooltip("Phạm vi/tầm bắn tối đa của đạn trước khi tự hủy (đơn vị: Unity units).")]
    [SerializeField] private float range = 10f;
    [Tooltip("Thời gian chờ giữa 2 lần bắn (càng nhỏ bắn càng nhanh).")]
    [SerializeField] private float fireRate = 0.25f;
    [Tooltip("Số viên đạn bắn ra trong một lần bấm.")]
    [SerializeField] private int projectilesPerShot = 1;
    [Tooltip("Góc xòe của các tia đạn khi bắn nhiều hơn 1 viên (đơn vị: độ).")]
    [SerializeField] private float spreadAngle = 15f;
    [Tooltip("Hệ số nhân sát thương của gậy đối với đạn.")]
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Effects & Audio")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private AudioClip shootSound;

    // Public Getters
    public string WeaponName => weaponName;
    public string Description => description;
    public Sprite WeaponSprite => weaponSprite;
    public Sprite WeaponIcon => weaponIcon;
    public Projectile ProjectilePrefab => projectilePrefab;
    public Sprite PrimaryBulletSprite => primaryBulletSprite;
    public int Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float Range => range;
    public float ProjectileLifeTime => projectileSpeed > 0f ? (range / projectileSpeed) : 2f;
    public float FireRate => fireRate;
    public int ProjectilesPerShot => Mathf.Max(1, projectilesPerShot);
    public float SpreadAngle => spreadAngle;
    public float DamageMultiplier => damageMultiplier;
    public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
    public AudioClip ShootSound => shootSound;
}
