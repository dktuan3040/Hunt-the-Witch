using System;
using System.Collections.Generic;
using UnityEngine;

// Runtime session only: scene changes preserve it; quitting the game does not.
public static class PlayerSessionState
{
    public sealed class Snapshot
    {
        public int CurrentHealth;
        public int MaxHealth;
        public WeaponData Weapon;
        public PlayerSkinData Skin;
        public SkillData[] Skills;
        public SkillData[] SourceSkills;
        public float[] Cooldowns;
        public int ActiveSlot;
    }

    private static readonly HashSet<string> visitedMaps = new HashSet<string>();

    public static bool VisitMap(UnityEngine.SceneManagement.Scene scene)
    {
        return visitedMaps.Add(string.IsNullOrEmpty(scene.path) ? scene.name : scene.path);
    }

    public static Snapshot Player { get; private set; }
    public static int Generation { get; private set; }
    private static readonly Dictionary<string, int> inventory = new Dictionary<string, int>();
    public static IReadOnlyDictionary<string, int> Inventory => inventory;
    public static event Action InventoryChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeSession()
    {
        Player = null;
        Generation = 0;
        visitedMaps.Clear();
        inventory.Clear();
        InventoryChanged = null;
    }

    public static void Store(Snapshot snapshot) => Player = snapshot;

    // Generation prevents the old scene's OnDisable from saving over a new game.
    public static void StartNewGame()
    {
        Generation++;
        visitedMaps.Clear();
        Player = null;
        inventory.Clear();
        InventoryChanged?.Invoke();
    }

    public static int GetItemCount(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && inventory.TryGetValue(itemId, out int count) ? count : 0;
    }

    public static void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0) return;
        inventory[itemId] = checked(GetItemCount(itemId) + amount);
        InventoryChanged?.Invoke();
    }

    public static bool TryRemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 || GetItemCount(itemId) < amount) return false;
        int remaining = inventory[itemId] - amount;
        if (remaining == 0) inventory.Remove(itemId);
        else inventory[itemId] = remaining;
        InventoryChanged?.Invoke();
        return true;
    }
    // Validate and spend the whole cost before sending one inventory change event.
    public static bool TrySpendItems(IReadOnlyDictionary<string,int> costs)
    {
        if (costs == null) return false;
        foreach (var pair in costs)
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0 || GetItemCount(pair.Key) < pair.Value) return false;
        bool changed = false;
        foreach (var pair in costs)
        {
            if (pair.Value == 0) continue;
            int remaining = GetItemCount(pair.Key) - pair.Value;
            if (remaining == 0) inventory.Remove(pair.Key); else inventory[pair.Key] = remaining;
            changed = true;
        }
        if (changed) InventoryChanged?.Invoke();
        return true;
    }
}