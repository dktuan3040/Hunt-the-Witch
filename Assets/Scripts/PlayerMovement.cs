using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerInput playerInput;

    private float skinSpeedBonus = 0f;

    public float CurrentMoveSpeed => moveSpeed + skinSpeedBonus;

    public void SetSkinSpeedBonus(float bonus)
    {
        skinSpeedBonus = bonus;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        // Tự động liên kết sự kiện từ PlayerInput, tránh bị lỗi quên kéo trong Unity Inspector
        if (playerInput != null && playerInput.actions != null)
        {
            var moveAction = playerInput.actions["Move"];
            if (moveAction != null)
            {
                moveAction.performed += OnMoveInput;
                moveAction.canceled += OnMoveInput;
            }
        }
    }

    private void OnDisable()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            var moveAction = playerInput.actions["Move"];
            if (moveAction != null)
            {
                moveAction.performed -= OnMoveInput;
                moveAction.canceled -= OnMoveInput;
            }
        }
    }

    private void OnMoveInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // Giữ hàm Move để tương thích ngược nếu cấu hình qua UnityEvent trong Inspector
    public void Move(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Update()
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", moveInput != Vector2.zero);
        }
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveInput * CurrentMoveSpeed;
        }
    }
}
