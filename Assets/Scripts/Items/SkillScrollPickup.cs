using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D),typeof(Rigidbody2D))]
public sealed class SkillScrollPickup : MonoBehaviour
{
    [SerializeField] private SkillData skill;
    public SpriteRenderer skillIcon;
    public SpriteRenderer scrollVisual;
    public Sprite defaultScrollSprite;
    [SerializeField,Min(.1f)] private float visualSize=.7f;
    public SkillData Skill => skill;
    private SkillScrollPickup template;
    private bool claimed,consumed;
    private float availableAt;
    [Min(.1f)] public float interactionRange=1.2f;
    private void Awake(){GetComponent<CircleCollider2D>().isTrigger=true;var rb=GetComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.gravityScale=0;rb.useFullKinematicContacts=true;ApplyVisual();}
    public static SkillScrollPickup Spawn(SkillScrollPickup prefab,SkillData data,Vector3 position,Health ignore)
    {
        if(prefab==null||data==null)return null;
        var pickup=Instantiate(prefab,position,Quaternion.identity);pickup.skill=data;pickup.template=prefab;pickup.claimed=false;pickup.consumed=false;pickup.availableAt=Time.time+.65f;

        pickup.name="Scroll_"+data.SkillId;
        pickup.ApplyVisual();
        pickup.gameObject.SetActive(true);return pickup;
    }
    private void ApplyVisual()
    {
        if(skillIcon!=null)skillIcon.enabled=false;
        if(scrollVisual==null)scrollVisual=GetComponentInChildren<SpriteRenderer>(true);
        if(scrollVisual==null||defaultScrollSprite==null)return;
        scrollVisual.sprite=defaultScrollSprite;scrollVisual.color=Color.white;scrollVisual.enabled=true;scrollVisual.sortingOrder=20;
        float size=Mathf.Max(defaultScrollSprite.bounds.size.x,defaultScrollSprite.bounds.size.y);
        if(size>0)scrollVisual.transform.localScale=Vector3.one*(visualSize/size);
    }
    public bool CanInteract(PlayerSkillManager manager)
    {
        if(!isActiveAndEnabled||consumed||claimed||skill==null||manager==null||Time.time<availableAt||SkillScrollSelectionUI.BlocksGameplay||PauseMenuUI.InteractionInputBlocked)return false;
        var actor=manager.GetComponent<Health>();
        return actor!=null&&actor.Team==Team.Player&&!actor.IsDead&&manager.gameObject.scene==gameObject.scene&&Vector2.Distance(manager.transform.position,transform.position)<=interactionRange&&SkillUnlockProgress.GetState(skill)==SkillKnowledge.Unlocked;
    }
    public bool TryTake(PlayerSkillManager manager)
    {
        if(!CanInteract(manager))return false;
        for(int i=0;i<manager.Slots.Length;i++)if(manager.Slots[i]==null){manager.EquipSkill(i,skill);Consume();return true;}
        claimed=true;
        if(SkillScrollSelectionUI.Show(manager,this))return true;
        claimed=false;return false;
    }
    public SkillScrollPickup DropReplacement(SkillData replaced,Health owner)
    {return Spawn(template!=null?template:Resources.Load<SkillScrollPickup>("SkillScrollPickup"),replaced,owner.transform.position+Vector3.right*.9f,owner);}
    public void Release(Health actor){claimed=false;availableAt=Time.time+.65f;}
    public void Consume(){if(consumed)return;consumed=true;gameObject.SetActive(false);Destroy(gameObject);}
}
