using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class SkillUnlockPanelUI : MonoBehaviour
{
    [Serializable] public sealed class NodeView
    {
        public string id, title;
        [TextArea] public string description;
        public SkillData skill;
        public WeaponData weapon;
        public PlayerSkinData skin;
        public bool IsEquipment => weapon!=null||skin!=null;
        public Sprite DisplayIcon => weapon!=null?(weapon.WeaponIcon!=null?weapon.WeaponIcon:weapon.WeaponSprite):skin!=null?(skin.SkinIcon!=null?skin.SkinIcon:skin.DefaultSprite):skill!=null?skill.SkillIcon:null;
        public SkillSynergyData recipe;
        public SkillKnowledge state;
        public bool invalidData;
        [HideInInspector] public bool unknown, unlocked, stateInitialized;
        public Button button;
        public Image fill, frame, icon;
        public GameObject selected, learnedBadge;
        public TMP_Text label;
    }
    [Serializable] public sealed class RequirementView
    {
        public GameObject root;
        public Image background, icon, status;
        public TMP_Text label, value;
    }
    [Serializable] public sealed class SkillUnlockEvent : UnityEvent<SkillData> { }
    private struct RequirementLine
    {
        public string label, value; public bool met; public Sprite icon;
        public RequirementLine(string l,string v,bool m,Sprite i) {label=l;value=v;met=m;icon=i;}
    }

    [Header("Layout")]
    public RectTransform designRoot, graphRoot, connectionsRoot;
    public Vector2 designSize = new Vector2(960,540);
    public GameObject panelRoot, detailsPanel;
    public SkillTreeNavigation treeNavigation;
    public List<NodeView> nodes = new List<NodeView>();
    public int selectedIndex = -1;
    [Header("Details")]
    public TMP_Text detailTitle, detailCategory, detailDescription, detailHint, requirementsTitle, unlockButtonLabel;
    public Image detailIcon;
    public Button unlockButton;
    public ScrollRect requirementsScroll;
    public RectTransform requirementContent;
    public GameObject requirementTemplate;
    public List<RequirementView> requirementRows = new List<RequirementView>();
    [Header("Sprite states")]
    public Sprite frameLocked,frameAvailable,frameUnlocked,frameSelected;
    public Sprite fillLocked,fillAvailable,fillUnlocked,fillSelected;
    public Sprite questionIcon,coreIcon,lockIcon,checkIcon,crossIcon,rowMet,rowUnmet,levelIcon,pointIcon;
    [Header("Synergy connections only")]
    public Sprite linkLockedHorizontal,linkLockedVertical,linkLockedCorner,portLocked;
    public Sprite linkUnlockedHorizontal,linkUnlockedVertical,linkUnlockedCorner,portUnlocked;
    [Header("Gameplay integration")]
    public SkillUnlockEvent UnlockRequested = new SkillUnlockEvent();

    private static readonly Color Muted = new Color32(131,152,175,255);
    private static readonly Color Ready = new Color32(118,214,183,255);
    private static readonly Color Missing = new Color32(214,140,136,255);
    private int graphHash = int.MinValue;

    private void OnEnable()
    {
        InitializeStates();
        if (Application.isPlaying)
        {
            foreach(var n in nodes) {SkillUnlockProgress.Register(n.skill);SkillUnlockProgress.RegisterRecipe(n.recipe);}
            SkillUnlockProgress.ObserveInventory();
            SkillUnlockProgress.Changed += OnProgressChanged;
            ItemUnlockProgress.Changed += OnProgressChanged;
            PlayerSessionState.InventoryChanged += OnProgressChanged;
        }
        FitToParent(); selectedIndex=-1; RefreshSelection();
    }
    private void OnDisable() {SkillUnlockProgress.Changed -= OnProgressChanged;ItemUnlockProgress.Changed-=OnProgressChanged;PlayerSessionState.InventoryChanged-=OnProgressChanged;}
    private void Update()
    {
        FitToParent();
        if (!Application.isPlaying && ComputeGraphHash()!=graphHash) RefreshSelection();
    }
    private void OnProgressChanged() {RefreshSelection();}

    public void InitializeStates()
    {
        foreach(var n in nodes)
        {
            if(n==null||n.stateInitialized) continue;
            n.state=n.unlocked?SkillKnowledge.Unlocked:n.unknown?SkillKnowledge.Unknown:SkillKnowledge.Discovered;
            n.stateInitialized=true;
        }
    }
    public SkillKnowledge StateOf(NodeView n)
    {
        if(n==null)return SkillKnowledge.Unknown;
        if(n.weapon!=null)return n.weapon.IsUnlocked?SkillKnowledge.Unlocked:SkillKnowledge.Discovered;
        if(n.skin!=null)return n.skin.IsUnlocked?SkillKnowledge.Unlocked:SkillKnowledge.Discovered;
        if(Application.isPlaying&&n.skill!=null)return SkillUnlockProgress.GetState(n.skill);
        if(n.state==SkillKnowledge.Unknown&&n.recipe!=null&&n.recipe.RequiredSkills.Count>0)
        {
            bool allKnown=true;
            foreach(var s in n.recipe.RequiredSkills)allKnown &= StateOfSkillDirect(s)>=SkillKnowledge.Discovered;
            if(allKnown)return SkillKnowledge.Discovered;
        }
        return n.state;
    }
    private SkillKnowledge StateOfSkillDirect(SkillData s)
    {
        if(s==null)return SkillKnowledge.Unknown;
        if(Application.isPlaying)return SkillUnlockProgress.GetState(s);
        foreach(var n in nodes)if(n.skill==s)return n.state;
        return SkillKnowledge.Unknown;
    }
    public SkillKnowledge StateOfSkill(SkillData s)
    {
        if(s==null)return SkillKnowledge.Unknown;
        foreach(var n in nodes)if(n.skill==s)return StateOf(n);
        return Application.isPlaying?SkillUnlockProgress.GetState(s):SkillKnowledge.Unknown;
    }
    public void RevealSkill(SkillData skill)
    {
        if(Application.isPlaying){SkillUnlockProgress.RevealSkill(skill);return;}
        foreach(var n in nodes)if(n.skill==skill&&n.state==SkillKnowledge.Unknown)n.state=SkillKnowledge.Discovered;
        RefreshSelection();
    }
    public void MarkSkillUnlocked(SkillData skill)
    {
        if(Application.isPlaying){SkillUnlockProgress.MarkUnlocked(skill);return;}
        foreach(var n in nodes)if(n.skill==skill)n.state=SkillKnowledge.Unlocked;
        RefreshSelection();
    }
    public void FitToParent()
    {
        var parent=transform as RectTransform;
        if(parent==null||designRoot==null)return;
        float fit=Mathf.Min(parent.rect.width/designSize.x,parent.rect.height/designSize.y);
        if(fit>0&&(designRoot.localScale-new Vector3(fit,fit,1)).sqrMagnitude>0.00001f)designRoot.localScale=new Vector3(fit,fit,1);
    }
    public void SelectNode(int index)
    {
        if(index<0||index>=nodes.Count)return;
        selectedIndex=index;RefreshSelection();
        if(requirementsScroll!=null){Canvas.ForceUpdateCanvases();requirementsScroll.verticalNormalizedPosition=1;}
    }
    public void HideDetails() {selectedIndex=-1;RefreshSelection();}
    private bool AllPrerequisitesMet(NodeView n)
    {
        if(n.recipe==null)return true;
        foreach(var s in n.recipe.RequiredSkills)if(s==null||StateOfSkill(s)!=SkillKnowledge.Unlocked)return false;
        return n.recipe.RequiredSkills.Count>0;
    }
    private bool CanLearn(NodeView n)
    {
        if(n==null||n.invalidData)return false;
        if(n.weapon!=null)return n.weapon.CanUnlock;
        if(n.skin!=null)return n.skin.CanUnlock;
        if(n.skill==null||StateOf(n)!=SkillKnowledge.Discovered)return false;
        if(Application.isPlaying)return SkillUnlockProgress.CanUnlock(n.skill);
        if(!AllPrerequisitesMet(n))return false;
        var totals=new Dictionary<string,long>();
        foreach(var item in n.skill.UnlockItems)
        {
            if(item==null||string.IsNullOrWhiteSpace(item.ItemId))return false;
            if(!totals.ContainsKey(item.ItemId))totals[item.ItemId]=0;
            totals[item.ItemId]+=Mathf.Max(1,item.amount);
        }
        foreach(var cost in totals)if(PlayerSessionState.GetStoredItemCount(cost.Key)<cost.Value)return false;
        return true;
    }

    [ContextMenu("Refresh selection")]
    public void RefreshSelection()
    {
        InitializeStates();
        foreach(var n in nodes)
        {
            if(n==null)continue;
            var state=StateOf(n);bool unknown=state==SkillKnowledge.Unknown,learned=state==SkillKnowledge.Unlocked;
            bool chosen=selectedIndex>=0&&selectedIndex<nodes.Count&&nodes[selectedIndex]==n;
            bool available=CanLearn(n);
            n.unknown=unknown;n.unlocked=learned;
            if(n.fill!=null)n.fill.sprite=learned?fillUnlocked:available?fillAvailable:fillLocked;
            if(n.frame!=null)n.frame.sprite=chosen?frameSelected:learned?frameUnlocked:available?frameAvailable:frameLocked;
            if(n.selected!=null)n.selected.SetActive(chosen);
            if(n.learnedBadge!=null)n.learnedBadge.SetActive(learned);
            if(n.icon!=null)
            {
                n.icon.sprite=unknown?questionIcon:n.DisplayIcon!=null?n.DisplayIcon:coreIcon;
                n.icon.rectTransform.sizeDelta=unknown?new Vector2(16,16):new Vector2(24,24);
            }
            if(n.label!=null)n.label.text=unknown?"?":n.title;
        }
        FitGraphBounds();
        int hash=ComputeGraphHash();
        if(hash!=graphHash){RebuildConnections();graphHash=ComputeGraphHash();}
        bool show=selectedIndex>=0&&selectedIndex<nodes.Count;
        if(detailsPanel!=null)detailsPanel.SetActive(show);
        if(!show||detailTitle==null)return;
        var selected=nodes[selectedIndex];
        if(selected.IsEquipment){RenderEquipmentDetails(selected);return;}
        var selectedState=StateOf(selected);bool hidden=selectedState==SkillKnowledge.Unknown;
        detailTitle.text=hidden?"?":selected.skill!=null?selected.skill.SkillName.TrimStart('★',' '):selected.title;
        detailCategory.text=hidden?"UNDISCOVERED SPELL":selected.recipe!=null?"SYNERGY":selected.skill!=null?"ACTIVE SPELL / "+selected.skill.ElementTag:"ARCANE FOUNDATION";
        detailDescription.text=hidden?"Find an item related to this spell to reveal its identity.":selected.skill!=null?selected.skill.Description:selected.description;
        detailIcon.sprite=hidden?questionIcon:selected.skill!=null&&selected.skill.SkillIcon!=null?selected.skill.SkillIcon:coreIcon;
        detailIcon.preserveAspect=true;
        requirementsTitle.text="REQUIREMENTS";
        var rows=new List<RequirementLine>();
        if(selected.recipe!=null)
        {
            foreach(var prerequisite in selected.recipe.RequiredSkills)
            {
                var state=StateOfSkill(prerequisite);bool known=state!=SkillKnowledge.Unknown,met=state==SkillKnowledge.Unlocked;
                rows.Add(new RequirementLine(known&&prerequisite!=null?prerequisite.SkillName.TrimStart('★',' '):"?",met?"Unlocked":known?"Discovered - locked":"Undiscovered",met,known&&prerequisite!=null&&prerequisite.SkillIcon!=null?prerequisite.SkillIcon:questionIcon));
            }
        }
        if(!hidden&&selected.skill!=null)
        {
            var itemDefinitions=new Dictionary<string,SkillUnlockItemRequirement>();
            var itemTotals=new Dictionary<string,long>();
            foreach(var item in selected.skill.UnlockItems)
            {
                if(item==null)continue;
                string key=item.ItemId??"";
                if(!itemDefinitions.ContainsKey(key)){itemDefinitions[key]=item;itemTotals[key]=0;}
                itemTotals[key]+=Mathf.Max(1,item.amount);
            }
            foreach(var pair in itemDefinitions)
            {
                var item=pair.Value;int count=PlayerSessionState.GetStoredItemCount(pair.Key);long need=itemTotals[pair.Key];
                bool met=selectedState==SkillKnowledge.Unlocked||count>=need;
                rows.Add(new RequirementLine(string.IsNullOrWhiteSpace(item.DisplayName)?(string.IsNullOrWhiteSpace(pair.Key)?"?":pair.Key):item.DisplayName,selectedState==SkillKnowledge.Unlocked?"Completed":count+" / "+need,met,item.Icon!=null?item.Icon:pointIcon));
            }
        }
        if(rows.Count==0)
            rows.Add(new RequirementLine(hidden?"?":selectedState==SkillKnowledge.Unlocked?"Spell unlocked":"Unlock requirements",hidden?"Undiscovered":selectedState==SkillKnowledge.Unlocked?"Completed":"Ready",!hidden,hidden?questionIcon:checkIcon));
        RenderRows(rows);
        bool ready=CanLearn(selected);
        if(unlockButton!=null)unlockButton.interactable=ready;
        if(unlockButtonLabel!=null){unlockButtonLabel.text=selectedState==SkillKnowledge.Unlocked?"UNLOCKED":ready?"UNLOCK SKILL":"LOCKED";unlockButtonLabel.color=ready?Color.white:Muted;}
        if(detailHint!=null){detailHint.text=selectedState==SkillKnowledge.Unlocked?"This skill is already unlocked.":ready?"All requirements are ready.":hidden?"Discover this skill first.":"Some requirements are still locked.";detailHint.color=ready?Ready:Muted;}
    }
    private void RenderEquipmentDetails(NodeView node)
    {
        bool learned=StateOf(node)==SkillKnowledge.Unlocked,ready=CanLearn(node);
        detailTitle.text=node.weapon!=null?node.weapon.WeaponName:node.skin.SkinName;
        detailCategory.text=node.weapon!=null?"STAFF":"SKIN";
        detailDescription.text=node.weapon!=null?node.weapon.Description:node.skin.Description;
        detailIcon.sprite=node.DisplayIcon;detailIcon.preserveAspect=true;
        requirementsTitle.text="REQUIREMENTS";
        var costs=node.weapon!=null?node.weapon.UnlockItems:node.skin.UnlockItems;
        var totals=new Dictionary<ItemData,long>();bool invalid=false;
        foreach(var cost in costs)
        {
            if(cost==null||cost.item==null||cost.amount<1){invalid=true;continue;}
            if(!totals.ContainsKey(cost.item))totals[cost.item]=0;
            totals[cost.item]+=cost.amount;
        }
        var rows=new List<RequirementLine>();
        foreach(var pair in totals)
        {
            int owned=PlayerSessionState.GetStoredItemCount(pair.Key.ItemId);
            rows.Add(new RequirementLine(pair.Key.DisplayName,learned?"Completed":owned+" / "+pair.Value,learned||owned>=pair.Value,pair.Key.Icon));
        }
        if(invalid)rows.Add(new RequirementLine("Missing item data","Unavailable",false,questionIcon));
        if(rows.Count==0)rows.Add(new RequirementLine("Available by default","Unlocked",true,checkIcon));
        RenderRows(rows);

        var player = learned ? FindEquipmentPlayer() : null;
        var weapon = player != null && node.weapon != null ? player.GetComponentInChildren<WeaponShoot>() : null;
        var skin = player != null && node.skin != null ? player.GetComponent<PlayerSkinController>() : null;
        bool equipped = weapon != null ? weapon.CurrentWeapon == node.weapon : skin != null && skin.IsWearing(node.skin);
        bool canEquip = learned && !equipped && (weapon != null || skin != null);
        bool interactable = learned ? canEquip : ready;

        if (unlockButton != null) unlockButton.interactable = interactable;
        if (unlockButtonLabel != null)
        {
            unlockButtonLabel.text = learned ? (equipped ? "EQUIPPED" : "EQUIP")
                : ready ? "UNLOCK " + (node.weapon != null ? "STAFF" : "SKIN") : "LOCKED";
            unlockButtonLabel.color = equipped ? Ready : interactable ? Color.white : Muted;
        }
        if (detailHint != null)
        {
            if (equipped) detailHint.text = node.weapon != null ? "Currently held." : "Currently worn.";
            else if (canEquip) detailHint.text = "Ready to equip.";
            else if (learned) detailHint.text = "Available to equip in game.";
            else detailHint.text = ready ? "All requirements are ready." : "Collect the required materials.";
            detailHint.color = equipped || interactable ? Ready : Muted;
        }
    }

    private Health FindEquipmentPlayer()
    {
        if (!Application.isPlaying) return null;
        foreach (var player in FindObjectsByType<PlayerMovement>())
        {
            if (player.gameObject.scene == gameObject.scene && player.TryGetComponent<Health>(out var health)
                && health.Team == Team.Player && !health.IsDead)
                return health;
        }
        return null;
    }

    private void Equip(NodeView node)
    {
        var player = FindEquipmentPlayer();
        if (player == null) return;

        if (node.weapon != null)
        {
            var weapon = player.GetComponentInChildren<WeaponShoot>();
            if (weapon == null || weapon.CurrentWeapon == node.weapon) return;
            weapon.EquipWeapon(node.weapon);
        }
        else
        {
            var skin = player.GetComponent<PlayerSkinController>();
            if (skin == null || skin.IsWearing(node.skin)) return;
            skin.ApplySkin(node.skin);
        }

        if (player.TryGetComponent<PlayerStatePersistence>(out var persistence))
            persistence.Capture();
    }

    private void RenderRows(List<RequirementLine> rows)
    {
        while(requirementRows.Count<rows.Count&&requirementTemplate!=null&&requirementContent!=null)
        {
            var go=Instantiate(requirementTemplate,requirementContent,false);go.name="Requirement_"+requirementRows.Count;
            requirementRows.Add(new RequirementView {root=go,background=go.GetComponent<Image>(),icon=go.transform.Find("CostIcon").GetComponent<Image>(),status=go.transform.Find("Status").GetComponent<Image>(),label=go.transform.Find("RequirementName").GetComponent<TMP_Text>(),value=go.transform.Find("RequirementValue").GetComponent<TMP_Text>()});
        }
        for(int i=0;i<requirementRows.Count;i++)
        {
            var view=requirementRows[i];if(view.root==null)continue;
            view.root.SetActive(i<rows.Count);if(i>=rows.Count)continue;
            var rt=(RectTransform)view.root.transform;rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(0,-i*38);rt.sizeDelta=new Vector2(224,32);rt.localScale=Vector3.one;
            var data=rows[i];view.background.sprite=data.met?rowMet:rowUnmet;view.icon.sprite=data.icon;view.icon.preserveAspect=true;
            view.label.text=data.label;view.value.text=data.value;view.value.color=data.met?Ready:Missing;view.status.sprite=data.met?checkIcon:crossIcon;
        }
        if(requirementContent!=null)requirementContent.sizeDelta=new Vector2(224,Mathf.Max(108,rows.Count*38-6));
        if(requirementsScroll!=null&&requirementsScroll.verticalScrollbar!=null)requirementsScroll.verticalScrollbar.gameObject.SetActive(rows.Count>3);
    }

    private void FitGraphBounds()
    {
        if(graphRoot==null)return;
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        var corners=new Vector3[4];bool any=false;
        foreach(var n in nodes)
        {
            if(n.button==null)continue;any=true;((RectTransform)n.button.transform).GetWorldCorners(corners);
            foreach(var c in corners){Vector2 p=graphRoot.InverseTransformPoint(c);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        }
        if(!any)return;
        Vector2 shift=new Vector2(Mathf.Max(0,64-min.x),Mathf.Min(0,-64-max.y));
        if(shift.sqrMagnitude>0.01f)
        {
            foreach(var n in nodes)if(n.button!=null)((RectTransform)n.button.transform).anchoredPosition+=shift;
            graphRoot.anchoredPosition-=shift*graphRoot.localScale.x;min+=shift;max+=shift;
        }
        Vector2 size=new Vector2(Mathf.Max(1600,max.x+128),Mathf.Max(1100,-min.y+128));
        if((graphRoot.sizeDelta-size).sqrMagnitude>0.01f)graphRoot.sizeDelta=size;
    }
    private int ComputeGraphHash()
    {
        unchecked
        {
            int hash=17;
            foreach(var n in nodes)
            {
                hash=hash*31+(int)StateOf(n);
                if(n.button!=null)hash=hash*31+((RectTransform)n.button.transform).anchoredPosition.GetHashCode();
                if(n.recipe!=null)foreach(var s in n.recipe.RequiredSkills)hash=hash*31+(s!=null?s.SkillId.GetHashCode():0);
            }
            return hash;
        }
    }
    public void RebuildConnections()
    {
        if(graphRoot==null||connectionsRoot==null||linkLockedHorizontal==null)return;
        for(int i=connectionsRoot.childCount-1;i>=0;i--)
        {
            var child=connectionsRoot.GetChild(i).gameObject;child.SetActive(false);
            if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);
        }
        var pairs=new HashSet<string>();
        foreach(var target in nodes)
        {
            if(target.recipe==null||target.button==null)continue;
            bool allReady=AllPrerequisitesMet(target);
            foreach(var skill in target.recipe.RequiredSkills)
            {
                NodeView source=null;foreach(var n in nodes)if(n.skill!=null&&n.skill==skill){source=n;break;}
                if(source==null||source.button==null||!pairs.Add(source.id+"->"+target.id))continue;
                DrawSynergy(source,target,StateOf(source)==SkillKnowledge.Unlocked,allReady);
            }
        }
    }
    private Image LinkImage(string name,Sprite sprite,Vector2 center,Vector2 size,bool stretch)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.layer=connectionsRoot.gameObject.layer;
        var rt=(RectTransform)go.transform;rt.SetParent(connectionsRoot,false);rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=Vector2.one*0.5f;rt.anchoredPosition=center;rt.sizeDelta=size;
        var image=go.GetComponent<Image>();image.sprite=sprite;image.type=stretch?Image.Type.Sliced:Image.Type.Simple;image.raycastTarget=false;return image;
    }
    private void DrawSynergy(NodeView source,NodeView target,bool sourceReady,bool allReady)
    {
        var a=(RectTransform)source.button.transform;var b=(RectTransform)target.button.transform;
        Vector2 ac=graphRoot.InverseTransformPoint(a.TransformPoint(a.rect.center)),bc=graphRoot.InverseTransformPoint(b.TransformPoint(b.rect.center));
        Vector2 delta=bc-ac;Vector2 start,end;Vector2[] points;
        if(Mathf.Abs(delta.y)>=Mathf.Abs(delta.x)*0.5f)
        {
            float sign=Mathf.Sign(delta.y);start=ac+Vector2.up*sign*a.rect.height*0.5f;end=bc-Vector2.up*sign*b.rect.height*0.5f;
            float mid=(start.y+end.y)*0.5f;points=new[]{start,new Vector2(start.x,mid),new Vector2(end.x,mid),end};
        }
        else
        {
            float sign=Mathf.Sign(delta.x);start=ac+Vector2.right*sign*a.rect.width*0.5f;end=bc-Vector2.right*sign*b.rect.width*0.5f;
            float mid=(start.x+end.x)*0.5f;points=new[]{start,new Vector2(mid,start.y),new Vector2(mid,end.y),end};
        }
        for(int i=0;i<3;i++)
        {
            bool ready=i==2?allReady:sourceReady;var p=points[i];var q=points[i+1];bool horizontal=Mathf.Abs(p.y-q.y)<0.01f;
            LinkImage("Synergy_"+source.id+"_"+target.id+"_"+i,ready?(horizontal?linkUnlockedHorizontal:linkUnlockedVertical):(horizontal?linkLockedHorizontal:linkLockedVertical),(p+q)*0.5f,horizontal?new Vector2(Mathf.Max(1,Mathf.Abs(q.x-p.x)),8):new Vector2(8,Mathf.Max(1,Mathf.Abs(q.y-p.y))),true);
        }
        for(int i=1;i<3;i++)
        {
            bool ready=i==2?allReady:sourceReady;var p=points[i];bool north=points[i-1].y>p.y||points[i+1].y>p.y,east=points[i-1].x>p.x||points[i+1].x>p.x;
            var corner=LinkImage("SynergyCorner",ready?linkUnlockedCorner:linkLockedCorner,p,new Vector2(16,16),false);
            corner.rectTransform.localEulerAngles=new Vector3(0,0,north?(east?0:90):(east?270:180));
        }
        LinkImage("SynergySourcePort",sourceReady?portUnlocked:portLocked,start,new Vector2(10,10),false);
        LinkImage("SynergyTargetPort",allReady?portUnlocked:portLocked,end,new Vector2(10,10),false);
    }
    public void RequestUnlock()
    {
        if (selectedIndex < 0 || selectedIndex >= nodes.Count) return;
        var node = nodes[selectedIndex];
        if (node == null || node.invalidData) return;

        if (node.IsEquipment)
        {
            if (!Application.isPlaying) return;
            if (StateOf(node) == SkillKnowledge.Unlocked)
                Equip(node);
            else if (CanLearn(node))
            {
                if (node.weapon != null) node.weapon.TryUnlock();
                else node.skin.TryUnlock();
            }
            RefreshSelection();
            return;
        }

        if (!CanLearn(node)) return;
        var skill=node.skill;
        if(Application.isPlaying&&!SkillUnlockProgress.TryUnlock(skill))return;
        UnlockRequested.Invoke(skill);RefreshSelection();
    }
    public void Close() {if(panelRoot!=null)panelRoot.SetActive(false);}
}
