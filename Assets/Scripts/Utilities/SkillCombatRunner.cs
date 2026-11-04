using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkillCombatRunner : MonoBehaviour
{
    private SkillCombatSettings settings;
    private SkillEffectSequence effect;
    private SpriteRenderer visual;
    private PhysicsScene2D physicsScene;
    private Transform anchor;
    private Transform caster;
    private Health target;
    private Collider2D targetFootCollider;
    private Vector2 origin;
    private Vector2 direction;
    private float multiplier;
    private float phase;
    private float delay;
    private float previewReach;
    private HashSet<Health> struck = new HashSet<Health>();
    private readonly Dictionary<Health, float> nextDamage = new Dictionary<Health, float>();
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly List<Health> enemies = new List<Health>();
    private float FrameTime => effect.FrameTime;
    private float Radius => Mathf.Max(.05f, settings.radius);
    private float Duration => Mathf.Max(.05f, settings.duration);
    private float Interval => Mathf.Max(.05f, settings.tickInterval);

    public static void Cast(SkillData skill, Transform caster, Transform muzzle, SpriteRenderer source, float multiplier)
    {
        var config = skill.Combat;
        Vector2 aim = muzzle.right;
        Vector2 atPlayer = caster.position;
        Vector2 atMuzzle = muzzle.position;
        int count = skill.UltimateProjectilesPerShot;
        switch (config.pattern)
        {
            case SkillCastPattern.Area:
                var affected = new List<Health>();
                Collect(caster.gameObject.scene.GetPhysicsScene2D(), atPlayer, Mathf.Max(.05f, config.radius),
                    new List<Collider2D>(), affected);
                foreach (var enemy in affected)
                    StartVisual(skill, source, caster, enemy.transform.position, aim, multiplier, target: enemy);
                break;
            case SkillCastPattern.Nearest:
                var targets = new List<Health>();
                Collect(caster.gameObject.scene.GetPhysicsScene2D(), atPlayer, config.range,
                    new List<Collider2D>(), targets);
                targets.Sort((a,b) => ((Vector2)a.transform.position-atPlayer).sqrMagnitude
                    .CompareTo(((Vector2)b.transform.position-atPlayer).sqrMagnitude));
                int chosen = Mathf.Max(1, config.targetCount);
                int casts = config.repeatTargets && targets.Count > 0 ? chosen : Mathf.Min(chosen, targets.Count);
                for (int i = 0; i < casts; i++)
                {
                    var enemy = targets[i % targets.Count];
                    StartVisual(skill, source, caster, enemy.transform.position, aim, multiplier, target: enemy);
                }
                break;
            case SkillCastPattern.WallLines:
                float spacing = Mathf.Max(.05f, config.wallSpacing);
                int segments = Mathf.Clamp(Mathf.FloorToInt(config.range / spacing), 1, 64);
                for (int i = 0; i < count; i++)
                {
                    Vector2 dir = Spread(aim, i, count, skill.UltimateSpreadAngle);
                    float clear = WallDistance(caster.gameObject.scene.GetPhysicsScene2D(), atMuzzle, dir, config.range);
                    for (int j = 1; j <= segments && j * spacing <= clear; j++)
                        StartVisual(skill, source, caster, atMuzzle + dir * (j * spacing), dir,
                            multiplier, delay: (j-1) * config.wallDelay);
                }
                break;
            case SkillCastPattern.Projectile:
            case SkillCastPattern.Arc:
            case SkillCastPattern.Vortex:
            case SkillCastPattern.Burst:
                var sharedHits = config.pattern == SkillCastPattern.Burst ? new HashSet<Health>() : null;
                for (int i = 0; i < count; i++)
                {
                    Vector2 dir = config.radial ? Rotate(Vector2.up, i * 360f / count)
                        : Spread(aim, i, count, skill.UltimateSpreadAngle);
                    StartVisual(skill, source, caster, atMuzzle, dir, multiplier,
                        anchor: config.followCaster ? muzzle : null,
                        phase: i * 360f / count, sharedHits: sharedHits);
                }
                break;
            default:
                Vector2 point = config.pattern == SkillCastPattern.PullTrap ? atPlayer : atMuzzle;
                StartVisual(skill, source, caster, point, aim, multiplier,
                    anchor: config.followCaster ? muzzle : null);
                break;
        }
    }

    private static void StartVisual(SkillData skill, SpriteRenderer source, Transform caster,
        Vector2 point, Vector2 direction, float multiplier, Transform anchor = null,
        Health target = null, float phase = 0, float delay = 0, HashSet<Health> sharedHits = null)
    {
        var data = skill.UltimateProjectile;
        var visualObject = SkillEffectPlayer.Create(source, point, Quaternion.identity, data.ProjectileScale,
            separateVisual: skill.Combat.visualOffset != Vector2.zero);
        var runner = visualObject.gameObject.AddComponent<SkillCombatRunner>();
        runner.settings = skill.Combat;
        runner.effect = data.Effect;
        runner.visual = visualObject.GetComponentInChildren<SpriteRenderer>();
        runner.physicsScene = visualObject.gameObject.scene.GetPhysicsScene2D();
        runner.caster = caster;
        runner.anchor = anchor;
        runner.target = target;
        if (target != null && runner.settings.placeAtTargetFeet)
        {
            runner.targetFootCollider = FindFootCollider(target);
            runner.transform.position = runner.GetTargetFeet();
        }
        runner.origin = point;
        runner.direction = direction;
        runner.phase = phase;
        runner.delay = delay;
        runner.multiplier = multiplier;
        if (sharedHits != null) runner.struck = sharedHits;
        if (runner.settings.pattern == SkillCastPattern.Channel || runner.settings.pattern == SkillCastPattern.Projectile ||
            runner.settings.pattern == SkillCastPattern.Burst)
            runner.AimVisual();
        runner.StartCoroutine(runner.Run());
    }

    private IEnumerator Run()
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (!CanContinue()) { Destroy(gameObject); yield break; }
        switch (settings.pattern)
        {
            case SkillCastPattern.Arc:
                yield return FlyArc();
                break;
            case SkillCastPattern.Vortex:
                yield return Spiral();
                break;
            default:
                yield return Phase(effect.startup, false);
                if (!CanContinue()) break;
                if (settings.pattern == SkillCastPattern.Nearest && settings.explosionRadius > 0)
                    target = null;
                if (settings.pattern == SkillCastPattern.Projectile)
                    yield return FlyProjectile();
                else
                    yield return Stay();
                break;
        }
        Destroy(gameObject);
    }

    private bool CanContinue()
    {
        if (!settings.followCaster) return true;
        if (anchor == null || caster == null) return false;
        return !caster.TryGetComponent<Health>(out var health) || !health.IsDead;
    }

    private void Follow()
    {
        if (anchor != null)
        {
            origin = anchor.position;
            if (settings.pattern == SkillCastPattern.Channel ||
                (settings.pattern == SkillCastPattern.Burst && !settings.radial))
            {
                direction = anchor.right;
                AimVisual();
            }
            transform.position = origin;
        }
        else if (target != null && !target.IsDead)
            transform.position = settings.placeAtTargetFeet
                ? GetTargetFeet() : target.transform.position;
    }

    private void AimVisual()
    {
        bool facingLeft = direction.x < 0f;
        transform.rotation = settings.keepSpriteHorizontal ? Quaternion.identity : Rotation(direction);
        visual.flipX = settings.keepSpriteHorizontal && facingLeft;
        visual.flipY = !settings.keepSpriteHorizontal && settings.mirrorWhenAimingLeft && facingLeft;

        if (visual.transform != transform)
        {
            var offset = settings.visualOffset;
            visual.transform.localPosition = new Vector3(
                offset.x * (visual.flipX ? -1 : 1) / transform.localScale.x,
                offset.y * (visual.flipY ? -1 : 1) / transform.localScale.y,
                0);
        }
    }

    private static Collider2D FindFootCollider(Health enemy)
    {
        foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
            if (collider.enabled && !collider.isTrigger)
                return collider;
        return null;
    }

    private Vector3 GetTargetFeet()
    {
        var position = target.transform.position;
        if (targetFootCollider != null && targetFootCollider.enabled)
            position.y = targetFootCollider.bounds.min.y;
        position.y += settings.targetFootOffsetY;
        return position;
    }

    private IEnumerator Phase(Sprite[] frames, bool damage)
    {
        float length = Length(frames) * FrameTime;
        for (float t = 0; t < length && CanContinue(); t += Time.deltaTime)
        {
            Follow();
            SetFrame(frames, t, false);
            if (damage) TickStationary(t);
            yield return null;
        }
    }

    private IEnumerator Stay()
    {
        BoxCollider2D wall = null;
        if (settings.pattern == SkillCastPattern.WallLines)
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
            {
                gameObject.layer = wallLayer;
                wall = gameObject.AddComponent<BoxCollider2D>();
                wall.size = new Vector2(Radius / Mathf.Abs(transform.localScale.x), Radius / Mathf.Abs(transform.localScale.y));
            }
        }
        for (float t = 0; t < Duration && CanContinue(); t += Time.deltaTime)
        {
            Follow();
            SetFrame(effect.active, t, true);
            TickStationary(t);
            yield return null;
        }
        if (wall != null) wall.enabled = false;
        yield return Phase(effect.ending, settings.hitDuringEnding);
    }

    private Vector2 HitboxWorldOffset()
    {
        Vector2 offset = settings.hitboxOffset;
        if (visual.flipX) offset.x = -offset.x;
        if (visual.flipY) offset.y = -offset.y;
        Vector2 worldOffset = transform.rotation * (Vector3)offset;
        if (settings.hitboxFollowsVisual)
            worldOffset += (Vector2)(visual.transform.position - transform.position);
        return worldOffset;
    }

    private void TickStationary(float elapsed)
    {
        Vector2 center = (Vector2)transform.position + HitboxWorldOffset();
        if (settings.pattern == SkillCastPattern.Area)
        {
            if (target != null && !target.IsDead)
                Damage(target, settings.damage, settings.damageOverTime);
            return;
        }
        if (settings.pattern == SkillCastPattern.Nearest && settings.explosionRadius <= 0)
        {
            if (target != null && !target.IsDead) Damage(target, settings.damage, true);
            return;
        }
        if (settings.pattern == SkillCastPattern.Channel || settings.pattern == SkillCastPattern.Burst)
        {
            float reach = Mathf.Min(WallDistance(physicsScene, origin, direction, settings.range),
                WallDistance(physicsScene, center, direction, settings.range));
            if (settings.pattern == SkillCastPattern.Burst)
                reach = Mathf.Min(reach, Mathf.Max(Radius, settings.speed * elapsed));
            previewReach = reach;
            Collect(physicsScene, center + direction * (reach * .5f), reach * .5f + Radius, overlaps, enemies);
            foreach (var enemy in enemies)
            {
                Vector2 offset = (Vector2)enemy.transform.position - center;
                float along = Vector2.Dot(offset, direction);
                if (along < -Radius * .4f || along > reach || (offset - direction * along).sqrMagnitude > Radius * Radius)
                    continue;
                Damage(enemy, settings.damage, settings.pattern == SkillCastPattern.Channel);
            }
            return;
        }
        Collect(physicsScene, center, settings.explosionRadius > 0 ? settings.explosionRadius : Radius, overlaps, enemies);
        foreach (var enemy in enemies)
        {
            if (settings.pullSpeed > 0) EnemySkillStatus.For(enemy).Pull(center, settings.pullSpeed);
            bool repeat = settings.damageOverTime;
            Damage(enemy, settings.damage, repeat);
        }
    }

    private IEnumerator FlyProjectile()
    {
        Vector2 at = transform.position;
        float speed = Mathf.Max(.05f, settings.speed);
        float range = Mathf.Max(.05f, settings.range);
        float flown = 0;
        bool blocked = false;
        for (float t = 0; t < Duration && flown < range && !blocked; t += Time.deltaTime)
        {
            SetFrame(effect.active, t, true);
            float step = Mathf.Min(speed * Mathf.Min(Time.deltaTime, Duration - t), range - flown);
            float clear = Mathf.Min(WallDistance(physicsScene, at, direction, step),
                WallDistance(physicsScene, at + HitboxWorldOffset(), direction, step));
            int samples = Mathf.Max(1, Mathf.CeilToInt(clear / Mathf.Max(.04f, Radius * .5f)));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 point = at + direction * (clear * i / samples);
                transform.position = point;
                if (Sweep(point)) { blocked = true; break; }
            }
            if (blocked || clear < step) break;
            flown += clear;
            at += direction * clear;
            yield return null;
        }
        if (settings.explosionRadius > 0)
        {
            if (!blocked && !settings.explodeOnExpiry) yield break;
            SetFrame(effect.ending, 0, false);
            Explode(transform.position);
        }
        yield return Phase(effect.ending, false);
    }

    private bool Sweep(Vector2 point)
    {
        point += HitboxWorldOffset();
        Collect(physicsScene, point, settings.trailRadius > 0 ? settings.trailRadius : Radius, overlaps, enemies);
        if (settings.onHitSkill != null)
            enemies.Sort((a, b) => Vector2.Dot(a.transform.position, direction)
                .CompareTo(Vector2.Dot(b.transform.position, direction)));
        foreach (var enemy in enemies)
        {
            if (settings.pullSpeed > 0) EnemySkillStatus.For(enemy).Pull(point, settings.pullSpeed);
            if (settings.explosionRadius > 0)
            {
                if (settings.trailRadius <= 0) return true;
                Damage(enemy, settings.trailDamage, true, applyStatus: false);
                continue;
            }

            Damage(enemy, settings.damage, false);
            if (!settings.pierce) return true;
        }
        return false;
    }

    private IEnumerator FlyArc()
    {
        Vector2 start = origin;
        Vector2 end = start + direction * WallDistance(physicsScene, start, direction, settings.range);
        for (float t = 0; t < Duration; t += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(t / Duration);
            transform.position = Vector2.Lerp(start, end, progress) +
                Vector2.up * (4 * progress * (1 - progress) * settings.arcHeight);
            if (t < Length(effect.startup) * FrameTime) SetFrame(effect.startup, t, false);
            else SetFrame(effect.active, 0, false);
            yield return null;
        }
        transform.position = end;
        SetFrame(effect.ending, 0, false);
        SkillEffectPlayer.SpawnOneShot(visual, end, Quaternion.identity, transform.localScale, effect.impact, FrameTime);
        Explode(end, playImpact: false);
        yield return Phase(effect.ending, false);
    }

    private IEnumerator Spiral()
    {
        float startup = Length(effect.startup) * FrameTime;
        for (float t = 0; t < startup + Duration; t += Time.deltaTime)
        {
            float radius = Mathf.Min(settings.range, settings.vortexStartRadius + settings.speed * t);
            transform.position = origin + Rotate(Vector2.up, phase + settings.vortexDegreesPerSecond * t) * radius;
            if (t < startup) SetFrame(effect.startup, t, false);
            else SetFrame(effect.active, t - startup, true);
            Sweep(transform.position);
            yield return null;
        }
        yield return Phase(effect.ending, false);
    }

    private void Explode(Vector2 center, bool playImpact = true)
    {
        Collect(physicsScene, center + HitboxWorldOffset(), settings.explosionRadius, overlaps, enemies);
        foreach (var enemy in enemies)
            Damage(enemy, settings.explosionDamage, false, playImpact: playImpact, useSeparateHit: true);
    }

    private void Damage(Health enemy, int amount, bool repeat, bool applyStatus = true,
        bool playImpact = true, bool useSeparateHit = false)
    {
        if (enemy == null || enemy.IsDead) return;
        if (repeat)
        {
            if (nextDamage.TryGetValue(enemy, out float next) && Time.time < next) return;
            nextDamage[enemy] = Time.time + Interval;
        }
        else if (!useSeparateHit && !struck.Add(enemy)) return;

        Vector2 position = enemy.transform.position;
        if (playImpact && Length(effect.impact) > 0)
            SpawnAttachedImpact(enemy);
        if (applyStatus && settings.onHitSkill != null && settings.onHitSkill.Combat != null)
            StartVisual(settings.onHitSkill, visual, caster, position, direction, multiplier, target: enemy);

        enemy.TakeDamage(Mathf.RoundToInt(amount * multiplier));
        if (!applyStatus || enemy.IsDead) return;
        if (settings.status == SkillStatus.None && settings.knockbackDistance <= 0) return;
        var status = EnemySkillStatus.For(enemy);
        status.Apply(settings, multiplier);
        if (settings.knockbackDistance > 0)
            status.Push(direction, settings.knockbackDistance, settings.knockbackDuration);
    }

    private void SpawnAttachedImpact(Health enemy)
    {
        var hit = SkillEffectPlayer.Create(visual, enemy.transform.position, transform.rotation, transform.localScale);
        hit.GetComponent<SpriteRenderer>().flipY = visual.flipY;
        hit.PlayAttached(enemy.transform, effect.impact, FrameTime);
    }

    private static void Collect(PhysicsScene2D scene, Vector2 point, float radius,
        List<Collider2D> colliders, List<Health> result)
    {
        colliders.Clear();
        result.Clear();
        scene.OverlapCircle(point, radius, ContactFilter2D.noFilter, colliders);
        foreach (var collider in colliders)
        {
            var enemy = collider.GetComponentInParent<Health>();
            if (enemy != null && enemy.Team == Team.Enemy && !enemy.IsDead && !result.Contains(enemy))
                result.Add(enemy);
        }
    }

    private static float WallDistance(PhysicsScene2D scene, Vector2 origin, Vector2 direction, float distance)
    {
        if (distance <= 0) return 0;
        var hit = scene.Raycast(origin, direction, distance, LayerMask.GetMask("Wall"));
        return hit.collider != null ? hit.distance : distance;
    }

    private void SetFrame(Sprite[] frames, float elapsed, bool repeat)
    {
        int count = Length(frames);
        if (count == 0) return;
        int index = Mathf.FloorToInt(elapsed / FrameTime);
        visual.sprite = frames[repeat ? index % count : Mathf.Min(index, count - 1)];
    }

    private void OnDrawGizmosSelected()
    {
        if (settings == null || visual == null || !Application.isPlaying) return;
        Gizmos.color = new Color(.25f, 1f, .6f, .9f);
        Vector2 center = (Vector2)transform.position + HitboxWorldOffset();
        if (settings.pattern == SkillCastPattern.Channel || settings.pattern == SkillCastPattern.Burst)
        {
            Vector2 side = new Vector2(-direction.y, direction.x) * Radius;
            Vector2 start = center - direction * (Radius * .4f);
            Vector2 end = center + direction * previewReach;
            Gizmos.DrawLine(start + side, end + side);
            Gizmos.DrawLine(start - side, end - side);
            Gizmos.DrawLine(start + side, start - side);
            Gizmos.DrawLine(end + side, end - side);
            return;
        }

        float radius = settings.trailRadius > 0 ? settings.trailRadius : Radius;
        Gizmos.DrawWireSphere(center, radius);
        if (settings.explosionRadius > 0) Gizmos.DrawWireSphere(center, settings.explosionRadius);
    }

    private static int Length(Sprite[] frames) => frames == null ? 0 : frames.Length;
    private static Quaternion Rotation(Vector2 direction) =>
        Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    private static Vector2 Rotate(Vector2 direction, float angle) =>
        Quaternion.Euler(0, 0, angle) * direction;
    private static Vector2 Spread(Vector2 direction, int index, int count, float angle) =>
        count <= 1 ? direction : Rotate(direction, -angle * .5f + angle * index / (count - 1));
}
