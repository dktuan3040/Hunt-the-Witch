using System.Numerics;
using UnityEngine;
using UnityEngine.Rendering;

public class SpreadFireball : MonoBehaviour
{
    [Header("Skill setting")]
    public GameObject fireBallPrefab;
    public int numberOfFireBall = 12;
    public float fireBallspeed = 5f;
    
    public float damage = 1f;

    public void CastRadialFireBalls()
    {
        float angleStep = 360f / numberOfFireBall;
        float currentAngle = 0f;

        Projectile projectile = fireBallPrefab.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.SetTeam(Team.Enemy);
        }

        Collider2D tenguCollider = GetComponent<Collider2D>();
        Collider2D fireBallCollider = fireBallPrefab.GetComponent<Collider2D>();
        if (tenguCollider != null && fireBallCollider != null)
        {
            Physics2D.IgnoreCollision(fireBallCollider, tenguCollider);
        }

        for (int i = 0; i < numberOfFireBall; i++)
        {
            UnityEngine.Quaternion rotation = UnityEngine.Quaternion.Euler(0, 0, currentAngle);
            UnityEngine.Vector3 dir = rotation * UnityEngine.Vector3.right;
            UnityEngine.Vector3 spawnPos = transform.position - dir * 0.4f; // Đẩy nhẹ ra ngoài rìa theo hướng bắn

            GameObject fireBall = Instantiate(fireBallPrefab, spawnPos, rotation);

            Projectile proj = fireBall.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetTeam(Team.Enemy); // Phe quái để gây dame lên Player
                // Khởi tạo đạn: hướng bay, hệ số dame, data, tốc độ, thời gian tồn tại (3s), sát thương
                proj.Initialize(dir, 1f, null, fireBallspeed, 3f, Mathf.RoundToInt(damage));
            }

            currentAngle += angleStep;
        }

    }
}
