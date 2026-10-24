using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillSynergy", menuName = "Hunt the Witch/Skills/Skill Synergy Data")]
public class SkillSynergyData : ScriptableObject
{
    [Header("Synergy Info")]
    [SerializeField] private string synergyName = "Firestorm Synergy";
    [TextArea(2, 3)]
    [SerializeField] private string description = "Combines Fire and Wind to unleash massive flaming whirlwinds.";

    [Header("Recipe")]
    [Tooltip("Danh sách các skill cần có trên người để kết hợp.")]
    [SerializeField] private List<SkillData> requiredSkills = new List<SkillData>();

    [Header("Result")]
    [Tooltip("Skill Synergy được tạo thành. Tất cả các slot tham gia vào công thức sẽ chuyển hóa thành skill này!")]
    [SerializeField] private SkillData resultingSynergySkill;

    // Public Getters
    public string SynergyName => synergyName;
    public string Description => description;
    public List<SkillData> RequiredSkills => requiredSkills;
    public SkillData ResultingSynergySkill => resultingSynergySkill;
    public int RequiredSlotCount => requiredSkills != null ? requiredSkills.Count : 0;

    /// <summary>
    /// Kiểm tra xem mảng slot hiện tại của người chơi có thỏa mãn công thức Synergy này không
    /// </summary>
    public bool TryMatchRecipe(SkillData[] currentSlots, out List<int> matchedSlotIndices)
    {
        matchedSlotIndices = new List<int>();

        if (requiredSkills == null || requiredSkills.Count == 0 || resultingSynergySkill == null)
        {
            return false;
        }

        // Tạo danh sách ID các skill cần tìm
        List<string> neededSkillIds = new List<string>();
        foreach (var req in requiredSkills)
        {
            if (req != null)
            {
                neededSkillIds.Add(req.SkillId);
            }
        }

        List<int> candidateIndices = new List<int>();

        // Duyệt qua các slot của người chơi
        for (int i = 0; i < currentSlots.Length; i++)
        {
            if (currentSlots[i] == null) continue;

            // Nếu slot này đã là synergy bậc cao hơn hoặc bằng thì không gộp lại
            string currentId = currentSlots[i].SkillId;

            int matchIndex = neededSkillIds.IndexOf(currentId);
            if (matchIndex != -1)
            {
                candidateIndices.Add(i);
                neededSkillIds.RemoveAt(matchIndex);
            }
        }

        // Nếu đã tìm thấy đủ tất cả các thành phần yêu cầu
        if (neededSkillIds.Count == 0)
        {
            matchedSlotIndices = candidateIndices;
            return true;
        }

        return false;
    }
}

