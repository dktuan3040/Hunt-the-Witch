using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifeTime = 3f;
    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction.normalized;
        // Di chuyển viên đạn
        rb.linearVelocity = moveDirection * speed;
        // xoay projectile theo hướng
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        float spriteOffset = -90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + spriteOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Player"))
        {
            return;
        }
        // chạm quái, tường, vật cản thì hủy
        if(collision.CompareTag("Enemy") || collision.CompareTag("Wall") || collision.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
