using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SkillKnowledge { Unknown, Discovered, Unlocked }

/// <summary>Session skill knowledge. Inventory pickups reveal skills without unlocking them.</summary>
public static class SkillUnlockProgress
{
    private static readonly Dictionary<string, SkillKnowledge> knowledge = new Dictionary<string, SkillKnowledge>();
    private static readonly Dictionary<string, SkillData> skills = new Dictionary<string, SkillData>();
    private static readonly Dictionary<string, SkillSynergyData> recipes = new Dictionary<string, SkillSynergyData>();
    private static int generation;
    private static readonly HashSet<SkillData> blocked = new HashSet<SkillData>();
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        knowledge.Clear(); skills.Clear(); recipes.Clear(); blocked.Clear(); Changed = null; generation = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        PlayerSessionState.InventoryChanged -= ObserveInventory;
        PlayerSessionState.InventoryChanged += ObserveInventory;
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
        generation = PlayerSessionState.Generation;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var catalog in Resources.FindObjectsOfTypeAll<SkillTreeCatalog>()) RegisterCatalog(catalog);
        foreach (var skill in Resources.FindObjectsOfTypeAll<SkillData>()) Register(skill);
        foreach (var recipe in Resources.FindObjectsOfTypeAll<SkillSynergyData>()) RegisterRecipe(recipe);
        foreach (var manager in Resources.FindObjectsOfTypeAll<PlayerSkillManager>())
            if (manager.gameObject.scene.IsValid() && manager.Slots != null)
                foreach (var skill in manager.Slots) MarkUnlocked(skill);
        ObserveInventory();
    }

    public static void RegisterCatalog(SkillTreeCatalog catalog)
    {
        foreach(var entry in catalog.entries)
        {
            if(entry.skill==null)continue;
            Register(entry.skill);
            if(entry.invalid) {blocked.Add(entry.skill);recipes.Remove(entry.skill.SkillId);}
            else blocked.Remove(entry.skill);
        }
        foreach(var entry in catalog.entries)if(!entry.invalid)RegisterRecipe(entry.recipe);
    }

    public static void Register(SkillData skill)
    {
        if (skill != null) skills[skill.SkillId] = skill;
    }

    public static void RegisterRecipe(SkillSynergyData recipe)
    {
        if (recipe == null || recipe.ResultingSynergySkill == null ||
            !recipe.ResultingSynergySkill.IsSynergySkill || blocked.Contains(recipe.ResultingSynergySkill)) return;
        Register(recipe.ResultingSynergySkill);
        recipes[recipe.ResultingSynergySkill.SkillId] = recipe;
        foreach (var prerequisite in recipe.RequiredSkills) Register(prerequisite);
    }

    public static SkillKnowledge GetState(SkillData skill)
    {
        SkillKnowledge value;
        return skill != null && knowledge.TryGetValue(skill.SkillId, out value) ? value : SkillKnowledge.Unknown;
    }

    public static SkillData PickLearnedSkill()
    {
        SkillData selected = null;
        int eligible = 0;
        foreach (var skill in skills.Values)
        {
            if (skill == null || skill.IsSynergySkill || blocked.Contains(skill) ||
                GetState(skill) != SkillKnowledge.Unlocked) continue;

            if (UnityEngine.Random.Range(0, ++eligible) == 0)
                selected = skill;
        }
        return selected;
    }

    public static void RevealSkill(SkillData skill)
    {
        Register(skill);
        if (Promote(skill, SkillKnowledge.Discovered)) Notify();
    }

    public static void MarkUnlocked(SkillData skill)
    {
        Register(skill);
        if (Promote(skill, SkillKnowledge.Unlocked)) Notify();
    }

    private static bool Promote(SkillData skill, SkillKnowledge state)
    {
        if (skill == null || GetState(skill) >= state) return false;
        knowledge[skill.SkillId] = state;
        return true;
    }

    private static bool DiscoverSynergies()
    {
        bool changed = false;
        // A known recipe cannot reveal a still-unknown component's identity.
        for (int pass=0;pass<recipes.Count;pass++)
        {
            bool advanced = false;
            foreach (var recipe in recipes.Values)
            {
                if (recipe == null || recipe.ResultingSynergySkill == null || blocked.Contains(recipe.ResultingSynergySkill)) continue;
                bool known = recipe.RequiredSkills.Count > 0;
                foreach (var required in recipe.RequiredSkills)
                    known &= required != null && GetState(required) >= SkillKnowledge.Discovered;
                if (known) advanced |= Promote(recipe.ResultingSynergySkill, SkillKnowledge.Discovered);
            }
            changed |= advanced;
            if (!advanced) break;
        }
        return changed;
    }

    private static void Notify()
    {
        DiscoverSynergies();
        Changed?.Invoke();
    }

    public static void ObserveInventory()
    {
        bool changed = false;
        if (generation != PlayerSessionState.Generation)
        {
            generation = PlayerSessionState.Generation;
            knowledge.Clear(); changed = true;
        }
        foreach (var skill in skills.Values)
        {
            if (skill == null) continue;
            foreach (var item in skill.UnlockItems)
                if (item != null && !string.IsNullOrWhiteSpace(item.ItemId) && PlayerSessionState.GetItemCount(item.ItemId) > 0)
                    changed |= Promote(skill, SkillKnowledge.Discovered);
        }
        changed |= DiscoverSynergies();
        if (changed) Changed?.Invoke();
    }

    private static bool GetCosts(SkillData skill, out Dictionary<string,int> costs)
    {
        costs = new Dictionary<string,int>();
        if (skill == null) return false;
        foreach (var item in skill.UnlockItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId)) return false;
            int old; costs.TryGetValue(item.ItemId,out old);
            long total = (long)old + Mathf.Max(1,item.amount);
            if (total > int.MaxValue) return false;
            costs[item.ItemId] = (int)total;
        }
        return true;
    }

    public static bool CanUnlock(SkillData skill)
    {
        if (skill == null || blocked.Contains(skill) || GetState(skill) != SkillKnowledge.Discovered) return false;
        SkillSynergyData recipe;
        if (recipes.TryGetValue(skill.SkillId,out recipe))
            foreach (var required in recipe.RequiredSkills)
                if (required == null || GetState(required) != SkillKnowledge.Unlocked) return false;
        Dictionary<string,int> costs;
        if (!GetCosts(skill,out costs)) return false;
        foreach (var pair in costs)
            if (PlayerSessionState.GetItemCount(pair.Key) < pair.Value) return false;
        return true;
    }

    public static bool TryUnlock(SkillData skill)
    {
        if (!CanUnlock(skill)) return false;
        Dictionary<string,int> costs;
        if (!GetCosts(skill,out costs) || !PlayerSessionState.TrySpendItems(costs)) return false;
        MarkUnlocked(skill);
        return true;
    }
}
