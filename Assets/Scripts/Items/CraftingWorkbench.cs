using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class CraftingWorkbench : MonoBehaviour
{
    [Min(.1f)] public float interactionRange=2.2f;
    public GameObject unlockMenuPrefab;
    private UnlockMenuTabs menu;
    public bool CanInteract(Health actor)
    {
        return !ExpeditionFlow.IsExpeditionActive&&isActiveAndEnabled&&actor!=null&&actor.Team==Team.Player&&!actor.IsDead
            &&actor.gameObject.scene==gameObject.scene&&!PauseMenuUI.InteractionInputBlocked
            &&!SkillScrollSelectionUI.BlocksGameplay&&Time.timeScale>0
            &&Vector2.Distance(actor.transform.position,transform.position)<=interactionRange
            &&(unlockMenuPrefab!=null||FindMenu()!=null);
    }
    private UnlockMenuTabs FindMenu()
    {
        if(menu!=null)return menu;
        foreach(var candidate in FindObjectsByType<UnlockMenuTabs>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(candidate.gameObject.scene==gameObject.scene){menu=candidate;break;}
        return menu;
    }
    public bool TryInteract(Health actor)
    {
        if(!CanInteract(actor))return false;
        FindMenu();
        if(menu==null)
        {
            var root=Instantiate(unlockMenuPrefab);
            SceneManager.MoveGameObjectToScene(root,gameObject.scene);
            menu=root.GetComponentInChildren<UnlockMenuTabs>(true);
            if(menu==null){Destroy(root);return false;}
        }
        if(menu.GetComponent<WorkbenchUnlockMenu>()==null)menu.gameObject.AddComponent<WorkbenchUnlockMenu>();
        if(UnityEngine.EventSystems.EventSystem.current==null)
        {
            var events=new GameObject("WorkbenchEventSystem",typeof(UnityEngine.EventSystems.EventSystem));
            SceneManager.MoveGameObjectToScene(events,gameObject.scene);
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        menu.gameObject.SetActive(true);
        return true;
    }
}
