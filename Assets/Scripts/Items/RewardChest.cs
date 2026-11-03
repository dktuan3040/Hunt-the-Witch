using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class RewardChest : MonoBehaviour
{
    public LootTableData lootTable;
    public ItemPickup pickupPrefab;
    [Min(.1f)] public float interactionRange=1.5f;
    public Sprite openedSprite;
    public UnityEvent onOpened=new UnityEvent();
    public bool IsOpened {get;private set;}
    public bool IsAvailable {get;private set;}=true;
    public bool CanInteract => isActiveAndEnabled&&IsAvailable&&!IsOpened&&lootTable!=null&&pickupPrefab!=null;
    public void SetAvailable(bool available){IsAvailable=available;}
    public bool TryOpen(Health actor)
    {
        if(!CanInteract||actor==null||actor.Team!=Team.Player||actor.IsDead||actor.gameObject.scene!=gameObject.scene||PauseMenuUI.CombatInputBlocked||Time.timeScale<=0||Vector2.Distance(actor.transform.position,transform.position)>interactionRange)return false;
        IsOpened=true;LootSpawner.Spawn(lootTable,pickupPrefab,transform.position);
        var visual=GetComponentInChildren<SpriteRenderer>();if(visual!=null){if(openedSprite!=null)visual.sprite=openedSprite;else visual.color=Color.gray;}
        onOpened.Invoke();
        return true;
    }
}
