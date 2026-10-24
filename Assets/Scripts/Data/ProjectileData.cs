using UnityEngine;

[CreateAssetMenu(fileName = "NewProjectileData", menuName = "Hunt the Witch/Combat/Projectile Data")]
public class ProjectileData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string projectileName = "Standard Projectile";

    [Header("Visual & Animation")]
    [Tooltip("Sprite của viên đạn (nếu không dùng Animator hoặc làm sprite mặc định).")]
    [SerializeField] private Sprite projectileSprite;
    [Tooltip("Animator Controller cho viên đạn (ví dụ hiệu ứng lửa cháy, xoáy băng... riêng của từng skill).")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [Tooltip("Màu sắc ánh xạ lên đạn (Color Tint).")]
    [SerializeField] private Color spriteColor = Color.white;
    [Tooltip("Scale của viên đạn (1,1,1 là mặc định).")]
    [SerializeField] private Vector3 projectileScale = Vector3.one;

    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [Tooltip("Góc xoay bù trừ cho sprite đạn nếu sprite vẽ hướng lên (thường là -90 độ).")]
    [SerializeField] private float spriteOffset = -90f;

    [Header("Combat")]
    [SerializeField] private int damage = 1;
    [Tooltip("Số lượng mục tiêu đạn có thể xuyên qua trước khi biến mất. 1 = biến mất ngay khi chạm mục tiêu đầu tiên.")]
    [SerializeField] private int pierceCount = 1;

    [Header("Effects")]
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private AudioClip impactSound;

    // Public Getters
    public string ProjectileName => projectileName;
    public Sprite ProjectileSprite => projectileSprite;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public Color SpriteColor => spriteColor;
    public Vector3 ProjectileScale => projectileScale;
    public float Speed => speed;
    public float LifeTime => lifeTime;
    public float SpriteOffset => spriteOffset;
    public int Damage => damage;
    public int PierceCount => pierceCount;
    public GameObject ImpactVfxPrefab => impactVfxPrefab;
    public AudioClip ImpactSound => impactSound;
}

