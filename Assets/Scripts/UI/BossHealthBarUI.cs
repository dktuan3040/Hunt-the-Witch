using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public sealed class BossHealthBarUI : MonoBehaviour
{
    public Image fill;
    public TMP_Text bossName;
    private Canvas canvas;
    private Health target;
    public Health Target => target;
    private void Awake(){canvas=GetComponent<Canvas>();canvas.enabled=false;}
    public void Bind(Health boss,string displayName)
    {
        Unsubscribe();
        if(canvas==null)canvas=GetComponent<Canvas>();
        target=boss;
        if(bossName!=null)bossName.text=displayName;
        if(target!=null){target.OnHealthChanged+=Refresh;target.OnDied+=Hide;Refresh(target.CurrentHealth,target.MaxHealth);}
        canvas.enabled=target!=null&&!target.IsDead;
    }
    private void Refresh(int current,int maximum){if(fill!=null)fill.fillAmount=Mathf.Clamp01((float)current/Mathf.Max(1,maximum));}
    private void Hide(){if(canvas!=null)canvas.enabled=false;}
    private void Update(){if(target==null||target.IsDead)Hide();}
    private void Unsubscribe(){if(target!=null){target.OnHealthChanged-=Refresh;target.OnDied-=Hide;}}
    private void OnDestroy(){Unsubscribe();}
}
