using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class UnlockMenuTabs : MonoBehaviour
{
    public GameObject[] pages=new GameObject[3];
    public Button[] buttons;
    public Sprite activeFrame,inactiveFrame;
    [Range(0,2)] public int selectedTab;
    private void OnEnable(){Bind();SelectTab(selectedTab);}
    public void Bind()
    {
        if(buttons==null)return;
        for(int i=0;i<buttons.Length;i++)
        {
            int index=i%3;
            if(buttons[i]==null)continue;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(()=>SelectTab(index));
        }
    }
    public void SelectTab(int index)
    {
        if(index<0||index>=pages.Length)return;
        selectedTab=index;
        for(int i=0;i<pages.Length;i++)if(pages[i]!=null)pages[i].SetActive(i==index);
        if(buttons==null)return;
        for(int i=0;i<buttons.Length;i++)
        {
            var button=buttons[i];if(button==null)continue;bool active=i%3==index;
            var rect=(RectTransform)button.transform;
            rect.sizeDelta=new Vector2(64,active?64:48);
            rect.anchoredPosition=new Vector2(36+(i%3)*72,active?-18:-34);
            var image=button.GetComponent<Image>();
            var icon=button.transform.Find("OutlineIcon") as RectTransform;
            if(icon!=null)
            {
                icon.anchoredPosition=Vector2.zero;
                icon.sizeDelta=Vector2.one*(active?30:24);
            }
            image.sprite=active?activeFrame:inactiveFrame;
            image.color=active?Color.white:new Color(.65f,.72f,.83f,1);
        }
    }
}
