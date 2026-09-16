using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFacing : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerSprite;
    private Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = Camera.main;
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateFacing();
    }

    private void UpdateFacing()
    {
        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);

        if(mouseWorldPosition.x > transform.position.x)
        {
            playerSprite.flipX = false;
        }
        else if(mouseWorldPosition.x < transform.position.x)
        {
            playerSprite.flipX = true;
        }
    }
}
