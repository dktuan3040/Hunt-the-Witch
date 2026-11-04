using System.Collections.Generic;
using UnityEngine;

/// <summary>Shares the gameplay clock between menus that can overlap.</summary>
internal static class GameplayPause
{
    private static readonly HashSet<object> owners = new HashSet<object>();
    private static float previousTimeScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        owners.Clear();
        previousTimeScale = 1f;
    }

    public static bool IsPaused => owners.Count > 0;
    public static bool HasOtherOwner(object owner) =>
        owners.Count > (owners.Contains(owner) ? 1 : 0);

    public static void Acquire(object owner)
    {
        if (!owners.Add(owner)) return;
        if (owners.Count == 1)
            previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public static void Release(object owner)
    {
        if (!owners.Remove(owner) || owners.Count != 0) return;
        Time.timeScale = previousTimeScale;
        PauseMenuUI.BlockUntilGameplayInputReleased();
    }
}
