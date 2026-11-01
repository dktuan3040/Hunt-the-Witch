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
        public SkillData[] Skills;
        public float[] Cooldowns;
        public int ActiveSlot;
        public WeaponData Weapon;
        public PlayerSkinData Skin;
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
        inventory.Clear();
        InventoryChanged = null;
    }

    public static void Store(Snapshot snapshot) => Player = snapshot;

    // Generation prevents the old scene's OnDisable from saving over a new game.
    public static void StartNewGame()
    {
        Generation++;
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
}