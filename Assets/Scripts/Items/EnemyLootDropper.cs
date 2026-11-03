using UnityEngine;

[RequireComponent(typeof(Health)),DisallowMultipleComponent]
public sealed class EnemyLootDropper : MonoBehaviour
{
    public LootTableData lootTable;
    public ItemPickup pickupPrefab;
    [Min(0)] public float scatterRadius=.6f;
    private Health health;
    private bool dropped;
    private void Awake(){health=GetComponent<Health>();health.OnDied+=DropOnce;}
    // Health disables behaviours before OnDied; unsubscribe only on destruction.
    private void OnDestroy(){if(health!=null)health.OnDied-=DropOnce;}
    public void DropOnce()
    {
        if(dropped||health==null||!health.IsDead||health.Team!=Team.Enemy||lootTable==null||pickupPrefab==null)return;
        dropped=true;LootSpawner.Spawn(lootTable,pickupPrefab,transform.position,scatterRadius);
    }
}
