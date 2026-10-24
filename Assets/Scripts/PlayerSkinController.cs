using UnityEngine;

public class PlayerSkinController : MonoBehaviour
{
    [Header("Current Skin")]
    [SerializeField] private PlayerSkinData currentSkin;

    [Header("References (Tự động tìm nếu để trống)")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    public PlayerSkinData CurrentSkin => currentSkin;

    private void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if (currentSkin != null)
        {
            ApplySkin(currentSkin);
        }
    }

    public void ApplySkin(PlayerSkinData newSkin)
    {
        if (newSkin == null) return;

        currentSkin = newSkin;

        // Cập nhật Animator Controller nếu skin có bộ animation riêng
        if (animator != null && newSkin.AnimatorController != null)
        {
            animator.runtimeAnimatorController = newSkin.AnimatorController;
        }

        // Cập nhật sprite nếu có
        if (playerSpriteRenderer != null && newSkin.DefaultSprite != null)
        {
            playerSpriteRenderer.sprite = newSkin.DefaultSprite;
        }

        // Cập nhật tốc độ cộng thêm vào PlayerMovement nếu có
        if (playerMovement != null)
        {
            playerMovement.SetSkinSpeedBonus(newSkin.MoveSpeedBonus);
        }
    }
}
