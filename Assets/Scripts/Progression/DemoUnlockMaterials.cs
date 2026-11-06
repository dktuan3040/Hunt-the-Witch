#if UNITY_EDITOR || HUNT_WITCH_DEMO
using System;
using System.Collections.Generic;

public static class DemoUnlockMaterials
{
#if HUNT_WITCH_DEMO
    private static bool staffInstalled;
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDemo() { staffInstalled = false; }
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeDemo()
    {
        if (staffInstalled) return;
        var catalog = GameContentCatalog.Instance;
        if (catalog == null || catalog.weapons.Count == 0 || catalog.weapons[0] == null)
            throw new InvalidOperationException("Demo staff template missing.");
        var staff = WeaponData.CreateDemoStaff(catalog.weapons[0]);
        catalog.weapons.Add(staff);
        var materials = GameProfile.Bank;
        GameProfile.SelectWeapon(staff);
        staffInstalled = true;
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
