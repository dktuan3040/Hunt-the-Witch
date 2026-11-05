using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class KeyGate : MonoBehaviour
{
    [SerializeField] private ItemData requiredKey;
    public ItemData RequiredKey => requiredKey;
    public bool IsOpened { get; private set; }

    public bool TryOpen(Component actor)
    {
        if (IsOpened || !isActiveAndEnabled || requiredKey == null || actor == null) return false;
        var health = actor.GetComponentInParent<Health>();
        if (health == null || health.Team != Team.Player || health.IsDead || health.gameObject.scene != gameObject.scene) return false;
        if (!PlayerSessionState.TryRemoveItem(requiredKey.ItemId, 1)) return false;
        IsOpened = true;
        gameObject.SetActive(false);
        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision) => TryOpen(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => TryOpen(collision.collider);
    private void OnTriggerEnter2D(Collider2D other) => TryOpen(other);
    private void OnTriggerStay2D(Collider2D other) => TryOpen(other);
}
