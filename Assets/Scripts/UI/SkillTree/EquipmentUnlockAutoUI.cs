using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways, DefaultExecutionOrder(-100), DisallowMultipleComponent]
public sealed class EquipmentUnlockAutoUI : MonoBehaviour
{
    public bool skinPage;
    public List<PlayerSkinData> skins=new List<PlayerSkinData>();
    public List<WeaponData> staffs=new List<WeaponData>();
    public SkillUnlockPanelUI panel;
    public SkillTreeNodeView nodePrefab;
    public RectTransform nodesRoot;
    private void OnEnable(){Rebuild();}
    [ContextMenu("Rebuild equipment unlocks")]
    public void Rebuild()
    {
        if(panel==null||nodePrefab==null||nodesRoot==null)return;
        panel.nodes.Clear();
        for(int i=nodesRoot.childCount-1;i>=0;i--)
        {
            var go=nodesRoot.GetChild(i).gameObject;go.SetActive(false);
            if(Application.isPlaying)Destroy(go);else DestroyImmediate(go);
        }
        int count=skinPage?skins.Count:staffs.Count;
        for(int i=0;i<count;i++)
        {
            var skin=skinPage?skins[i]:null;var weapon=skinPage?null:staffs[i];
            if(skin==null&&weapon==null)continue;
            var node=Instantiate(nodePrefab,nodesRoot,false);
            node.name=(skinPage?"Skin_":"Staff_")+(skinPage?skin.name:weapon.name);
            ((RectTransform)node.transform).anchoredPosition=new Vector2(800-(Mathf.Min(count,5)-1)*64+(panel.nodes.Count%5)*128,-500-(panel.nodes.Count/5)*112);
            var view=new SkillUnlockPanelUI.NodeView {
                id=node.name,title=skinPage?skin.SkinName:weapon.WeaponName,
                skin=skin,weapon=weapon,stateInitialized=true,state=SkillKnowledge.Discovered,
                button=node.button,fill=node.fill,frame=node.frame,icon=node.icon,selected=node.selected,learnedBadge=node.learnedBadge
            };
            int index=panel.nodes.Count;panel.nodes.Add(view);
            node.button.onClick.RemoveAllListeners();node.button.onClick.AddListener(()=>panel.SelectNode(index));
            node.gameObject.SetActive(true);
        }
        panel.selectedIndex=-1;panel.RebuildConnections();panel.RefreshSelection();
    }
}
