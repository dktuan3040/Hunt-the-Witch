#if UNITY_EDITOR || HUNT_WITCH_DEMO
using System;
using System.Collections.Generic;

public static class DemoUnlockMaterials
{
#if HUNT_WITCH_DEMO
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeDemo()
    {
        var materials = GameProfile.Bank;
        UnityEngine.Debug.Log("Demo inventory ready: " + materials.Count + " material types.");
    }
#endif
    public static Dictionary<string, int> RequiredTotals(GameContentCatalog catalog)
    {
        if (catalog == null) throw new InvalidOperationException("Game content catalog is missing.");
        var totals = new Dictionary<string, int>();
        foreach (var skill in catalog.skills)
            if (skill != null && skill.UnlockItems != null)
                foreach (var cost in skill.UnlockItems)
                    if (cost != null) Add(totals, cost.ItemId, cost.amount);
        foreach (var weapon in catalog.weapons)
            if (weapon != null && weapon.UnlockItems != null)
                foreach (var cost in weapon.UnlockItems)
                    if (cost != null) Add(totals, cost.item != null ? cost.item.ItemId : null, cost.amount);
        foreach (var skin in catalog.skins)
            if (skin != null && skin.UnlockItems != null)
                foreach (var cost in skin.UnlockItems)
                    if (cost != null) Add(totals, cost.item != null ? cost.item.ItemId : null, cost.amount);
        return totals;
    }
    private static void Add(Dictionary<string,int> totals, string id, int amount)
    {
        if (string.IsNullOrWhiteSpace(id) || amount < 1)
            throw new InvalidOperationException("Unlock recipe contains an invalid material requirement.");
        int current; totals.TryGetValue(id, out current);
        totals[id] = checked(current + amount);
    }
}
#endif
