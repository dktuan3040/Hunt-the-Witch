using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Spatial one-shot effects controlled by the existing Settings SFX slider.
public sealed class SfxAudio : MonoBehaviour
{
    private const string VolumeKey = "Settings_SfxVol";
    private static readonly HashSet<SfxAudio> active = new HashSet<SfxAudio>();
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeSettings()
    {
        active.Clear();
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_MasterVol", .8f));
    }

    public static AudioSource PlayAtPoint(AudioClip clip, Vector3 position)
    {
        if (clip == null) return null;
        var sound = new GameObject("SFX: " + clip.name);
        sound.transform.position = position;
        var player = sound.AddComponent<SfxAudio>();
        player.source = sound.AddComponent<AudioSource>();
        player.source.playOnAwake = false;
        player.source.spatialBlend = 1f;
        player.source.clip = clip;
        player.source.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .8f));
        active.Add(player);
        player.source.Play();
        player.StartCoroutine(player.Cleanup(clip.length));
        return player.source;
    }

    public static void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        foreach (var player in active)
            if (player != null && player.source != null) player.source.volume = value;
    }

    private IEnumerator Cleanup(float duration)
    {
        yield return new WaitForSecondsRealtime(duration + .05f);
        Destroy(gameObject);
    }

    private void OnDestroy() => active.Remove(this);
}
