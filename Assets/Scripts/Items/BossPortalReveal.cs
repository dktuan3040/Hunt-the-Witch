using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossPortalReveal : MonoBehaviour
{
    [SerializeField] private Health boss;
    [SerializeField] private GameObject portalVisual;
    public bool IsRevealed {get;private set;}
    public Health Boss => boss;
    private void Awake(){SetVisual(false);}
    private Health FindBoss()
    {
        Health closest=null;float distance=float.PositiveInfinity;
        foreach(var health in FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(health.gameObject.scene!=gameObject.scene||health.Team!=Team.Enemy||!health.IsBoss)continue;
            float d=(health.transform.position-transform.position).sqrMagnitude;
            if(d<distance){distance=d;closest=health;}
        }
        return closest;
    }
    public void BindBoss(Health target)
    {
        if(boss!=null)boss.OnDied-=Reveal;
        boss=target;
        if(boss==null||!isActiveAndEnabled)return;
        boss.OnDied+=Reveal;
        if(boss.IsDead)Reveal();
    }
    private void OnEnable(){BindBoss(boss!=null?boss:FindBoss());}
    private void LateUpdate()
    {
        if(IsRevealed)return;
        if(boss==null)BindBoss(FindBoss());
        else if(boss.IsDead)Reveal();
    }
    private void OnDisable(){if(boss!=null)boss.OnDied-=Reveal;}
    private void SetVisual(bool show){if(portalVisual!=null&&portalVisual!=gameObject)portalVisual.SetActive(show);}
    private void Reveal(){IsRevealed=true;SetVisual(true);}
}
