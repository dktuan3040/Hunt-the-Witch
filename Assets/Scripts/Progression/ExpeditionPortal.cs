using UnityEngine;

[RequireComponent(typeof(BoxCollider2D)),DisallowMultipleComponent]
public sealed class ExpeditionPortal : MonoBehaviour
{
    public string destination=ExpeditionFlow.FirstMap;
    public BossPortalReveal bossGate;
    public bool requireBossDefeated;
    [Tooltip("Fit the trigger to the visible portal, including offset/scaled visual children.")]
    public bool fitTriggerToVisual=true;
    [Range(.25f,1f)] public float triggerCoverage=.7f;
    [Min(.1f)] public float interactionRange=2.5f;
    private BoxCollider2D trigger;
    private SpriteRenderer[] visuals;
    public Vector3 InteractionPosition {get{Cache();return transform.TransformPoint(trigger.offset);}}
    private void Awake(){Cache();RefreshTrigger();}
    private void OnEnable(){Cache();RefreshTrigger();}
    private void Cache()
    {
        if(trigger==null)trigger=GetComponent<BoxCollider2D>();
        if(visuals==null)visuals=GetComponentsInChildren<SpriteRenderer>(true);
    }
    public void RefreshTrigger()
    {
        Cache();trigger.isTrigger=true;
        if(fitTriggerToVisual)
        {
            bool found=false;Vector2 minimum=Vector2.zero,maximum=Vector2.zero;
            foreach(var visual in visuals)
            {
                if(visual==null||visual.sprite==null||!visual.enabled)continue;
                // localBounds is available even while the boss-gated visual is hidden.
                var bounds=visual.localBounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)
                {
                    var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,0));
                    Vector2 local=transform.InverseTransformPoint(visual.transform.TransformPoint(point));
                    if(!found){minimum=maximum=local;found=true;}else{minimum=Vector2.Min(minimum,local);maximum=Vector2.Max(maximum,local);}
                }
            }
            if(found)
            {
                trigger.offset=(minimum+maximum)*.5f;
                var size=(maximum-minimum)*Mathf.Clamp(triggerCoverage,.25f,1f);
                trigger.size=new Vector2(Mathf.Max(.4f,size.x),Mathf.Max(.4f,size.y));
            }
        }
        trigger.enabled=isActiveAndEnabled&&(!requireBossDefeated||(bossGate!=null&&bossGate.IsRevealed));
    }
    private void LateUpdate(){RefreshTrigger();}
    public bool CanEnter(Health actor)
    {
        return !ExpeditionFlow.IsLoading&&isActiveAndEnabled&&actor!=null&&actor.Team==Team.Player&&!actor.IsDead
            &&actor.gameObject.scene==gameObject.scene&&!PauseMenuUI.InteractionInputBlocked
            &&!SkillScrollSelectionUI.BlocksGameplay&&Time.timeScale>0
            &&Vector2.Distance(actor.transform.position,InteractionPosition)<=interactionRange
            &&(!requireBossDefeated||(bossGate!=null&&bossGate.IsRevealed));
    }
    public bool TryEnter(Component actor)
    {
        var health=actor!=null?actor.GetComponentInParent<Health>():null;
        return CanEnter(health)&&ExpeditionFlow.Travel(destination,health);
    }
    private void OnDisable(){var outline=GetComponent<InteractionOutline>();if(outline!=null)outline.SetVisible(false);}
    private void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
}
