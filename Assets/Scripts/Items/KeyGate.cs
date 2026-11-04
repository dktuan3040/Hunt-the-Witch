using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class KeyGate : MonoBehaviour
{
    [SerializeField] private ItemData requiredKey;
    public ItemData RequiredKey => requiredKey;

    public bool TryOpen(Component actor)
    {
        if (!isActiveAndEnabled || requiredKey == null || actor == null) return false;
        var health = actor.GetComponentInParent<Health>();
        if (health == null || health.Team != Team.Player || health.IsDead) return false;
        if (PlayerSessionState.GetItemCount(requiredKey.ItemId) < 1) return false;
        gameObject.SetActive(false);
        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryOpen(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TryOpen(collision.collider);
    private void OnTriggerEnter2D(Collider2D other) => TryOpen(other);
    private void OnTriggerStay2D(Collider2D other) => TryOpen(other);
}
