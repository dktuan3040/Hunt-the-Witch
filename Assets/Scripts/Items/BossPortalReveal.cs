using UnityEngine;

// Keep this controller on the active portal root; only its visual child is hidden.
[DisallowMultipleComponent]
public sealed class BossPortalReveal : MonoBehaviour
{
    [SerializeField] private Health boss;
    [SerializeField] private GameObject portalVisual;
    public bool IsRevealed { get; private set; }

    private void Awake()
    {
        if (portalVisual != null && portalVisual != gameObject)
            portalVisual.SetActive(false);
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnDied += Reveal;
        if (boss.IsDead) Reveal();
    }

    private void OnDisable()
    {
        if (boss != null) boss.OnDied -= Reveal;
    }

    private void Reveal()
    {
        IsRevealed = true;
        if (portalVisual != null && portalVisual != gameObject)
            portalVisual.SetActive(true);
    }
}
