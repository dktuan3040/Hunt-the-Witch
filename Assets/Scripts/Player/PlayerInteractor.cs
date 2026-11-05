using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerInput),typeof(Health)),DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private string interactAction="Player/Interact";
    private PlayerInput input;
    private Health health;
    private InteractionOutline highlighted;
    private void Awake(){input=GetComponent<PlayerInput>();health=GetComponent<Health>();}
    private void Update()
    {
        var action=input!=null&&input.actions!=null?input.actions.FindAction(interactAction,false):null;
        if(action==null||!action.enabled){Highlight(null);return;}
        RewardChest chest;SkillScrollPickup scroll;CraftingWorkbench workbench;SceneDoor door;ExpeditionPortal portal;
        FindTarget(out chest,out scroll,out workbench,out door,out portal);
        Highlight(portal!=null?portal.gameObject:door!=null?door.gameObject:workbench!=null?workbench.gameObject:scroll!=null?scroll.gameObject:chest!=null?chest.gameObject:null);
        if(action.WasPressedThisFrame())TryInteract();
    }
    private void Highlight(GameObject target)
    {
        if(highlighted!=null&&target==highlighted.gameObject)
        {
            if(!highlighted.enabled)highlighted.enabled=true;
            if(!highlighted.IsVisible)highlighted.SetVisible(true);
            return;
        }
        if(highlighted!=null)highlighted.SetVisible(false);
        highlighted=null;
        if(target==null)return;
        highlighted=target.GetComponent<InteractionOutline>();
        if(highlighted==null)highlighted=target.AddComponent<InteractionOutline>();
        highlighted.SetVisible(true);
    }
    private void OnDisable(){Highlight(null);}
    private void FindTarget(out RewardChest nearest,out SkillScrollPickup nearestScroll,out CraftingWorkbench nearestWorkbench,out SceneDoor nearestDoor,out ExpeditionPortal nearestPortal)
    {
        nearest=null;nearestScroll=null;nearestWorkbench=null;nearestDoor=null;nearestPortal=null;
        if(!isActiveAndEnabled||health==null||health.Team!=Team.Player||health.IsDead||PauseMenuUI.InteractionInputBlocked||SkillScrollSelectionUI.BlocksGameplay||Time.timeScale<=0)return;
        float distance=float.PositiveInfinity;
        foreach(var chest in FindObjectsByType<RewardChest>())
        {
            if(chest.gameObject.scene!=gameObject.scene||!chest.CanInteract)continue;
            float d=Vector2.Distance(transform.position,chest.transform.position);
            if(d<=chest.interactionRange&&d<distance){nearest=chest;distance=d;}
        }
        var skills=GetComponent<PlayerSkillManager>();
        if(skills!=null)foreach(var scroll in FindObjectsByType<SkillScrollPickup>())
        {
            if(!scroll.CanInteract(skills))continue;
            float d=Vector2.Distance(transform.position,scroll.transform.position);
            if(d<distance){nearestScroll=scroll;nearest=null;distance=d;}
        }
        foreach(var workbench in FindObjectsByType<CraftingWorkbench>())
        {
            if(!workbench.CanInteract(health))continue;
            float d=Vector2.Distance(transform.position,workbench.transform.position);
            if(d<distance){nearestWorkbench=workbench;nearest=null;nearestScroll=null;distance=d;}
        }
        foreach(var door in FindObjectsByType<SceneDoor>())
        {
            if(!door.CanInteract(health))continue;
            float d=Vector2.Distance(transform.position,door.InteractionPosition);
            if(d<distance){nearestDoor=door;nearestWorkbench=null;nearest=null;nearestScroll=null;distance=d;}
        }
        foreach(var portal in FindObjectsByType<ExpeditionPortal>())
        {
            if(!portal.CanEnter(health))continue;
            float d=Vector2.Distance(transform.position,portal.InteractionPosition);
            if(d<distance){nearestPortal=portal;nearestDoor=null;nearestWorkbench=null;nearest=null;nearestScroll=null;distance=d;}
        }
    }
    public bool TryInteract()
    {
        RewardChest chest;SkillScrollPickup scroll;CraftingWorkbench workbench;SceneDoor door;ExpeditionPortal portal;
        FindTarget(out chest,out scroll,out workbench,out door,out portal);
        var skills=GetComponent<PlayerSkillManager>();
        bool interacted=portal!=null?portal.TryEnter(health):door!=null?door.TryInteract(health):workbench!=null?workbench.TryInteract(health):scroll!=null?scroll.TryTake(skills):chest!=null&&chest.TryOpen(health);
        Highlight(null);
        return interacted;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install(){SceneManager.sceneLoaded-=AddToPlayers;SceneManager.sceneLoaded+=AddToPlayers;}
    private static void AddToPlayers(Scene scene,LoadSceneMode mode)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var player in root.GetComponentsInChildren<PlayerInput>(true))
        {var h=player.GetComponent<Health>();if(h!=null&&h.Team==Team.Player&&player.GetComponent<PlayerInteractor>()==null)player.gameObject.AddComponent<PlayerInteractor>();}
    }
}
