using UnityEngine;

[RequireComponent(typeof(Health)),DisallowMultipleComponent]
public sealed class EnemyIdentity : MonoBehaviour
{
    public EnemyTypeData enemyType;
    private Health health;
    private void Awake(){health=GetComponent<Health>();health.OnDied+=RecordDeath;}
    // Health disables behaviours before publishing OnDied.
    private void OnDestroy(){if(health!=null)health.OnDied-=RecordDeath;}
    private void RecordDeath(){MapKillProgress.RecordKill(health,enemyType);}
}
