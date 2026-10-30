using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAim : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tự động lật vũ khí khi ngắm sang trái để không bị lộn ngược hoặc lệch trục.")]
    [SerializeField] private bool flipWhenFacingLeft = true;
    [SerializeField] private SpriteRenderer weaponSpriteRenderer;

    private Camera mainCamera;
    private Vector3 initialScale;

    void Awake()
    {
        if (weaponSpriteRenderer == null)
        {
            weaponSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Đảm bảo không lật SpriteRenderer trực tiếp vì sprite vẽ chéo sẽ bị gãy góc
        if (weaponSpriteRenderer != null)
        {
            weaponSpriteRenderer.flipY = false;
        }

        initialScale = transform.localScale;
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        AimWeapon();
    }

    private void AimWeapon()
    {
        if (Mouse.current == null || mainCamera == null)
        {
            return;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        // Hướng từ WeaponPivot tới con trỏ chuột
        Vector2 direction = mouseWorldPosition - transform.position;

        // Tính góc xoay theo chuột (đơn vị: độ)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Xử lý khi ngắm sang nửa bên trái (|angle| > 90 độ)
        if (flipWhenFacingLeft)
        {
            Vector3 scale = initialScale;
            if (Mathf.Abs(angle) > 90f)
            {
                // Lật trục Y của cả cụm WeaponPivot thay vì flipY của SpriteRenderer.
                // Cách này giữ nguyên vị trí tay cầm gậy và bù trừ góc chéo 45 độ hoàn hảo!
                scale.y = -Mathf.Abs(initialScale.y);
            }
            else
            {
                scale.y = Mathf.Abs(initialScale.y);
            }

            transform.localScale = scale;

            // Luôn giữ flipY của SpriteRenderer là false để tránh bị biến dạng
            if (weaponSpriteRenderer != null && weaponSpriteRenderer.flipY)
            {
                weaponSpriteRenderer.flipY = false;
            }
        }
    }
}
