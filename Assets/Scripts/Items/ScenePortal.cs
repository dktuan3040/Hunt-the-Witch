using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class ScenePortal : MonoBehaviour
{
    [SerializeField] private string destinationScene = "M_F1";
    private ExpeditionPortal portal;
    private bool ownsPortal;

    private void Reset()
    {
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.1f, 2.3f);
    }

    private void Awake() => portal = ResolvePortal();

    private ExpeditionPortal ResolvePortal()
    {
        // Older scenes may put this component on the visual of an existing portal.
        // Keep the parent's destination and boss gate instead of creating a second exit.
        var existing = GetComponentInParent<ExpeditionPortal>();
        if (existing != null)
        {
            if (existing.gameObject != gameObject) GetComponent<BoxCollider2D>().enabled = false;
            return existing;
        }
        var created = gameObject.AddComponent<ExpeditionPortal>();
        ownsPortal = true;
        created.destination = destinationScene;
        created.bossGate = GetComponentInParent<BossPortalReveal>();
        created.requireBossDefeated = created.bossGate != null;
        created.RefreshTrigger();
        return created;
    }

    private void OnEnable() { if (ownsPortal && portal != null) portal.enabled = true; }
    private void OnDisable() { if (ownsPortal && portal != null) portal.enabled = false; }

    public bool TryTravel(Component actor)
    {
        if (!isActiveAndEnabled || actor == null) return false;
        if (portal == null) portal = ResolvePortal();
        return portal.TryEnter(actor);
    }
}
