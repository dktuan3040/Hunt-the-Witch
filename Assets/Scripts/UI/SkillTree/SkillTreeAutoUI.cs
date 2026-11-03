using System.Linq;
using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(-100), DisallowMultipleComponent]
public sealed class SkillTreeAutoUI : MonoBehaviour
{
    public SkillTreeCatalog catalog;
    public SkillTreeNodeView nodePrefab;
    public RectTransform nodesRoot;
    public SkillUnlockPanelUI panel;
    public bool IsReady => catalog!=null && nodePrefab!=null && nodesRoot!=null && panel!=null;
    private void OnEnable() { if(IsReady) Rebuild(); }
    [ContextMenu("Rebuild From Data")]
    public void RebuildFromData()
    {
#if UNITY_EDITOR
        if(!Application.isPlaying) { SkillTreeCatalogEditorSync.Synchronize(); return; }
#endif
        Rebuild();
    }
    public void Rebuild()
    {
        if(!IsReady)return;
        SkillData selected=panel.selectedIndex>=0&&panel.selectedIndex<panel.nodes.Count?panel.nodes[panel.selectedIndex].skill:null;
        var existing=nodesRoot.GetComponentsInChildren<SkillTreeNodeView>(true).ToList();
        panel.nodes.Clear();
        if(Application.isPlaying) SkillUnlockProgress.RegisterCatalog(catalog);
        foreach(var entry in catalog.entries)
        {
            if(entry.skill==null)continue;
            var node=existing.FirstOrDefault(n=>n.assetGuid==entry.assetGuid);
            if(node==null)node=Instantiate(nodePrefab,nodesRoot,false);
            existing.Remove(node);node.assetGuid=entry.assetGuid;node.name="Skill_"+entry.skill.SkillId;
            ((RectTransform)node.transform).anchoredPosition=entry.position;
            node.gameObject.SetActive(true);
            var view=node.Bind(entry);panel.nodes.Add(view);
            // Prefab has no persistent callbacks; every generated node binds its own current index.
            int index=panel.nodes.Count-1;node.button.onClick.RemoveAllListeners();
            node.button.onClick.AddListener(()=>panel.SelectNode(index));
        }
        foreach(var node in existing)
        {
            node.gameObject.SetActive(false);
            if(Application.isPlaying)Destroy(node.gameObject);else DestroyImmediate(node.gameObject);
        }
        panel.selectedIndex=selected!=null?panel.nodes.FindIndex(n=>n.skill==selected):-1;
        if(Application.isPlaying)SkillUnlockProgress.ObserveInventory();
        panel.RebuildConnections();panel.RefreshSelection();
    }
}
