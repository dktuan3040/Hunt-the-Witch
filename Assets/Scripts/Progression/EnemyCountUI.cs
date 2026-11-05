using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyCountUI : MonoBehaviour
{
    private readonly HashSet<Health> known=new HashSet<Health>();
    private GameObject root;
    private TMP_Text label;
    private float nextScan;
    public int Remaining {get;private set;}
    public int Total => known.Count;
    private void Start()
    {
        root=new GameObject("EnemyCountHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("EnemyCounter",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-28,-28);rect.sizeDelta=new Vector2(380,72);
        panel.GetComponent<Image>().color=new Color(.03f,.05f,.08f,.85f);panel.GetComponent<Image>().raycastTarget=false;
        var text=new GameObject("RemainingEnemies",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(panel.transform,false);
        var tr=text.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(16,8);tr.offsetMax=new Vector2(-16,-8);
        label=text.GetComponent<TextMeshProUGUI>();label.fontSize=28;label.alignment=TextAlignmentOptions.MidlineRight;label.raycastTarget=false;
        RefreshCount();
    }
    public void RefreshCount()
    {
        // Include dormant scene bosses and inactive placed enemies in the map total.
        // Remember observed enemies so destruction after death cannot shrink the denominator.
        foreach(var health in FindObjectsByType<Health>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(health.Team==Team.Enemy&&health.gameObject.scene==gameObject.scene)known.Add(health);
        Remaining=0;
        foreach(var health in known)if(health!=null&&!health.IsDead&&health.gameObject.scene==gameObject.scene)Remaining++;
        if(label!=null)label.text="Enemies remaining: "+Remaining+"/"+Total;
    }
    private void Update(){if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+.2f;RefreshCount();}
    private void OnDestroy(){if(root!=null)Destroy(root);}
}
