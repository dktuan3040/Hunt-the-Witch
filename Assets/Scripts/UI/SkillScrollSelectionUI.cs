using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SkillScrollSelectionUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Image incomingIcon,qIcon,eIcon;
    public TMP_Text incomingName,qName,eName;
    public GameObject qSelected,eSelected;
    public Button confirmButton;
    private static SkillScrollSelectionUI instance;
    private static int closedFrame=-100;
    public static bool IsOpen => instance!=null&&instance.panelRoot!=null&&instance.panelRoot.activeSelf;
    public static bool BlocksGameplay => IsOpen||Time.frameCount<=closedFrame+1;
    public int SelectedSlot {get;private set;}
    private PlayerSkillManager manager;
    private SkillScrollPickup incoming;
    private SkillData startingSkill;
    private bool ownsPause;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;closedFrame=-100;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
    private static void SceneLoaded(Scene scene,LoadSceneMode mode){Ensure();}
    private static SkillScrollSelectionUI Ensure()
    {
        if(instance!=null)return instance;
        instance=FindAnyObjectByType<SkillScrollSelectionUI>(FindObjectsInactive.Include);
        if(instance==null){var prefab=Resources.Load<SkillScrollSelectionUI>("SkillScrollSelectionUI");if(prefab!=null)instance=Instantiate(prefab);}
        return instance;
    }
    private void Awake(){if(instance==null)instance=this;if(panelRoot!=null)panelRoot.SetActive(false);}
    private void OnDisable(){if(instance==this&&ownsPause)Close(true);}
    private void OnDestroy(){if(instance==this){if(ownsPause)Close(true);instance=null;}}
    public static bool Show(PlayerSkillManager player,SkillScrollPickup scroll)
    {
        if(IsOpen||player==null||scroll==null)return false;
        var ui=Ensure();if(ui==null||ui.panelRoot==null)return false;
        ui.incoming=scroll;
        ui.Open(player);
        return true;
    }

    public static bool ShowStartingSkill(PlayerSkillManager player, SkillData skill)
    {
        if (IsOpen || player == null || skill == null ||
            player.GetReplacementSkill(0) != null || player.GetReplacementSkill(1) != null ||
            SkillUnlockProgress.GetState(skill) != SkillKnowledge.Unlocked) return false;

        var ui = Ensure();
        if (ui == null || ui.panelRoot == null) return false;
        ui.startingSkill = skill;
        ui.Open(player);
        return true;
    }

    private void Open(PlayerSkillManager player)
    {
        manager = player;
        ownsPause = true;
        SelectedSlot = 0;
        SetModeText();
        panelRoot.SetActive(true);
        GameplayPause.Acquire(this);
        Refresh();
    }

    private void SetModeText()
    {
        bool isStarting = startingSkill != null;
        var title = panelRoot.transform.Find("Window/Title")?.GetComponent<TMP_Text>();
        var instructions = panelRoot.transform.Find("Window/Instructions")?.GetComponent<TMP_Text>();
        var cancel = panelRoot.transform.Find("Window/Cancel");
        if (title != null) title.text = isStarting ? "CHOOSE A SLOT" : "CHOOSE A SKILL TO REPLACE";
        if (instructions != null) instructions.text = isStarting
            ? "Left click Q or E to equip"
            : "Hover a slot - Left click to replace";
        if (cancel != null) cancel.gameObject.SetActive(!isStarting);
    }
    private void Update()
    {
        if(!IsOpen||instance!=this)return;
        var health = manager != null ? manager.GetComponent<Health>() : null;
        if (health == null || health.IsDead || (incoming == null && startingSkill == null))
        {
            Close(true);
            return;
        }
        if (startingSkill == null && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Cancel();

    }
    public void SelectSlot(int slot){if(manager==null||slot<0||slot>1)return;SelectedSlot=slot;Refresh();}
    private void Refresh()
    {
        var a=manager.GetReplacementSkill(0);var b=manager.GetReplacementSkill(1);
        var offered = startingSkill != null ? startingSkill : incoming.Skill;
        incomingIcon.sprite=offered.SkillIcon;incomingName.text=offered.SkillName.TrimStart('★',' ');
        qIcon.sprite=a!=null?a.SkillIcon:null;eIcon.sprite=b!=null?b.SkillIcon:null;
        qName.text=a!=null?a.SkillName.TrimStart('★',' '):"-";eName.text=b!=null?b.SkillName.TrimStart('★',' '):"-";
        qSelected.SetActive(SelectedSlot==0);eSelected.SetActive(SelectedSlot==1);
        var current = manager.GetReplacementSkill(SelectedSlot);
        if (confirmButton != null) confirmButton.interactable = startingSkill != null ? current == null : current != null;
    }
    public void ChooseSlot(int slot){SelectSlot(slot);Confirm();}
    public bool Confirm()
    {
        if(!IsOpen||instance!=this||manager==null)return false;
        if (startingSkill != null)
        {
            if (manager.GetReplacementSkill(SelectedSlot) != null) return false;
            manager.EquipSkill(SelectedSlot, startingSkill);
            manager.SelectSlot(SelectedSlot);
            Close(false);
            return true;
        }

        if (incoming == null) return false;
        var old=manager.GetReplacementSkill(SelectedSlot);if(old==null)return false;
        var dropped=incoming.DropReplacement(old,manager.GetComponent<Health>());if(dropped==null)return false;
        manager.EquipSkill(SelectedSlot,incoming.Skill);manager.SelectSlot(SelectedSlot);incoming.Consume();
        Close(false);return true;
    }
    public void Cancel(){if(startingSkill==null)Close(true);}
    private void Close(bool release)
    {
        if(!ownsPause)return;
        ownsPause=false;
        if(release&&incoming!=null)incoming.Release(manager!=null?manager.GetComponent<Health>():null);
        manager=null;incoming=null;startingSkill=null;closedFrame=Time.frameCount;
        if(panelRoot!=null)panelRoot.SetActive(false);
        GameplayPause.Release(this);
    }
}
