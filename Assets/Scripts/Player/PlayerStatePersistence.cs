using UnityEngine;
using UnityEngine.SceneManagement;

// Keep scene-specific Player objects, cameras and UI; transfer only player data.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class PlayerStatePersistence : MonoBehaviour
{
    private Health health;
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
        weapon = GetComponentInChildren<WeaponShoot>(true);
        skin = GetComponent<PlayerSkinController>();
        generation = PlayerSessionState.Generation;
        var saved = PlayerSessionState.Player;
        if (saved != null)
        {
            if (weapon != null) weapon.EquipWeapon(saved.Weapon);
            if (skin != null && saved.Skin != null) skin.ApplySkin(saved.Skin);

            if (health != null)
            {
                int hp = saved.CurrentHealth <= 0 ? saved.MaxHealth : saved.CurrentHealth;
                health.RestoreSessionState(hp, saved.MaxHealth);
            }
        }
        initialized = true;
        Capture();
    }


    // Capture when unloading the scene, including while paused.
    private void OnDisable() => Capture();
    private void LateUpdate() => Capture();

    public void Capture()
    {
        if (!initialized || generation != PlayerSessionState.Generation) return;

        if (health == null || health.IsDead || health.CurrentHealth <= 0) return;
        var saved = PlayerSessionState.Player ?? new PlayerSessionState.Snapshot();
        saved.CurrentHealth = health.CurrentHealth;
        saved.MaxHealth = health.MaxHealth;
        saved.Weapon = weapon != null ? weapon.CurrentWeapon : null;
        saved.Skin = skin != null ? skin.CurrentSkin : null;
        if (TryGetComponent<PlayerSkillManager>(out var skills) && skills.LoadoutInitialized)
        {
            int count = skills.Slots.Length;
            if (saved.Skills == null || saved.Skills.Length != count)
            {
                saved.Skills = new SkillData[count];
                saved.SourceSkills = new SkillData[count];
                saved.Cooldowns = new float[count];
            }

            for (int i = 0; i < count; i++)
            {
                saved.Skills[i] = skills.Slots[i];
                saved.SourceSkills[i] = skills.GetReplacementSkill(i);
                saved.Cooldowns[i] = skills.GetCooldownRemaining(i);
            }
            saved.ActiveSlot = skills.ActiveSlotIndex;
        }
        PlayerSessionState.Store(saved);
    }
}