using UnityEngine;
using UnityEngine.SceneManagement;

// Keep scene-specific Player objects, cameras and UI; transfer only player data.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerStatePersistence : MonoBehaviour
{
    private Health health;
    private PlayerSkillManager skills;
    private WeaponShoot weapon;
    private PlayerSkinController skin;
    private int generation;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoader()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var player in root.GetComponentsInChildren<PlayerMovement>(true))
            {
                if (player.GetComponent<PlayerStatePersistence>() == null)
                    player.gameObject.AddComponent<PlayerStatePersistence>();
            }
        }
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        skills = GetComponent<PlayerSkillManager>();
        weapon = GetComponentInChildren<WeaponShoot>(true);
        skin = GetComponent<PlayerSkinController>();
        generation = PlayerSessionState.Generation;
        var saved = PlayerSessionState.Player;
        if (saved != null)
        {
            if (skills != null) skills.RestoreSessionState(saved.Skills, saved.Cooldowns, saved.ActiveSlot);
            if (weapon != null) weapon.EquipWeapon(saved.Weapon);
            if (skin != null && saved.Skin != null) skin.ApplySkin(saved.Skin);
            if (health != null) health.RestoreSessionState(saved.CurrentHealth, saved.MaxHealth);
        }
        initialized = true;
        Capture();
    }

    // Capture when unloading the scene, including while paused or already dead.
    private void OnDisable() => Capture();
    private void LateUpdate() => Capture();

    public void Capture()
    {
        if (!initialized || generation != PlayerSessionState.Generation) return;
        if (health == null) return;
        var saved = PlayerSessionState.Player ?? new PlayerSessionState.Snapshot();
        saved.CurrentHealth = health.CurrentHealth;
        saved.MaxHealth = health.MaxHealth;
        if (skills != null)
        {
            if (saved.Skills == null || saved.Skills.Length != skills.Slots.Length)
            {
                saved.Skills = new SkillData[skills.Slots.Length];
                saved.Cooldowns = new float[skills.Slots.Length];
            }
            for (int i = 0; i < skills.Slots.Length; i++)
            {
                saved.Skills[i] = skills.Slots[i];
                saved.Cooldowns[i] = skills.GetCooldownRemaining(i);
            }
            saved.ActiveSlot = skills.ActiveSlotIndex;
        }
        saved.Weapon = weapon != null ? weapon.CurrentWeapon : null;
        saved.Skin = skin != null ? skin.CurrentSkin : null;
        PlayerSessionState.Store(saved);
    }
}