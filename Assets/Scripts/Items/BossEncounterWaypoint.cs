using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-250)]
[RequireComponent(typeof(BoxCollider2D)),DisallowMultipleComponent]
public sealed class BossEncounterWaypoint : MonoBehaviour
{
    public KeyGate gate;
    public BossHealthBarUI healthBarPrefab;
    [Header("Optional fallback when no boss is placed in the map")]
    [Tooltip("Leave empty to use only bosses dragged into the scene. Never spawns an extra boss when a scene boss exists.")]
    public Health bossPrefab;
    public Transform spawnPoint;
    [Tooltip("Direction from the gate into the boss arena.")]
    public Vector2 entryDirection=Vector2.right;
    public bool HasStarted {get;private set;}
    public Health Boss {get;private set;}
    public BossHealthBarUI HealthBar {get;private set;}
    private readonly System.Collections.Generic.List<Health> dormantBosses=new System.Collections.Generic.List<Health>();
    private void Awake()
    {
        // Park scene-authored bosses before their AI starts. Their Health references
        // remain valid for the portal reveal controllers while they are dormant.
        foreach(var health in FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(health.gameObject.scene!=gameObject.scene||health.Team!=Team.Enemy||!health.IsBoss)continue;
            dormantBosses.Add(health);health.gameObject.SetActive(false);
        }
    }
    private Health player;
    private float nextBossCheck;

    public bool CanBegin(Health actor)
    {
        if(PauseMenuUI.InteractionInputBlocked||Time.timeScale<=0||HasStarted||!isActiveAndEnabled||actor==null||actor.Team!=Team.Player||actor.IsDead||actor.gameObject.scene!=gameObject.scene)return false;
        // Opening the gate already consumed the key; the open gate authorizes this encounter.
        if(gate==null||!gate.IsOpened)return false;
        var center=GetComponent<BoxCollider2D>().bounds.center;
        return Vector2.Dot((Vector2)actor.transform.position-(Vector2)center,entryDirection.normalized)>0;
    }
    public bool TryBegin(Health actor)
    {
        if(!CanBegin(actor)||healthBarPrefab==null)return false;
        HasStarted=true;
        player=actor;
        foreach(var boss in dormantBosses)if(boss!=null)boss.gameObject.SetActive(true);
        // Prefer scene bosses, including inactive ones waiting for a later phase.
        // A placed boss must never cause the fallback prefab to be spawned as well.
        if(!HasSceneBoss()&&bossPrefab!=null&&spawnPoint!=null)
        {
            var spawned=Instantiate(bossPrefab,spawnPoint.position,spawnPoint.rotation);
            SceneManager.MoveGameObjectToScene(spawned.gameObject,gameObject.scene);
        }
        HealthBar=Instantiate(healthBarPrefab);
        SceneManager.MoveGameObjectToScene(HealthBar.gameObject,gameObject.scene);
        RefreshBoss();
        return true;
    }
    private bool HasSceneBoss()
    {
        foreach(var health in FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(health.gameObject.scene==gameObject.scene&&health.Team==Team.Enemy&&health.IsBoss)return true;
        return false;
    }
    private void Update()
    {
        if(!HasStarted||Time.time<nextBossCheck)return;
        nextBossCheck=Time.time+.2f;
        RefreshBoss();
    }
    public void RefreshBoss()
    {
        if(!HasStarted||HealthBar==null)return;
        if(player==null||player.IsDead){Boss=null;HealthBar.Bind(null,"");return;}
        // Keep the current boss until it dies or leaves, so the bar never flickers
        // between two nearby bosses while the player moves.
        if(Boss!=null&&!Boss.IsDead&&Boss.gameObject.activeInHierarchy)return;
        Boss=null;
        float nearest=float.PositiveInfinity;
        foreach(var health in FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if(health.gameObject.scene!=gameObject.scene||health.Team!=Team.Enemy||!health.IsBoss||health.IsDead)continue;
            float distance=(health.transform.position-player.transform.position).sqrMagnitude;
            if(distance<nearest){nearest=distance;Boss=health;}
        }
        string displayName="";
        if(Boss!=null)
        {
            var identity=Boss.GetComponent<EnemyIdentity>();
            displayName=identity!=null&&identity.enemyType!=null&&!string.IsNullOrWhiteSpace(identity.enemyType.displayName)
                ?identity.enemyType.displayName:Boss.name.Replace("(Clone)","").Trim();
        }
        HealthBar.Bind(Boss,displayName);
    }
    private void OnDestroy(){if(HealthBar!=null)Destroy(HealthBar.gameObject);}
    private void OnTriggerEnter2D(Collider2D other){TryBegin(other.GetComponentInParent<Health>());}
    private void OnTriggerStay2D(Collider2D other){TryBegin(other.GetComponentInParent<Health>());}
}
