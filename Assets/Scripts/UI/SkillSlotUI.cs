using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image cooldownOverlay; // Image dạng Filled (Radial 360 hoặc Vertical)
    [SerializeField] private Text cooldownText;
    [SerializeField] private Image activeBorder;
    [SerializeField] private Text keyLabelText;
    [SerializeField] private Text skillNameText;

    // Public Properties để cấu hình tự động khi sinh Canvas
    public Image SlotBackground { get => slotBackground; set => slotBackground = value; }
    public Image SkillIcon { get => skillIcon; set => skillIcon = value; }
    public Image CooldownOverlay { get => cooldownOverlay; set => cooldownOverlay = value; }
    public Text CooldownText { get => cooldownText; set => cooldownText = value; }
    public Image ActiveBorder { get => activeBorder; set => activeBorder = value; }
    public Text KeyLabelText { get => keyLabelText; set => keyLabelText = value; }
    public Text SkillNameText { get => skillNameText; set => skillNameText = value; }

    public void SetKeyLabel(string key)
    {
        if (keyLabelText != null)
        {
            keyLabelText.text = key;
        }
    }

    public void UpdateSkill(SkillData skill)
    {
        if (skill != null)
        {
            if (skillIcon != null)
            {
                skillIcon.gameObject.SetActive(true);
                skillIcon.sprite = skill.SkillIcon;
                // Nếu chưa có sprite icon thì lấy màu nguyên tố để dễ nhận biết
                skillIcon.color = skill.SkillIcon != null ? Color.white : skill.ThemeColor;
            }

            if (skillNameText != null)
            {
                skillNameText.text = skill.SkillName;
                skillNameText.color = skill.IsSynergySkill ? new Color(1f, 0.4f, 1f) : skill.ThemeColor;
            }

            if (slotBackground != null)
            {
                slotBackground.color = new Color(0.12f, 0.12f, 0.18f, 0.9f);
            }
        }
        else
        {
            if (skillIcon != null)
            {
                skillIcon.gameObject.SetActive(false);
            }

            if (skillNameText != null)
            {
                skillNameText.text = "(Trống)";
                skillNameText.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            }

            if (slotBackground != null)
            {
                slotBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.6f);
            }

            UpdateCooldown(0f, 1f);
        }
    }

    public void SetActive(bool isActive)
    {
        if (activeBorder != null)
        {
            activeBorder.gameObject.SetActive(isActive);
        }
    }

    public void UpdateCooldown(float current, float max)
    {
        if (current > 0f && max > 0f)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                cooldownOverlay.fillAmount = current / max;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = current >= 1.0f ? $"{current:F0}s" : $"{current:F1}s";
            }
        }
        else
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}
