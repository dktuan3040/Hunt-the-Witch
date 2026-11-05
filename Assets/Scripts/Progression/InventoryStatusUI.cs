using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryStatusUI : MonoBehaviour
{
    private GameObject root;
    private TMP_Text label;
    private void Start()
    {
        root=new GameObject("InventoryStatus",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("Inventory",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
        var r=panel.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(1,1);r.anchoredPosition=new Vector2(-28,-28);r.sizeDelta=new Vector2(300,300);
        panel.GetComponent<Image>().color=new Color(.03f,.05f,.08f,.85f);panel.GetComponent<Image>().raycastTarget=false;
        var text=new GameObject("Contents",typeof(RectTransform),typeof(TextMeshProUGUI));text.transform.SetParent(panel.transform,false);
        var tr=text.GetComponent<RectTransform>();tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(16,12);tr.offsetMax=new Vector2(-16,-12);
        label=text.GetComponent<TextMeshProUGUI>();label.fontSize=24;label.raycastTarget=false;
        PlayerSessionState.InventoryChanged+=Refresh;Refresh();
    }
    private void Refresh()
    {
        if(label==null)return;
        bool run=ExpeditionFlow.IsExpeditionActive;
        var text=new StringBuilder(run?"TEMPORARY BAG\nReturn to Home to keep loot\n\n":"STORAGE\nSaved permanently\n\n");
        int shown=0;
        if(run){foreach(var item in PlayerSessionState.Inventory){if(shown++<7)text.AppendLine(Name(item.Key)+" x"+item.Value);}}
        else{foreach(var item in GameProfile.Bank){if(shown++<7)text.AppendLine(Name(item.id)+" x"+item.amount);}}
        if(shown==0)text.Append("Empty");else if(shown>7)text.Append("+ "+(shown-7)+" more item types");label.text=text.ToString();
    }
    private static string Name(string id)=>GameContentCatalog.Instance!=null?GameContentCatalog.Instance.ItemName(id):id;
    private void OnDestroy(){PlayerSessionState.InventoryChanged-=Refresh;if(root!=null)Destroy(root);}
}
