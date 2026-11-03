using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Hunt the Witch/UI/Skill Tree Catalog")]
public sealed class SkillTreeCatalog : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public string assetGuid;
        public SkillData skill;
        public SkillSynergyData recipe;
        public Vector2 position;
        public bool invalid;
    }
    public List<Entry> entries = new List<Entry>();
    [TextArea] public string validationErrors;
    public bool IsBlocked(SkillData skill) { return entries.Exists(e=>e.skill==skill && e.invalid); }
}
