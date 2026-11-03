using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class RewardChest : MonoBehaviour
{
    public LootTableData lootTable;
    public ItemPickup pickupPrefab;
    [Min(.1f)] public float interactionRange=1.5f;
    [Tooltip("Minimum gap between the chest collider and the center of dropped loot.")]
    [Min(.25f)] public float lootClearance=.45f;
    [Min(0f)] public float lootScatterRadius=.45f;
    public Sprite openedSprite;
    public UnityEvent onOpened=new UnityEvent();
    public bool IsOpened {get;private set;}
    public bool IsAvailable {get;private set;}=true;
    public bool CanInteract => isActiveAndEnabled&&IsAvailable&&!IsOpened&&lootTable!=null&&pickupPrefab!=null;
    public void SetAvailable(bool available){IsAvailable=available;}
    public bool TryOpen(Health actor)
    {
        if(!CanInteract||actor==null||actor.Team!=Team.Player||actor.IsDead||actor.gameObject.scene!=gameObject.scene||PauseMenuUI.CombatInputBlocked||Time.timeScale<=0||Vector2.Distance(actor.transform.position,transform.position)>interactionRange)return false;
        IsOpened=true;LootSpawner.Spawn(lootTable,pickupPrefab,GetLootPosition(actor.transform.position),lootScatterRadius);
        var visual=GetComponentInChildren<SpriteRenderer>();if(visual!=null){if(openedSprite!=null)visual.sprite=openedSprite;else visual.color=Color.gray;}
        onOpened.Invoke();
        return true;
    }

    private Vector3 GetLootPosition(Vector3 actorPosition)
    {
        var bounds=new Bounds(transform.position,Vector3.zero);
        bool hasBounds=false;
        foreach(var collider in GetComponentsInChildren<Collider2D>())
        {
            if(!collider.enabled)continue;
            if(!hasBounds){bounds=collider.bounds;hasBounds=true;}
            else bounds.Encapsulate(collider.bounds);
        }
        if(!hasBounds)
        {
            var visual=GetComponentInChildren<SpriteRenderer>();
            if(visual!=null)bounds=visual.bounds;
        }
        Vector2 direction=(Vector2)(actorPosition-bounds.center);
        direction=direction.sqrMagnitude>.001f?direction.normalized:Vector2.down;
        // Reserve the full scatter radius, including the scroll's fixed .5-unit scatter.
        float extent=Mathf.Abs(direction.x)*bounds.extents.x+Mathf.Abs(direction.y)*bounds.extents.y;
        float distance=extent+Mathf.Max(.5f,lootScatterRadius)+Mathf.Max(.25f,lootClearance);
        Vector3 position=bounds.center+(Vector3)(direction*distance);
        position.z=transform.position.z;
        return position;
    }
}
