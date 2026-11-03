using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName="SkillScrollLoot",menuName="Hunt the Witch/Items/Skill Scroll Loot")]
public sealed class SkillScrollLootData : ScriptableObject
{
    public SkillTreeCatalog catalog;
    public SkillScrollPickup pickupPrefab;
    public SkillData Roll(float chance,System.Random random)
    {
        if(catalog==null||random==null||chance<=0||random.NextDouble()>=Mathf.Clamp01(chance))return null;
        var eligible=catalog.entries.Where(e=>e.skill!=null&&!e.invalid&&e.recipe==null&&!e.skill.IsSynergySkill&&SkillUnlockProgress.GetState(e.skill)==SkillKnowledge.Unlocked).Select(e=>e.skill).Distinct().ToArray();
        return eligible.Length==0?null:eligible[random.Next(eligible.Length)];
    }
    public SkillScrollPickup TrySpawn(float chance,System.Random random,Vector3 position)
    {
        if(pickupPrefab==null)return null;var skill=Roll(chance,random);
        return skill!=null?SkillScrollPickup.Spawn(pickupPrefab,skill,position,null):null;
    }
}
