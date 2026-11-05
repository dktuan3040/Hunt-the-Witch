using System.Collections.Generic;
using UnityEngine;

// A quiet common music level, with per-track loudness compensation.
[DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
public sealed class BgmAudio : MonoBehaviour
{
    private const string VolumeKey = "Settings_BgmVol";
    private static readonly HashSet<BgmAudio> active = new HashSet<BgmAudio>();
    [SerializeField, Range(0f, 1f)] private float baseVolume = .06f;
    [SerializeField, Range(0f, 1f)] private float trackGain = 1f;
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRegistry() => active.Clear();

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.spatialBlend = 0f;
        ApplyVolume();
    }

    private void OnEnable()
    {
        active.Add(this);
        ApplyVolume();
    }

    private void OnDisable() => active.Remove(this);

    private void ApplyVolume()
    {
        if (source == null) source = GetComponent<AudioSource>();
        source.volume = baseVolume * trackGain * Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .8f));
    }

    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value));
        foreach (var music in active)
            if (music != null) music.ApplyVolume();
    }
}
