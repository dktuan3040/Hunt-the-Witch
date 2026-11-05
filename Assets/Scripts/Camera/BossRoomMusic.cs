using UnityEngine;

// Attach to the boss music AudioSource. Uses the existing boss-room camera boundary.
[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
public sealed class BossRoomMusic : MonoBehaviour
{
    [SerializeField] private MapCameraRegions cameraRegions;
    [Tooltip("Optional: the normal map music to stop when the boss music starts.")]
    [SerializeField] private AudioSource mapMusic;
    private AudioSource bossMusic;
    private bool started;

    private void Awake()
    {
        bossMusic = GetComponent<AudioSource>();
        bossMusic.playOnAwake = false;
        bossMusic.Stop();
        bossMusic.loop = true;
        bossMusic.spatialBlend = 0f;
    }

    private void Start()
    {
        if (cameraRegions != null) return;
        foreach (var regions in FindObjectsByType<MapCameraRegions>())
        {
            if (regions.gameObject.scene != gameObject.scene) continue;
            cameraRegions = regions;
            break;
        }
    }

    private void Update()
    {
        if (started || cameraRegions == null || !cameraRegions.IsInBossRegion
            || PauseMenuUI.InteractionInputBlocked || bossMusic.clip == null) return;
        started = true;
        if (mapMusic != null && mapMusic != bossMusic) mapMusic.Stop();
        bossMusic.Play();
    }
}
