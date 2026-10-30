using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFacing : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSprite;
    private Camera mainCamera;

    void Awake()
    {
        if (playerSprite == null)
        {
            playerSprite = GetComponentInChildren<SpriteRenderer>();
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (Mouse.current == null || mainCamera == null || playerSprite == null)
        {
            return;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        if (mouseWorldPosition.x > transform.position.x)
        {
            playerSprite.flipX = false;
        }
        else if (mouseWorldPosition.x < transform.position.x)
        {
            playerSprite.flipX = true;
        }
    }
}
