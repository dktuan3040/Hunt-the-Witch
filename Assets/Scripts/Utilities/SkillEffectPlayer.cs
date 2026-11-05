using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SkillEffectSequence
{
    public Sprite[] startup;
    public Sprite[] active;
    public Sprite[] ending;
    public Sprite[] impact;
    [Min(1)] public float framesPerSecond = 18;
    [Min(0)] public float areaRadius;
    [Min(0)] public float castDistance;
    public float FrameTime => 1f / Mathf.Max(1, framesPerSecond);
    public bool IsConfigured => active != null && active.Length > 0;
}

public sealed class SkillEffectPlayer : MonoBehaviour
{
    public static IEnumerator PlayFrames(SpriteRenderer renderer, Sprite[] frames, float frameTime)
    {
        if (frames == null) yield break;
        foreach (var frame in frames)
        {
            renderer.sprite = frame;
            yield return new WaitForSeconds(frameTime);
        }
    }

    public static void SpawnOneShot(SpriteRenderer source, Vector3 position, Quaternion rotation,
        Vector3 scale, Sprite[] frames, float frameTime)
    {
        if (frames == null || frames.Length == 0) return;
        var player = Create(source, position, rotation, scale);
        player.StartCoroutine(player.PlayOneShot(frames, frameTime));
    }

    public static void CastArea(SpriteRenderer source, Vector3 origin, Vector2 direction,
        ProjectileData data, float multiplier)
    {
        var effect = data.Effect;
        var position = origin + (Vector3)(direction * effect.castDistance);
        var rotation = effect.castDistance < 2f
            ? Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + data.SpriteOffset)
            : Quaternion.identity;
        var player = Create(source, position, rotation, data.ProjectileScale);
        player.StartCoroutine(player.PlayArea(data, direction, multiplier));
    }

    internal static SkillEffectPlayer Create(SpriteRenderer source, Vector3 position, Quaternion rotation, Vector3 scale, bool separateVisual = false)
    {
        var visual = new GameObject("Skill Effect");
        if (source != null && source.gameObject.scene.IsValid() && source.gameObject.scene.isLoaded)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(visual, source.gameObject.scene);
        visual.transform.SetPositionAndRotation(position, rotation);
        visual.transform.localScale = scale;
        var spriteObject = visual;
        if (separateVisual)
        {
            spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(visual.transform, false);
        }
        var renderer = spriteObject.AddComponent<SpriteRenderer>();
        if (source != null)
        {
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder + 1;
        }
        return visual.AddComponent<SkillEffectPlayer>();
    }

    internal void PlayAttached(Transform target, Sprite[] frames, float frameTime)
    {
        StartCoroutine(FollowTarget(target, frames, frameTime));
    }

    private IEnumerator FollowTarget(Transform target, Sprite[] frames, float frameTime)
    {
        var renderer = GetComponent<SpriteRenderer>();
        for (float t = 0; target != null && t < frames.Length * frameTime; t += Time.deltaTime)
        {
            transform.position = target.position;
            renderer.sprite = frames[Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t / frameTime))];
            yield return null;
        }
        Destroy(gameObject);
    }

    private IEnumerator PlayOneShot(Sprite[] frames, float frameTime)
    {
        yield return PlayFrames(GetComponent<SpriteRenderer>(), frames, frameTime);
        Destroy(gameObject);
    }

    private IEnumerator PlayArea(ProjectileData data, Vector2 direction, float multiplier)
    {
        var effect = data.Effect;
        var renderer = GetComponent<SpriteRenderer>();
        yield return PlayFrames(renderer, effect.startup, effect.FrameTime);
        renderer.sprite = effect.active[0];

        var damaged = new HashSet<IDamageable>();
        var overlaps = new List<Collider2D>();
        gameObject.scene.GetPhysicsScene2D().OverlapCircle(transform.position, effect.areaRadius,
            ContactFilter2D.noFilter, overlaps);
        foreach (var collider in overlaps)
        {
            var target = collider.GetComponentInParent<IDamageable>();
            if (target == null || target.Team != Team.Enemy || target.IsDead || !damaged.Add(target)) continue;
            target.TakeDamage(Mathf.RoundToInt(data.Damage * multiplier), direction);
            if (data.ImpactSound != null)
                AudioSource.PlayClipAtPoint(data.ImpactSound, collider.bounds.center);
            SpawnOneShot(renderer, collider.bounds.center, transform.rotation, transform.localScale,
                effect.impact, effect.FrameTime);
        }

        yield return PlayFrames(renderer, effect.active, effect.FrameTime);
        yield return PlayFrames(renderer, effect.ending, effect.FrameTime);
        Destroy(gameObject);
    }
}
