using UnityEngine;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class EnemySkillStatus : MonoBehaviour
{
    private Health health;
    private Rigidbody2D body;
    private HitFeedback feedback;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private float freezeUntil, stunUntil, slowUntil, wetUntil, thawUntil, burnUntil, nextBurn;
    private float slowFactor = 1, wetFactor = 1, thawFactor = 1, burnInterval;
    private int burnDamage;
    private float pushUntil, pullUntil, pushDuration;
    private Vector2 pushVelocity, pullVelocity;
    private int tint;

    public bool Immobilized => Time.time < Mathf.Max(freezeUntil, stunUntil);
    public float MovementMultiplier
    {
        get
        {
            float now = Time.time;
            return Mathf.Min(now < slowUntil ? slowFactor : 1,
                Mathf.Min(now < wetUntil ? wetFactor : 1,
                    now >= freezeUntil && now < thawUntil ? thawFactor : 1));
        }
    }

    public static EnemySkillStatus For(Health target)
    {
        return target.TryGetComponent<EnemySkillStatus>(out var existing)
            ? existing : target.gameObject.AddComponent<EnemySkillStatus>();
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Rigidbody2D>();
        feedback = GetComponent<HitFeedback>();
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
    }

    public void Apply(SkillCombatSettings settings, float multiplier)
    {
        float now = Time.time;
        float until = now + Mathf.Max(0, settings.statusDuration);
        float factor = Mathf.Clamp01(settings.slowMultiplier);
        var flags = settings.status;
        if ((flags & SkillStatus.Stun) != 0) stunUntil = Mathf.Max(stunUntil, until);
        if ((flags & SkillStatus.Freeze) != 0)
        {
            freezeUntil = Mathf.Max(freezeUntil, until);
            thawUntil = Mathf.Max(thawUntil, freezeUntil + settings.thawSlowDuration);
            thawFactor = factor;
        }
        if ((flags & SkillStatus.Slow) != 0)
        {
            slowFactor = now < slowUntil ? Mathf.Min(slowFactor, factor) : factor;
            slowUntil = Mathf.Max(slowUntil, until);
        }
        if ((flags & SkillStatus.Wet) != 0)
        {
            wetFactor = now < wetUntil ? Mathf.Min(wetFactor, factor) : factor;
            wetUntil = Mathf.Max(wetUntil, until);
        }
        if ((flags & SkillStatus.Burn) != 0)
        {
            if (now >= burnUntil)
            {
                burnInterval = Mathf.Max(.05f, settings.burnInterval);
                nextBurn = now + burnInterval;
                burnDamage = 0;
            }
            burnDamage = Mathf.Max(burnDamage, Mathf.RoundToInt(settings.burnDamage * multiplier));
            burnUntil = Mathf.Max(burnUntil, until);
        }
    }

    public void Push(Vector2 direction, float distance, float duration)
    {
        pushDuration = Mathf.Max(.01f, duration);
        pushVelocity = direction.normalized * (2 * distance / pushDuration);
        pushUntil = Time.time + pushDuration;
    }

    public void Pull(Vector2 center, float speed)
    {
        var offset = center - (Vector2)transform.position;
        pullVelocity = Vector2.ClampMagnitude(offset / .1f, speed);
        pullUntil = Time.time + .12f;
    }

    private void FixedUpdate()
    {
        if (health.IsDead || body == null) return;
        if (Immobilized) body.linearVelocity = Vector2.zero;
        else if (Time.time < pushUntil)
            body.linearVelocity = pushVelocity * ((pushUntil - Time.time) / pushDuration);
        else if (Time.time < pullUntil) body.linearVelocity = pullVelocity;
    }

    private void Update()
    {
        if (health.IsDead) return;
        float now = Time.time;
        if (now >= nextBurn && nextBurn < burnUntil && burnDamage > 0)
        {
            nextBurn += burnInterval;
            health.TakeDamage(burnDamage);
        }
        if (health.IsDead) return;
        SetTint(now < freezeUntil ? 1 : now < wetUntil ? 2 : now < burnUntil ? 3 :
            now < slowUntil || now < thawUntil ? 1 : 0);
    }

    private void OnDisable()
    {
        SetTint(0);
        if (health != null && !health.IsDead && body != null &&
            (Immobilized || Time.time < pushUntil || Time.time < pullUntil))
            body.linearVelocity = Vector2.zero;
    }

    private void SetTint(int next)
    {
        if (next == tint) return;
        if (feedback != null) feedback.ClearFlash();
        var color = next == 1 ? new Color(.46f,.72f,1) :
            next == 2 ? new Color(.25f,.46f,.86f) : new Color(1,.38f,.34f);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null) continue;
            if (tint == 0) baseColors[i] = renderer.color;
            renderer.color = next == 0 ? baseColors[i] : baseColors[i] * color;
        }
        tint = next;
    }
}
