using System;
using UnityEngine;

public enum SkillCastPattern
{
    Legacy, Burst, Channel, Projectile, PullTrap, Nearest, WallLines, Arc, Vortex, Area
}

[Flags]
public enum SkillStatus
{
    None = 0, Slow = 1, Wet = 2, Burn = 4, Stun = 8, Freeze = 16
}

[Serializable]
public sealed class SkillCombatSettings
{
    public SkillCastPattern pattern;
    [Tooltip("Active time, or flight time for a moving spell. Startup and ending play separately.")]
    [Min(.05f)] public float duration = 1;
    [Min(.05f)] public float tickInterval = .4f;
    [Min(0)] public int damage = 12;
    [Min(.05f)] public float radius = 1;
    [Min(.05f)] public float range = 5;
    [Min(.05f)] public float speed = 8;
    [Min(1)] public int targetCount = 1;
    public bool repeatTargets;
    public bool pierce;
    public bool hitDuringEnding;
    public bool damageOverTime;
    public bool radial;
    public bool followCaster;
    [Min(0)] public float knockbackDistance;
    [Min(.01f)] public float knockbackDuration = .18f;
    [Min(0)] public float pullSpeed;
    [Min(0)] public int trailDamage;
    [Min(0)] public float trailRadius;
    [Min(0)] public float explosionRadius;
    [Min(0)] public int explosionDamage;
    public bool explodeOnExpiry;
    [Min(.05f)] public float wallSpacing = 1;
    [Min(0)] public float wallDelay = .13f;
    [Min(0)] public float arcHeight = 1.6f;
    [Min(0)] public float vortexStartRadius = .45f;
    public float vortexDegreesPerSecond = 140;
    public SkillStatus status;
    [Min(0)] public float statusDuration = 2;
    [Range(0,1)] public float slowMultiplier = .55f;
    [Min(0)] public float thawSlowDuration = 2;
    [Min(0)] public int burnDamage = 2;
    [Min(.05f)] public float burnInterval = 1;
    public SkillData onHitSkill;
}
