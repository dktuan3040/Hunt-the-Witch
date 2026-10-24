using UnityEngine;

/// <summary>Phe của đối tượng. Đạn và đòn đánh chỉ gây sát thương cho phe khác.</summary>
public enum Team
{
    Player,
    Enemy,
    Neutral // thùng vỡ, vật thể phá hủy được...
}

public interface IDamageable
{
    Team Team { get; }
    bool IsDead { get; }

    void TakeDamage(int damage);

    /// <summary>hitDirection: hướng đòn đánh (từ người đánh về phía nạn nhân), dùng cho knockback.</summary>
    void TakeDamage(int damage, Vector2 hitDirection);
}
