using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionOutline : MonoBehaviour
{
    private sealed class Border
    {
        public SpriteRenderer source;
        public readonly SpriteRenderer[] copies=new SpriteRenderer[8];
    }
    private readonly List<Border> borders=new List<Border>();
    private static readonly Vector2[] offsets={Vector2.left,Vector2.right,Vector2.up,Vector2.down,new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)};
    public bool IsVisible {get;private set;}
    private bool built;
    [SerializeField,Min(.001f)] private float minimumWorldWidth=.035f;
    public void SetVisible(bool visible)
    {
        if(visible&&!built)Build();
        IsVisible=visible;
        Sync();
    }
    private void Build()
    {
        var material=Resources.Load<Material>("InteractionOutline");
        if(material==null)return;
        var sources=GetComponentsInChildren<SpriteRenderer>(true);
        foreach(var source in sources)
        {
            var border=new Border{source=source};
            for(int i=0;i<offsets.Length;i++)
            {
                var child=new GameObject("InteractionOutline_"+i);
                child.layer=source.gameObject.layer;
                child.transform.SetParent(source.transform,false);
                var copy=child.AddComponent<SpriteRenderer>();
                copy.sharedMaterial=material;copy.enabled=false;
                border.copies[i]=copy;
            }
            borders.Add(border);
        }
        built=true;
    }
    private void LateUpdate(){if(IsVisible){if(!built)Build();Sync();}}
    private void Sync()
    {
        foreach(var border in borders)
        {
            var source=border.source;
            bool show=IsVisible&&isActiveAndEnabled&&source!=null&&source.enabled&&source.gameObject.activeInHierarchy&&source.sprite!=null;
            for(int i=0;i<border.copies.Length;i++)
            {
                var copy=border.copies[i];if(copy==null)continue;
                copy.enabled=show;if(!show)continue;
                copy.sprite=source.sprite;copy.color=new Color(1,1,1,source.color.a);
                copy.flipX=source.flipX;copy.flipY=source.flipY;
                copy.sortingLayerID=source.sortingLayerID;copy.sortingOrder=source.sortingOrder-1;
                copy.maskInteraction=source.maskInteraction;
                // Keep at least one source pixel, with a minimum world thickness so
                // high-resolution sprites scaled down to pickups remain readable.
                float pixel=1f/source.sprite.pixelsPerUnit;
                var scale=source.transform.lossyScale;
                float x=Mathf.Max(pixel,minimumWorldWidth/Mathf.Max(.0001f,Mathf.Abs(scale.x)));
                float y=Mathf.Max(pixel,minimumWorldWidth/Mathf.Max(.0001f,Mathf.Abs(scale.y)));
                copy.transform.localPosition=new Vector3(offsets[i].x*x,offsets[i].y*y,0);
            }
        }
    }
    private void OnDisable(){IsVisible=false;Sync();}
}
