using UnityEngine;

/// <summary>
/// Quản lý UI hiển thị vùng chọn Skill (Q/E):
/// - Đồng bộ với PlayerSkillManager (ActiveSlotIndex, Slots)
/// - Cập nhật ô chọn khi người chơi nhấn Q/E
/// </summary>
public class PlayerSkillHUD : MonoBehaviour
{
    [Header("Skill Manager Reference")]
    [Tooltip("Để trống sẽ tự động tìm PlayerSkillManager trên Player hoặc trong Scene")]
    [SerializeField] private PlayerSkillManager skillManager;

    [Header("Skill Slots")]
    [SerializeField] private SkillSlotUI slotQ;
    [SerializeField] private SkillSlotUI slotE;

    [Header("Synergy Connection")]
    [SerializeField] private GameObject synergyConnection;

    private bool subscribed;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        FindReferences();
        Subscribe();
        InitializeHUD();
    }

    private void OnEnable()
    {
        FindReferences();
        Subscribe();
        InitializeHUD();
    }

    private void OnDisable()
    {
        if (subscribed && skillManager != null)
        {
            skillManager.OnActiveSlotChanged -= HandleActiveSlotChanged;
            skillManager.OnSlotSkillChanged -= HandleSlotSkillChanged;
            skillManager.OnCooldownUpdated -= HandleCooldownUpdated;
        }
        subscribed = false;
    }

    private void Subscribe()
    {
        if (subscribed || skillManager == null) return;
        skillManager.OnActiveSlotChanged += HandleActiveSlotChanged;
        skillManager.OnSlotSkillChanged += HandleSlotSkillChanged;
        skillManager.OnCooldownUpdated += HandleCooldownUpdated;
        subscribed = true;
    }

    public void FindReferences()
    {
        if (synergyConnection == null)
        {
            var connection = transform.Find("Connect") ?? transform.Find("SkillSelectionArea/Connect");
            if (connection != null) synergyConnection = connection.gameObject;
        }
        if (skillManager == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                skillManager = player.GetComponent<PlayerSkillManager>() ?? player.GetComponentInChildren<PlayerSkillManager>();
            }
            if (skillManager == null)
            {
                skillManager = FindAnyObjectByType<PlayerSkillManager>();
            }
        }

        if (slotQ == null)
        {
            var qTr = transform.Find("SkillQ") ?? transform.Find("SkillSelectionArea/SkillQ");
            if (qTr != null) slotQ = qTr.GetComponent<SkillSlotUI>() ?? qTr.gameObject.AddComponent<SkillSlotUI>();
        }

        if (slotE == null)
        {
            var eTr = transform.Find("SkillE") ?? transform.Find("SkillSelectionArea/SkillE");
            if (eTr != null) slotE = eTr.GetComponent<SkillSlotUI>() ?? eTr.gameObject.AddComponent<SkillSlotUI>();
        }
    }

    public void InitializeHUD()
    {
        UpdateSynergyConnection();
        if (skillManager == null) return;

        if (slotQ != null && skillManager.Slots != null && skillManager.Slots.Length > 0)
        {
            slotQ.UpdateSkill(skillManager.Slots[0]);
            slotQ.SetCooldown(skillManager.GetCooldownRemaining(0));
        }

        if (slotE != null && skillManager.Slots != null && skillManager.Slots.Length > 1)
        {
            slotE.UpdateSkill(skillManager.Slots[1]);
            slotE.SetCooldown(skillManager.GetCooldownRemaining(1));
        }

        HandleActiveSlotChanged(skillManager.ActiveSlotIndex);
    }

    private void HandleActiveSlotChanged(int activeIndex)
    {
        if (slotQ != null) slotQ.SetSelected(activeIndex == 0);
        if (slotE != null) slotE.SetSelected(activeIndex == 1);
    }

    private void HandleSlotSkillChanged(int slotIndex, SkillData skill)
    {
        UpdateSynergyConnection();
        if (slotIndex == 0 && slotQ != null)
        {
            slotQ.UpdateSkill(skill);
            slotQ.SetCooldown(skillManager.GetCooldownRemaining(0));
        }
        if (slotIndex == 1 && slotE != null)
        {
            slotE.UpdateSkill(skill);
            slotE.SetCooldown(skillManager.GetCooldownRemaining(1));
        }
    }
    private void HandleCooldownUpdated(int slotIndex, float remaining, float maximum)
    {
        if (slotIndex == 0 && slotQ != null) slotQ.SetCooldown(remaining);
        if (slotIndex == 1 && slotE != null) slotE.SetCooldown(remaining);
    }
    private void UpdateSynergyConnection()
    {
        if (synergyConnection != null && synergyConnection.GetComponent<SkillSynergyConnector>() == null)
            synergyConnection.AddComponent<SkillSynergyConnector>();
        if (synergyConnection != null)
            synergyConnection.SetActive(skillManager != null && skillManager.HasLinkedSynergy);
    }
}