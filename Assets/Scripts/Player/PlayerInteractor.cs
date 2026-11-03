using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerInput),typeof(Health)),DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private string interactAction="Player/Interact";
    private PlayerInput input;
    private Health health;
    private void Awake(){input=GetComponent<PlayerInput>();health=GetComponent<Health>();}
    private void Update()
    {
        if(input==null||input.actions==null)return;
        var action=input.actions.FindAction(interactAction,false);
        if(action!=null&&action.enabled&&action.WasPressedThisFrame())TryInteract();
    }
    public bool TryInteract()
    {
        if(!isActiveAndEnabled||health==null||health.Team!=Team.Player||health.IsDead||PauseMenuUI.CombatInputBlocked||Time.timeScale<=0)return false;
        RewardChest nearest=null;SkillScrollPickup nearestScroll=null;float distance=float.PositiveInfinity;
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
        return nearestScroll!=null?nearestScroll.TryTake(skills):nearest!=null&&nearest.TryOpen(health);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install(){SceneManager.sceneLoaded-=AddToPlayers;SceneManager.sceneLoaded+=AddToPlayers;}
    private static void AddToPlayers(Scene scene,LoadSceneMode mode)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var player in root.GetComponentsInChildren<PlayerInput>(true))
        {var h=player.GetComponent<Health>();if(h!=null&&h.Team==Team.Player&&player.GetComponent<PlayerInteractor>()==null)player.gameObject.AddComponent<PlayerInteractor>();}
    }
}
