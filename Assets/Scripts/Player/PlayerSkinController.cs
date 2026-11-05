using UnityEngine;

public class PlayerSkinController : MonoBehaviour
{
    [Header("Current Skin")]
    [SerializeField] private PlayerSkinData currentSkin;

    [Header("References (Tự động tìm nếu để trống)")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement playerMovement;

    private Sprite initialSprite;
    private RuntimeAnimatorController initialController;
    private bool initialAnimatorEnabled;

    public PlayerSkinData CurrentSkin => currentSkin;

    public bool IsWearing(PlayerSkinData skin)
    {
        if (skin == null) return false;
        if (currentSkin != null) return currentSkin == skin;

        // Scene-authored players can already wear a matching skin without a data reference.
        bool hasSprite = skin.DefaultSprite != null;
        bool hasController = skin.AnimatorController != null;
        return (hasSprite || hasController)
            && (!hasSprite || initialSprite == skin.DefaultSprite)
            && (!hasController || initialController == skin.AnimatorController);
    }

    private void Awake()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = transform.Find("Visual")?.GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerSpriteRenderer != null) initialSprite = playerSpriteRenderer.sprite;
        if (animator != null)
        {
            initialController = animator.runtimeAnimatorController;
            initialAnimatorEnabled = animator.enabled;
        }

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
        if(Application.isPlaying)GameProfile.SelectSkin(newSkin);

        if (animator != null)
        {
            if (newSkin.AnimatorController != null)
            {
                animator.runtimeAnimatorController = newSkin.AnimatorController;
                animator.enabled = initialAnimatorEnabled;
            }
            else if (newSkin.DefaultSprite != null)
            {
                // A static skin must not be overwritten by the previous skin's animation.
                animator.enabled = false;
            }
            else
            {
                animator.runtimeAnimatorController = initialController;
                animator.enabled = initialAnimatorEnabled;
            }
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
