using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SceneDoor : MonoBehaviour
{
    [SerializeField] private string targetScenePath="Assets/Scenes/Maps/Trong Nhà.unity";
    [Min(.1f)] public float interactionRange=1.6f;
    public Vector2 interactionOffset=new Vector2(0,-1.25f);
    public Vector3 InteractionPosition => transform.TransformPoint(interactionOffset);
    public bool IsTransitioning {get;private set;}
    public bool CanInteract(Health actor)
    {
        return !IsTransitioning&&isActiveAndEnabled&&actor!=null&&actor.Team==Team.Player&&!actor.IsDead
            &&actor.gameObject.scene==gameObject.scene&&!PauseMenuUI.InteractionInputBlocked
            &&!SkillScrollSelectionUI.BlocksGameplay&&Time.timeScale>0
            &&Vector2.Distance(actor.transform.position,InteractionPosition)<=interactionRange
            &&!string.IsNullOrWhiteSpace(targetScenePath)&&Application.CanStreamedLevelBeLoaded(targetScenePath);
    }
    public bool TryInteract(Health actor)
    {
        if(!CanInteract(actor))return false;
        if(!ExpeditionFlow.Travel(targetScenePath,actor))return false;
        IsTransitioning=true;
        return true;
    }
}
