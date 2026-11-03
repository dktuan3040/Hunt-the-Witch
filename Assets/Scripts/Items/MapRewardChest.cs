using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(RewardChest)),DisallowMultipleComponent]
public sealed class MapRewardChest : MonoBehaviour
{
    [Serializable] public sealed class KillRequirement
    {
        public EnemyTypeData enemyType;
        [Min(1)] public int amount=1;
    }
    public List<KillRequirement> requiredKills=new List<KillRequirement>();
    [Header("Drop and left/right landing")]
    [Min(.1f)] public float fallHeight=3;
    [Min(.05f)] public float fallDuration=.4f;
    [Range(1,40)] public float leftTilt=18,rightTilt=14;
    public UnityEvent onLanded=new UnityEvent();
    public bool HasSpawned {get;private set;}
    public bool IsLanding {get;private set;}
    public bool IsLanded => HasSpawned&&!IsLanding;
    private RewardChest chest;
    private Renderer[] visuals;
    private Collider2D[] colliders;
    private bool[] rendererEnabled,colliderEnabled;
    private Vector3 groundPosition;
    private Quaternion groundRotation;
    private float halfWidth=.25f,halfHeight=.25f;
    private bool initialized;
    private void Awake()
    {
        chest=GetComponent<RewardChest>();groundPosition=transform.position;groundRotation=transform.rotation;
        visuals=GetComponentsInChildren<Renderer>(true);colliders=GetComponentsInChildren<Collider2D>(true);
        rendererEnabled=new bool[visuals.Length];colliderEnabled=new bool[colliders.Length];
        for(int i=0;i<visuals.Length;i++){rendererEnabled[i]=visuals[i].enabled;var sr=visuals[i] as SpriteRenderer;if(sr!=null&&sr.sprite!=null){halfWidth=Mathf.Max(.01f,sr.sprite.bounds.extents.x*Mathf.Abs(sr.transform.lossyScale.x));halfHeight=Mathf.Max(.01f,sr.sprite.bounds.extents.y*Mathf.Abs(sr.transform.lossyScale.y));}}
        for(int i=0;i<colliders.Length;i++)colliderEnabled[i]=colliders[i].enabled;
        initialized=true;chest.SetAvailable(false);SetVisuals(false);SetColliders(false);
    }
    private void OnEnable(){MapKillProgress.Changed+=OnKillProgress;if(initialized)TryReveal();}
    private void Start(){TryReveal();}
    private void OnDisable()
    {
        MapKillProgress.Changed-=OnKillProgress;
        if(initialized&&IsLanding){StopAllCoroutines();CompleteLanding();}
    }
    private void OnKillProgress(UnityEngine.SceneManagement.Scene scene){if(scene==gameObject.scene)TryReveal();}
    public bool RequirementsMet
    {
        get
        {
            if(requiredKills.Count==0)return false;
            var totals=new Dictionary<EnemyTypeData,long>();
            foreach(var r in requiredKills){if(r==null||r.enemyType==null)return false;if(!totals.ContainsKey(r.enemyType))totals[r.enemyType]=0;totals[r.enemyType]+=Mathf.Max(1,r.amount);}
            foreach(var p in totals)if(MapKillProgress.GetCount(gameObject.scene,p.Key)<p.Value)return false;
            return true;
        }
    }
    public bool TryReveal()
    {
        if(!initialized||HasSpawned||!isActiveAndEnabled||!RequirementsMet)return false;
        HasSpawned=true;IsLanding=true;SetVisuals(true);StartCoroutine(Land());return true;
    }
    private void SetVisuals(bool show){for(int i=0;i<visuals.Length;i++)if(visuals[i]!=null)visuals[i].enabled=show&&rendererEnabled[i];}
    private void SetColliders(bool show){for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=show&&colliderEnabled[i];}
    private float ContactHeight(float angle){float a=angle*Mathf.Deg2Rad;return halfHeight*(Mathf.Cos(a)-1)+halfWidth*Mathf.Abs(Mathf.Sin(a));}
    private void Pose(float x,float y,float angle){transform.position=groundPosition+new Vector3(x,y,0);transform.rotation=groundRotation*Quaternion.Euler(0,0,angle);}
    private IEnumerator Land()
    {
        yield return Segment(new Vector2(-.35f,fallHeight),new Vector2(0,ContactHeight(leftTilt)),-8,leftTilt,fallDuration,0,true);
        yield return Segment(new Vector2(0,ContactHeight(leftTilt)),new Vector2(0,ContactHeight(-rightTilt)),leftTilt,-rightTilt,.2f,.22f,false);
        yield return Segment(new Vector2(0,ContactHeight(-rightTilt)),new Vector2(0,ContactHeight(5)),-rightTilt,5,.14f,.06f,false);
        yield return Segment(new Vector2(0,ContactHeight(5)),Vector2.zero,5,0,.12f,0,false);
        CompleteLanding();
    }
    private IEnumerator Segment(Vector2 from,Vector2 to,float a,float b,float duration,float bounce,bool accelerate)
    {
        float elapsed=0;while(elapsed<duration){float t=Mathf.Clamp01(elapsed/duration),u=accelerate?t*t:t;Vector2 p=Vector2.Lerp(from,to,u);p.y+=Mathf.Sin(t*Mathf.PI)*bounce;Pose(p.x,p.y,Mathf.Lerp(a,b,u));elapsed+=Time.deltaTime;yield return null;}Pose(to.x,to.y,b);
    }
    private void CompleteLanding(){Pose(0,0,0);IsLanding=false;SetVisuals(true);SetColliders(true);chest.SetAvailable(true);onLanded.Invoke();}
}
