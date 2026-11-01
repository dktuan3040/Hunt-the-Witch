using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkillManager : MonoBehaviour
{
    [Header("Skill Slots (Q: 0, E: 1)")]
    [SerializeField] private SkillData[] slots = new SkillData[2];
    [SerializeField] private int activeSlotIndex = 0;

    [Header("Synergy Database")]
    [Tooltip("Danh sách toàn bộ các công thức Synergy có thể xảy ra trong game.")]
    [SerializeField] private List<SkillSynergyData> synergyDatabase = new List<SkillSynergyData>();

    [Header("Input Settings (Tự động liên kết PlayerInput như PlayerMovement)")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private InputActionReference skillSlot1Action;
    [SerializeField] private InputActionReference skillSlot2Action;

    // Các action được tự động phát hiện từ PlayerInput
    private InputAction boundSlot1Action;
    private InputAction boundSlot2Action;

    // Normal skills cool down independently; linked synergy slots share a cooldown.
    private float[] ultimateCooldownTimers = new float[2];

    // Events để kết nối Canvas UI
    public event Action<int, SkillData> OnSlotSkillChanged;

    // Notify the HUD when the selected skill slot changes.
    public event Action<int> OnActiveSlotChanged;
    public event Action OnSkillUseRequested;

    private readonly int[] lastActivationFrames = { -1, -1 };

    public event Action<int, float, float> OnCooldownUpdated; // slotIndex, current, max

    public int ActiveSlotIndex => activeSlotIndex;
    public SkillData[] Slots => slots;

    public bool HasLinkedSynergy
    {
        get
        {
            if (slots == null || slots.Length < 2 || slots[0] == null || slots[0] != slots[1])
                return false;
            if (slots[0].IsSynergySkill) return true;
            if (synergyDatabase != null)
            {
                foreach (var recipe in synergyDatabase)
                    if (recipe != null && recipe.ResultingSynergySkill == slots[0]) return true;
            }
            return false;
        }
    }

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }
    }

    private void OnEnable()
    {
        // 1. Nếu có gắn trực tiếp InputActionReference
        if (skillSlot1Action != null)
        {
            skillSlot1Action.action.performed += OnSlot1Performed;
            skillSlot1Action.action.Enable();
        }
        if (skillSlot2Action != null)
        {
            skillSlot2Action.action.performed += OnSlot2Performed;
            skillSlot2Action.action.Enable();
        }

        // 2. Tự động liên kết từ PlayerInput giống PlayerMovement
        if (playerInput != null && playerInput.actions != null)
        {
            boundSlot1Action = FindAction(playerInput, "Skill1", "Slot1", "SkillQ", "SlotQ", "SkillSlot1");
            if (boundSlot1Action != null) boundSlot1Action.performed += OnSlot1Performed;

            boundSlot2Action = FindAction(playerInput, "Skill2", "Slot2", "SkillE", "SlotE", "SkillSlot2");
            if (boundSlot2Action != null) boundSlot2Action.performed += OnSlot2Performed;
        }
    }

    private void OnDisable()
    {
        if (skillSlot1Action != null)
        {
            skillSlot1Action.action.performed -= OnSlot1Performed;
            skillSlot1Action.action.Disable();
        }
        if (skillSlot2Action != null)
        {
            skillSlot2Action.action.performed -= OnSlot2Performed;
            skillSlot2Action.action.Disable();
        }

        if (boundSlot1Action != null) boundSlot1Action.performed -= OnSlot1Performed;
        if (boundSlot2Action != null) boundSlot2Action.performed -= OnSlot2Performed;
    }
    private InputAction FindAction(PlayerInput input, params string[] actionNames)
    {
        foreach (var name in actionNames)
        {
            var action = input.actions[name];
            if (action != null) return action;
        }
        return null;
    }

    private void Start()
    {
        // Kích hoạt slot ban đầu
        SelectSlot(activeSlotIndex);

        // Kiểm tra synergy nếu đã gán sẵn skill ban đầu trong Inspector
        EvaluateSynergies();
    }

    private void Update()
    {
        // Fall back independently for each slot when no Input Action is assigned.
        if (Time.timeScale > 0f && Keyboard.current != null)
        {
            if (boundSlot1Action == null && skillSlot1Action == null && Keyboard.current.qKey.wasPressedThisFrame)
                ActivateSlot(0);
            if (boundSlot2Action == null && skillSlot2Action == null && Keyboard.current.eKey.wasPressedThisFrame)
                ActivateSlot(1);
        }

        UpdateCooldowns();
    }

    // Callbacks khi nhận Input Action
    private void OnSlot1Performed(InputAction.CallbackContext context) => ActivateSlot(0);
    private void OnSlot2Performed(InputAction.CallbackContext context) => ActivateSlot(1);

    // Callbacks công khai dành cho UnityEvents trong PlayerInput Inspector
    public void OnSkillSlot1(InputAction.CallbackContext context) { if (context.performed) ActivateSlot(0); }
    public void OnSkillSlot2(InputAction.CallbackContext context) { if (context.performed) ActivateSlot(1); }

    private void UpdateCooldowns()
    {
        for (int i = 0; i < ultimateCooldownTimers.Length; i++)
        {
            if (ultimateCooldownTimers[i] > 0f)
            {
                ultimateCooldownTimers[i] -= Time.deltaTime;
                if (ultimateCooldownTimers[i] < 0f)
                {
                    ultimateCooldownTimers[i] = 0f;
                }

                float maxCooldown = slots[i] != null ? slots[i].UltimateCooldown : 1f;
                OnCooldownUpdated?.Invoke(i, ultimateCooldownTimers[i], maxCooldown);
            }
        }
    }

    // Selecting the initial slot in Start must not automatically cast a skill.
    public void ActivateSlot(int index)
    {
        if (!isActiveAndEnabled || PauseMenuUI.CombatInputBlocked) return;
        if (index < 0 || index >= slots.Length || index >= lastActivationFrames.Length) return;
        // PlayerInput UnityEvents and performed callbacks can report the same press.
        if (lastActivationFrames[index] == Time.frameCount) return;
        lastActivationFrames[index] = Time.frameCount;

        SelectSlot(index);
        OnSkillUseRequested?.Invoke();
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slots.Length) return;

        activeSlotIndex = index;
        OnActiveSlotChanged?.Invoke(activeSlotIndex);
    }

    public SkillData GetActiveSkill()
    {
        if (activeSlotIndex >= 0 && activeSlotIndex < slots.Length)
        {
            return slots[activeSlotIndex];
        }
        return null;
    }

    public bool CanUseUltimate()
    {
        SkillData activeSkill = GetActiveSkill();
        if (activeSkill == null) return false;

        return GetCooldownRemaining(activeSlotIndex) <= 0f;
    }

    public void TriggerUltimateCooldown()
    {
        SkillData activeSkill = GetActiveSkill();
        if (activeSkill == null) return;

        float cooldown = Mathf.Max(0f, activeSkill.UltimateCooldown);
        if (HasLinkedSynergy)
        {
            // Lock both buttons before notifying UI or other listeners.
            ultimateCooldownTimers[0] = cooldown;
            ultimateCooldownTimers[1] = cooldown;
            OnCooldownUpdated?.Invoke(0, cooldown, cooldown);
            OnCooldownUpdated?.Invoke(1, cooldown, cooldown);
        }
        else
        {
            ultimateCooldownTimers[activeSlotIndex] = cooldown;
            OnCooldownUpdated?.Invoke(activeSlotIndex, cooldown, cooldown);
        }
    }

    /// <summary>
    /// Trang bị một skill vào slot cụ thể
    /// </summary>
    public void EquipSkill(int slotIndex, SkillData skill)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        slots[slotIndex] = skill;
        ultimateCooldownTimers[slotIndex] = 0f; // Reset cooldown cho skill mới

        OnSlotSkillChanged?.Invoke(slotIndex, skill);

        // Sau khi trang bị, tự động kiểm tra công thức Synergy!
        EvaluateSynergies();
    }

    /// <summary>
    /// Nhặt thêm skill (tự điền vào slot trống đầu tiên, hoặc thay thế slot hiện tại nếu đã đầy 2 slot)
    /// </summary>
    public void AddSkill(SkillData newSkill)
    {
        if (newSkill == null) return;

        // Tìm slot trống đầu tiên
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                EquipSkill(i, newSkill);
                return;
            }
        }

        // Nếu cả 2 slot đều đã có skill, thay thế slot đang active
        EquipSkill(activeSlotIndex, newSkill);
    }

    /// <summary>
    /// Đánh giá và tự động kích hoạt Synergy nếu thỏa mãn công thức
    /// </summary>
    public void EvaluateSynergies()
    {
        if (synergyDatabase == null || synergyDatabase.Count == 0) return;

        // Kiểm tra các công thức Synergy
        foreach (var synergy in synergyDatabase)
        {
            if (synergy != null && synergy.RequiredSlotCount <= slots.Length && synergy.RequiredSlotCount >= 2)
            {
                if (synergy.TryMatchRecipe(slots, out var matchedIndices))
                {
                    ApplySynergyToSlots(synergy, matchedIndices);
                    return;
                }
            }
        }
    }
    private void ApplySynergyToSlots(SkillSynergyData synergy, List<int> matchedIndices)
    {
        Debug.Log($"[Synergy Activated!] Kích hoạt: {synergy.SynergyName} trên {matchedIndices.Count} slot!");

        // Preserve the longest remaining cooldown when two skills merge.
        float sharedCooldown = 0f;
        foreach (int index in matchedIndices)
            sharedCooldown = Mathf.Max(sharedCooldown, ultimateCooldownTimers[index]);

        // Update all participating slots before notifying the HUD.
        foreach (int index in matchedIndices)
        {
            slots[index] = synergy.ResultingSynergySkill;
            ultimateCooldownTimers[index] = sharedCooldown;
        }

        foreach (int index in matchedIndices)
            OnSlotSkillChanged?.Invoke(index, synergy.ResultingSynergySkill);
    }

    public void RestoreSessionState(SkillData[] savedSkills, float[] savedCooldowns, int selectedSlot)
    {
        if (savedSkills == null) return;
        // Restore both slots atomically, without EquipSkill resetting cooldowns or fusing midway.
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = i < savedSkills.Length ? savedSkills[i] : null;
            ultimateCooldownTimers[i] = savedCooldowns != null && i < savedCooldowns.Length
                ? Mathf.Max(0f, savedCooldowns[i]) : 0f;
        }
        activeSlotIndex = Mathf.Clamp(selectedSlot, 0, slots.Length - 1);
        for (int i = 0; i < slots.Length; i++)
        {
            OnSlotSkillChanged?.Invoke(i, slots[i]);
            OnCooldownUpdated?.Invoke(i, ultimateCooldownTimers[i], slots[i] != null ? slots[i].UltimateCooldown : 0f);
        }
        OnActiveSlotChanged?.Invoke(activeSlotIndex);
    }

    public float GetCooldownRemaining(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < ultimateCooldownTimers.Length)
        {
            if (HasLinkedSynergy && slotIndex < 2)
                return Mathf.Max(ultimateCooldownTimers[0], ultimateCooldownTimers[1]);
            return ultimateCooldownTimers[slotIndex];
        }
        return 0f;
    }
}

