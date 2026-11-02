using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý hiển thị cho 1 ô Skill (Q hoặc E):
/// - Hiển thị icon skill (nằm dưới viền SkillBorder)
/// - Bật / tắt object Selected khi ô được chọn
/// - Làm mờ nhạt (dim) màu sắc khi ô không được chọn
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Ảnh hiển thị icon kỹ năng (nằm dưới viền SkillBorder)")]
    [SerializeField] private Image skillIcon;

    [Tooltip("Ảnh khung viền kỹ năng")]
    [SerializeField] private Image skillBorder;

    [Tooltip("Object Selected (chỉ hiện khi nhấn chọn skill)")]
    [SerializeField] private GameObject selectedObject;

    [Tooltip("Text phím bấm (Q hoặc E)")]
    [SerializeField] private TMP_Text keyText;

    [Header("Alpha Settings (Độ nhạt khi không chọn)")]
    [Tooltip("CanvasGroup để làm mờ toàn bộ ô khi không chọn")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float selectedAlpha = 1.0f;
    [SerializeField] private float unselectedAlpha = 0.55f;

    private bool isCoolingDown;
    private Color readyIconColor = Color.white;
    private Material readyIconMaterial;
    private Material cooldownMaterial;

    public Image SkillIcon => skillIcon;
    public Image SkillBorder => skillBorder;
    public GameObject SelectedObject => selectedObject;
    public TMP_Text KeyText => keyText;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        AutoFindReferences();
    }

    public void AutoFindReferences()
    {
        if (skillIcon == null)
        {
            var iconTr = transform.Find("SkillIcon") ?? transform.Find("SkillBorder/SkillIcon");
            if (iconTr != null) skillIcon = iconTr.GetComponent<Image>();
        }

        if (skillBorder == null)
        {
            var borderTr = transform.Find("SkillBorder");
            if (borderTr != null) skillBorder = borderTr.GetComponent<Image>();
        }

        if (selectedObject == null)
        {
            var selTr = transform.Find("Selected");
            if (selTr != null) selectedObject = selTr.gameObject;
        }

        if (keyText == null)
        {
            var titleTr = transform.Find("Title");
            if (titleTr != null) keyText = titleTr.GetComponent<TMP_Text>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// Bật/tắt trạng thái được chọn:
    /// - Khi được chọn: hiện Selected, alpha = 1.0 (sáng rõ)
    /// - Khi không được chọn: ẩn Selected, alpha = 0.55 (nhạt hơn)
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectedObject != null)
        {
            selectedObject.SetActive(isSelected);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isSelected ? selectedAlpha : unselectedAlpha;
        }
    }

    /// <summary>
    /// Cập nhật hình ảnh icon của Skill
    /// </summary>
    public void UpdateSkill(SkillData skill)
    {
        if (skillIcon == null) return;

        if (skill != null && skill.SkillIcon != null)
        {
            skillIcon.gameObject.SetActive(true);
            skillIcon.sprite = skill.SkillIcon;
            readyIconColor = Color.white;
            skillIcon.preserveAspect = true;
        }
        else if (skill != null)
        {
            skillIcon.gameObject.SetActive(true);
            skillIcon.sprite = null;
            readyIconColor = skill.ThemeColor;
        }
        else
        {
            skillIcon.gameObject.SetActive(false);
        }

        ApplyCooldownAppearance();
    }

    public void SetCooldown(float remaining)
    {
        bool coolingDown = remaining > 0f;
        if (isCoolingDown == coolingDown) return;
        isCoolingDown = coolingDown;
        ApplyCooldownAppearance();
    }

    private void ApplyCooldownAppearance()
    {
        if (skillIcon == null) return;

        if (isCoolingDown)
        {
            if (cooldownMaterial == null)
            {
                var shader = Resources.Load<Shader>("SkillCooldownGrayscale");
                if (shader != null)
                {
                    cooldownMaterial = new Material(shader);
                    cooldownMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
            }

            if (skillIcon.material != cooldownMaterial)
                readyIconMaterial = skillIcon.material;
            if (cooldownMaterial != null) skillIcon.material = cooldownMaterial;
            skillIcon.color = new Color(0.7f, 0.7f, 0.7f, readyIconColor.a);
        }
        else
        {
            if (cooldownMaterial != null && skillIcon.material == cooldownMaterial)
                skillIcon.material = readyIconMaterial;
            skillIcon.color = readyIconColor;
        }
    }

    private void OnDestroy()
    {
        if (cooldownMaterial == null) return;
        if (skillIcon != null && skillIcon.material == cooldownMaterial)
            skillIcon.material = readyIconMaterial;
        Destroy(cooldownMaterial);
    }
}
