using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShoot : MonoBehaviour
{
    [Header("Skill Manager Reference")]
    [Tooltip("Tự động tìm kiếm PlayerSkillManager trên Player cha.")]
    [SerializeField] private PlayerSkillManager skillManager;

    [Header("Weapon Configuration (ScriptableObject)")]
    [Tooltip("Gán WeaponData để tự động cấu hình cây gậy (sprite, tầm bay, tốc độ đạn, sát thương, tốc độ bắn...).")]
    [SerializeField] private WeaponData currentWeapon;

    [Header("Primary Attack Visual (Dùng chung cho đòn bắn thường)")]
    [Tooltip("Sprite đạn dùng chung cho tất cả các đòn bắn thường (chuột trái). Nếu để trống sẽ dùng sprite của prefab/gậy.")]
    [SerializeField] private Sprite commonPrimaryProjectileSprite;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;

    [Header("Fallback Settings (Dùng khi chưa gán Weapon)")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float fireRate = 0.25f;
    [SerializeField] private float fallbackProjectileSpeed = 12f;
    [SerializeField] private float fallbackRange = 10f;
    [SerializeField] private int fallbackDamage = 10;

    [Header("Input References")]
    [SerializeField] private InputActionReference shootAction;

    private float nextFireTime;

    public WeaponData CurrentWeapon => currentWeapon;
    public Sprite CommonPrimaryProjectileSprite => commonPrimaryProjectileSprite;

    private void Awake()
    {
        // Tự động tìm hoặc gắn PlayerSkillManager trên Player
        if (skillManager == null)
        {
            skillManager = GetComponentInParent<PlayerSkillManager>();
            if (skillManager == null && transform.root != null)
            {
                skillManager = transform.root.GetComponent<PlayerSkillManager>();
                if (skillManager == null)
                {
                    skillManager = transform.root.gameObject.AddComponent<PlayerSkillManager>();
                }
            }
        }

        // Tự động gắn HUD hiển thị 2 slot Q/E nếu chưa có
        if (transform.root != null && transform.root.GetComponent<PlayerSkillHUD>() == null)
        {
            transform.root.gameObject.AddComponent<PlayerSkillHUD>();
        }

        // Tự động tìm SpriteRenderer của Weapon nếu chưa gán
        if (weaponSpriteRenderer == null)
        {
            weaponSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (currentWeapon != null)
        {
            EquipWeapon(currentWeapon);
        }
    }

    private void Update()
    {
        HandleDirectMouseInput();
    }

    private void HandleDirectMouseInput()
    {
        if (PauseMenuUI.CombatInputBlocked || Mouse.current == null) return;

        // Chuột trái: Bắn spam liên tục (nhấn giữ hoặc nhấp) theo thông số của Gậy
        if (Mouse.current.leftButton.isPressed)
        {
            ShootPrimary();
        }

    }

    private void OnEnable()
    {
        if (skillManager != null) skillManager.OnSkillUseRequested += ShootUltimate;

        if (shootAction != null)
        {
            shootAction.action.performed += OnShootInput;
            shootAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (skillManager != null) skillManager.OnSkillUseRequested -= ShootUltimate;

        if (shootAction != null)
        {
            shootAction.action.performed -= OnShootInput;
            shootAction.action.Disable();
        }
    }

    private void OnShootInput(InputAction.CallbackContext context)
    {
        ShootPrimary();
    }

    /// <summary>
    /// Thay đổi gậy đang cầm tại runtime
    /// </summary>
    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        if (newWeapon == null) return;

        // Cập nhật hình ảnh cây gậy trên tay
        if (weaponSpriteRenderer != null && newWeapon.WeaponSprite != null)
        {
            weaponSpriteRenderer.sprite = newWeapon.WeaponSprite;
        }
    }

    /// <summary>
    /// Bắn đòn thường (Chuột trái) - Hoàn toàn điều khiển bởi Gậy (WeaponData), dùng chung sprite đạn cơ bản
    /// </summary>
    public void ShootPrimary()
    {
        if (!isActiveAndEnabled || PauseMenuUI.CombatInputBlocked) return;
        if (Time.time < nextFireTime) return;

        Projectile effectivePrefab = (currentWeapon != null && currentWeapon.ProjectilePrefab != null)
            ? currentWeapon.ProjectilePrefab 
            : projectilePrefab;

        if (firePoint == null || effectivePrefab == null) return;

        // Các thông số đòn thường do Gậy quyết định
        float effectiveFireRate = currentWeapon != null ? currentWeapon.FireRate : fireRate;
        int count = currentWeapon != null ? currentWeapon.ProjectilesPerShot : 1;
        float spread = currentWeapon != null ? currentWeapon.SpreadAngle : 0f;
        float speed = currentWeapon != null ? currentWeapon.ProjectileSpeed : fallbackProjectileSpeed;
        float lifeTime = currentWeapon != null ? currentWeapon.ProjectileLifeTime 
            : (fallbackProjectileSpeed > 0f ? fallbackRange / fallbackProjectileSpeed : 2f);
        int damage = currentWeapon != null ? currentWeapon.Damage : fallbackDamage;
        float dmgMultiplier = currentWeapon != null ? currentWeapon.DamageMultiplier : 1f;

        // Sprite đạn thường: ưu tiên sprite gậy nếu có, hoặc sprite chung, hoặc giữ nguyên sprite của prefab
        Sprite bulletSprite = (currentWeapon != null && currentWeapon.PrimaryBulletSprite != null)
            ? currentWeapon.PrimaryBulletSprite
            : commonPrimaryProjectileSprite;

        nextFireTime = Time.time + effectiveFireRate;

        // Bắn đạn thường (không truyền skillData, đạn không bị đổi sang visual của skill)
        SpawnProjectiles(
            effectivePrefab, 
            count, 
            spread, 
            dmgMultiplier, 
            skillData: null, 
            customSpeed: speed, 
            customLifeTime: lifeTime, 
            customDamage: damage, 
            customSprite: bulletSprite
        );

        // Hiệu ứng bắn của gậy
        PlayShootEffects();
    }

    /// <summary>
    /// Bắn chiêu Ultimate (Q/E) - Uy lực mạnh, có cooldown
    /// </summary>
    public void ShootUltimate()
    {
        if (!isActiveAndEnabled || PauseMenuUI.CombatInputBlocked || skillManager == null) return;

        SkillData activeSkill = skillManager.GetActiveSkill();
        if (activeSkill == null) return;

        // Kiểm tra cooldown
        if (!skillManager.CanUseUltimate())
        {
            // Cooldown chưa xong, không cho bắn
            return;
        }

        Projectile effectivePrefab = (currentWeapon != null && currentWeapon.ProjectilePrefab != null)
            ? currentWeapon.ProjectilePrefab 
            : projectilePrefab;

        if (firePoint == null || effectivePrefab == null) return;

        // Bắt đầu tính cooldown cho slot này
        skillManager.TriggerUltimateCooldown();

        float dmgMultiplier = currentWeapon != null ? currentWeapon.DamageMultiplier : 1f;

        // Bắn đạn Ultimate (lấy ProjectileData và hiệu ứng riêng của Ultimate)
        SpawnProjectiles(
            effectivePrefab,
            activeSkill.UltimateProjectilesPerShot,
            activeSkill.UltimateSpreadAngle,
            dmgMultiplier,
            activeSkill.UltimateProjectile
        );

        // Hiệu ứng Ultimate
        if (activeSkill.UltimateMuzzleFlash != null)
        {
            Instantiate(activeSkill.UltimateMuzzleFlash, firePoint.position, firePoint.rotation);
        }

        if (activeSkill.UltimateSound != null)
        {
            AudioSource.PlayClipAtPoint(activeSkill.UltimateSound, firePoint.position);
        }
        else
        {
            PlayShootEffects();
        }

        Debug.Log($"[Ultimate Unleashed!] Thi triển: {activeSkill.SkillName} (Cooldown: {activeSkill.UltimateCooldown}s)");
    }

    private void SpawnProjectiles(
        Projectile prefab, 
        int count, 
        float spreadAngle, 
        float dmgMultiplier, 
        ProjectileData skillData = null,
        float customSpeed = -1f,
        float customLifeTime = -1f,
        int customDamage = -1,
        Sprite customSprite = null)
    {
        Vector2 baseDirection = firePoint.right;

        if (count <= 1)
        {
            Projectile projectile = Instantiate(prefab, firePoint.position, firePoint.rotation);
            projectile.Initialize(baseDirection, dmgMultiplier, skillData, customSpeed, customLifeTime, customDamage, customSprite);
            return;
        }

        // Phân bổ góc xòe đều giữa các viên đạn
        float startAngle = -spreadAngle / 2f;
        float angleStep = spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion rotation = Quaternion.Euler(0, 0, currentAngle);
            Vector2 shootDir = rotation * baseDirection;

            Projectile projectile = Instantiate(prefab, firePoint.position, Quaternion.identity);
            projectile.Initialize(shootDir, dmgMultiplier, skillData, customSpeed, customLifeTime, customDamage, customSprite);
        }
    }

    private void PlayShootEffects()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.MuzzleFlashPrefab != null)
        {
            Instantiate(currentWeapon.MuzzleFlashPrefab, firePoint.position, firePoint.rotation);
        }

        if (currentWeapon.ShootSound != null)
        {
            AudioSource.PlayClipAtPoint(currentWeapon.ShootSound, firePoint.position);
        }
    }
}
