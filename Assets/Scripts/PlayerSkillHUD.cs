using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillHUD : MonoBehaviour
{
    [Header("Skill Manager")]
    [SerializeField] private PlayerSkillManager skillManager;

    [Header("Canvas Slot UI Elements (Q: 0, E: 1)")]
    [SerializeField] private SkillSlotUI[] slotUIs = new SkillSlotUI[2];

    private void Awake()
    {
        if (skillManager == null)
        {
            skillManager = GetComponentInParent<PlayerSkillManager>() ?? FindAnyObjectByType<PlayerSkillManager>();
        }

        // HUD will be set up manually in the Unity Editor; auto‑creation removed.
    }

    private void OnEnable()
    {
        if (skillManager != null)
        {
            skillManager.OnSlotSkillChanged += HandleSlotSkillChanged;
            skillManager.OnActiveSlotChanged += HandleActiveSlotChanged;
            skillManager.OnCooldownUpdated += HandleCooldownUpdated;
        }
    }

    private void OnDisable()
    {
        if (skillManager != null)
        {
            skillManager.OnSlotSkillChanged -= HandleSlotSkillChanged;
            skillManager.OnActiveSlotChanged -= HandleActiveSlotChanged;
            skillManager.OnCooldownUpdated -= HandleCooldownUpdated;
        }
    }

    private void Start()
    {
        InitializeHUD();
    }

    private void InitializeHUD()
    {
        string[] labels = { "Q", "E" };

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].SetKeyLabel(labels[i]);

                if (skillManager != null && skillManager.Slots != null && i < skillManager.Slots.Length)
                {
                    slotUIs[i].UpdateSkill(skillManager.Slots[i]);
                }
                else
                {
                    slotUIs[i].UpdateSkill(null);
                }

                bool isActive = (skillManager != null && skillManager.ActiveSlotIndex == i);
                slotUIs[i].SetActive(isActive);
            }
        }
    }

    private void HandleSlotSkillChanged(int slotIndex, SkillData newSkill)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Length && slotUIs[slotIndex] != null)
        {
            slotUIs[slotIndex].UpdateSkill(newSkill);
        }
    }

    private void HandleActiveSlotChanged(int activeIndex)
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].SetActive(i == activeIndex);
            }
        }
    }

    private void HandleCooldownUpdated(int slotIndex, float current, float max)
    {
        if (slotIndex >= 0 && slotIndex < slotUIs.Length && slotUIs[slotIndex] != null)
        {
            slotUIs[slotIndex].UpdateCooldown(current, max);
        }
    }

    
}

