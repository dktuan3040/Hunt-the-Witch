using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "Hunt the Witch/Skills/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Skill Identity")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName = "Fireball";
    [TextArea(2, 4)]
    [SerializeField] private string description = "Channels raw flame magic. Left-click to spam, right-click to unleash a blazing storm.";
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Color themeColor = new Color(1f, 0.4f, 0f, 1f);

    [Header("Synergy Classification")]
    [Tooltip("Hệ nguyên tố của skill.")]
    [SerializeField] private string elementTag = "Fire";
    [Tooltip("Đánh dấu nếu đây là skill đã được hợp nhất từ Synergy.")]
    [SerializeField] private bool isSynergySkill = false;

    [Header("Ultimate Attack (Chuột phải - Có hồi chiêu)")]
    [Tooltip("Dữ liệu đạn/vụ nổ/hiệu ứng riêng của chiêu Ultimate.")]
    [SerializeField] private ProjectileData ultimateProjectile;
    [Tooltip("Thời gian hồi chiêu Ultimate (giây).")]
    [SerializeField] private float ultimateCooldown = 5.0f;
    [Tooltip("Số lượng đạn bắn ra của Ultimate.")]
    [SerializeField] private int ultimateProjectilesPerShot = 5;
    [Tooltip("Góc xòe của Ultimate (đơn vị: độ).")]
    [SerializeField] private float ultimateSpreadAngle = 35f;
    [Tooltip("Hiệu ứng ánh sáng đầu nòng khi tung Ultimate.")]
    [SerializeField] private GameObject ultimateMuzzleFlash;
    [Tooltip("Âm thanh thi triển Ultimate.")]
    [SerializeField] private AudioClip ultimateSound;

    // Public Getters
    public string SkillId => string.IsNullOrEmpty(skillId) ? name : skillId;
    public string SkillName => skillName;
    public string Description => description;
    public Sprite SkillIcon => skillIcon;
    public Color ThemeColor => themeColor;
    public string ElementTag => elementTag;
    public bool IsSynergySkill => isSynergySkill;

    public ProjectileData UltimateProjectile => ultimateProjectile;
    public float UltimateCooldown => ultimateCooldown;
    public int UltimateProjectilesPerShot => Mathf.Max(1, ultimateProjectilesPerShot);
    public float UltimateSpreadAngle => ultimateSpreadAngle;
    public GameObject UltimateMuzzleFlash => ultimateMuzzleFlash;
    public AudioClip UltimateSound => ultimateSound;
}