using UnityEngine;

[CreateAssetMenu(fileName = "NewSkinData", menuName = "Hunt the Witch/Character/Skin Data")]
public class PlayerSkinData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skinName = "Apprentice Witch";
    [TextArea(2, 4)]
    [SerializeField] private string description = "The standard apprentice robes.";
    [SerializeField] private Sprite skinIcon;

    [Header("Visuals & Animation")]
    [Tooltip("Animator Controller chứa các clip Idle, Walk,... riêng của skin này.")]
    [SerializeField] private RuntimeAnimatorController animatorController;
    [Tooltip("Sprite mặc định nếu không sử dụng Animator.")]
    [SerializeField] private Sprite defaultSprite;

    [Header("Stats Modifiers")]
    [Tooltip("Tốc độ cộng thêm vào tốc độ cơ bản của nhân vật.")]
    [SerializeField] private float moveSpeedBonus = 0f;

    // Public Getters
    public string SkinName => skinName;
    public string Description => description;
    public Sprite SkinIcon => skinIcon;
    public RuntimeAnimatorController AnimatorController => animatorController;
    public Sprite DefaultSprite => defaultSprite;
    public float MoveSpeedBonus => moveSpeedBonus;
}
