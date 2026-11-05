using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PickupNotificationUI : MonoBehaviour
{
    private sealed class Entry {public TMP_Text text;public float expires;}
    private readonly List<Entry> entries=new List<Entry>();
    private static PickupNotificationUI instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){instance=null;}
    public static void Show(string itemName,int amount,bool isKey)
    {
        if(amount<=0)return;
        if(instance==null)
        {
            var root=new GameObject("PickupNotifications",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(PickupNotificationUI));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            instance=root.GetComponent<PickupNotificationUI>();
        }
        instance.Add(itemName,amount,isKey);
    }
    private void Add(string name,int amount,bool key)
    {
        if(entries.Count>=5){entries[0].text.gameObject.SetActive(false);Destroy(entries[0].text.gameObject);entries.RemoveAt(0);}
        var row=new GameObject("PickupMessage",typeof(RectTransform),typeof(TextMeshProUGUI));row.transform.SetParent(transform,false);
        var rect=row.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;rect.sizeDelta=new Vector2(900,40);
        var text=row.GetComponent<TextMeshProUGUI>();text.fontSize=28;text.fontStyle=FontStyles.Bold;text.raycastTarget=false;
        text.color=key?new Color(1f,.82f,.12f):Color.white;text.text="+"+amount+" "+name;
        entries.Add(new Entry{text=text,expires=Time.unscaledTime+3.5f});Layout();
    }
    private void Layout(){for(int i=0;i<entries.Count;i++)((RectTransform)entries[i].text.transform).anchoredPosition=new Vector2(32,32+(entries.Count-1-i)*42);}
    private void Update()
    {
        bool changed=false;
        for(int i=entries.Count-1;i>=0;i--)
        {
            var entry=entries[i];float left=entry.expires-Time.unscaledTime;
            if(left<=0){entry.text.gameObject.SetActive(false);Destroy(entry.text.gameObject);entries.RemoveAt(i);changed=true;continue;}
            entry.text.alpha=Mathf.Clamp01(left/.6f);
        }
        if(changed)Layout();
    }
    private void OnDestroy(){if(instance==this)instance=null;}
}
