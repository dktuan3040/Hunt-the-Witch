using UnityEngine;
using UnityEngine.UI;

public sealed class SkillTreeNodeView : MonoBehaviour
{
    public string assetGuid;
    public Button button;
    public Image fill, frame, icon;
    public GameObject selected, learnedBadge;
    public SkillUnlockPanelUI.NodeView Bind(SkillTreeCatalog.Entry entry)
    {
        return new SkillUnlockPanelUI.NodeView {
            id=entry.assetGuid, skill=entry.skill, title=entry.skill.SkillName,
            description=entry.skill.Description, recipe=entry.recipe, invalidData=entry.invalid,
            stateInitialized=true, state=SkillKnowledge.Unknown,
            button=button,fill=fill,frame=frame,icon=icon,selected=selected,learnedBadge=learnedBadge
        };
    }
}
