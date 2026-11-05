using UnityEngine;

[RequireComponent(typeof(CircleCollider2D),typeof(Rigidbody2D))]
public sealed class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData item;
    [SerializeField,Min(1)] private int amount=1;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField,Min(.1f)] private float visualSize=.6f;
    private bool collected;
    public ItemData Item => item;
    public int Amount => amount;
    private void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger=true;
        if(item!=null)Initialize(item,amount);
        var rb=GetComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;rb.useFullKinematicContacts=true;
    }
    public void Initialize(ItemData data,int count)
    {
        item=data;amount=Mathf.Max(1,count);collected=false;
        if(visual==null)visual=GetComponentInChildren<SpriteRenderer>();
        // Scale a dedicated visual so the pickup collider is not resized with it.
        if(visual!=null&&visual.transform==transform)
        {
            var source=visual;
            var child=new GameObject("ItemVisual");child.transform.SetParent(transform,false);
            visual=child.AddComponent<SpriteRenderer>();
            visual.sharedMaterial=source.sharedMaterial;visual.sortingLayerID=source.sortingLayerID;
            visual.flipX=source.flipX;visual.flipY=source.flipY;visual.maskInteraction=source.maskInteraction;
            source.enabled=false;
        }
        if(visual!=null&&data!=null){visual.sprite=data.Icon;visual.color=Color.white;visual.sortingOrder=20;if(data.Icon!=null){float size=Mathf.Max(data.Icon.bounds.size.x,data.Icon.bounds.size.y);if(size>0)visual.transform.localScale=Vector3.one*(visualSize/size);}}
    }
    private void OnTriggerEnter2D(Collider2D other){TryCollect(other);}
    private void OnTriggerStay2D(Collider2D other){TryCollect(other);}
    public bool TryCollect(Component actor)
    {
        if(collected||item==null||actor==null)return false;
        var health=actor.GetComponentInParent<Health>();
        if(health==null||health.Team!=Team.Player||health.IsDead)return false;
        if(item.PickupEffect==ItemPickupEffect.Heal)
        {
            int missing=health.MaxHealth-health.CurrentHealth;
            int restore=(int)System.Math.Min(missing,System.Math.Ceiling((decimal)health.MaxHealth*(decimal)item.HealFraction*amount));
            if(restore<=0)return false;
            collected=true;health.Heal(restore);
        }
        else
        {
            if((long)PlayerSessionState.GetItemCount(item.ItemId)+amount>int.MaxValue)return false;
            collected=true;PlayerSessionState.AddItem(item.ItemId,amount);
        }
        if(this!=null){gameObject.SetActive(false);Destroy(gameObject);}return true;
    }
}
