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
    private float previousScale;
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
    private void OnDisable(){if(instance==this&&ownsPause)Cancel();}
    private void OnDestroy(){if(instance==this){if(ownsPause)Cancel();instance=null;}}
    public static bool Show(PlayerSkillManager player,SkillScrollPickup scroll)
    {
        if(IsOpen||player==null||scroll==null)return false;
        var ui=Ensure();if(ui==null||ui.panelRoot==null)return false;
        ui.manager=player;ui.incoming=scroll;ui.previousScale=Time.timeScale;ui.ownsPause=true;
        ui.SelectedSlot=0;ui.panelRoot.SetActive(true);Time.timeScale=0;
        ui.Refresh();return true;
    }
    private void Update()
    {
        if(!IsOpen||instance!=this)return;
        if(manager==null||incoming==null||manager.GetComponent<Health>()==null||manager.GetComponent<Health>().IsDead){Cancel();return;}
        if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){Cancel();return;}

    }
    public void SelectSlot(int slot){if(manager==null||slot<0||slot>1)return;SelectedSlot=slot;Refresh();}
    private void Refresh()
    {
        var a=manager.GetReplacementSkill(0);var b=manager.GetReplacementSkill(1);
        incomingIcon.sprite=incoming.Skill.SkillIcon;incomingName.text=incoming.Skill.SkillName.TrimStart('★',' ');
        qIcon.sprite=a!=null?a.SkillIcon:null;eIcon.sprite=b!=null?b.SkillIcon:null;
        qName.text=a!=null?a.SkillName.TrimStart('★',' '):"-";eName.text=b!=null?b.SkillName.TrimStart('★',' '):"-";
        qSelected.SetActive(SelectedSlot==0);eSelected.SetActive(SelectedSlot==1);
        confirmButton.interactable=manager.GetReplacementSkill(SelectedSlot)!=null;
    }
    public void ChooseSlot(int slot){SelectSlot(slot);Confirm();}
    public bool Confirm()
    {
        if(!IsOpen||instance!=this||manager==null||incoming==null)return false;
        var old=manager.GetReplacementSkill(SelectedSlot);if(old==null)return false;
        var dropped=incoming.DropReplacement(old,manager.GetComponent<Health>());if(dropped==null)return false;
        manager.EquipSkill(SelectedSlot,incoming.Skill);manager.SelectSlot(SelectedSlot);incoming.Consume();
        Close(false);return true;
    }
    public void Cancel(){Close(true);}
    private void Close(bool release)
    {
        if(!ownsPause)return;
        ownsPause=false;
        if(release&&incoming!=null)incoming.Release(manager!=null?manager.GetComponent<Health>():null);
        manager=null;incoming=null;closedFrame=Time.frameCount;
        if(panelRoot!=null)panelRoot.SetActive(false);Time.timeScale=previousScale;
    }
}
