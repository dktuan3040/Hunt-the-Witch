using UnityEngine;

public class SpreadFireball : MonoBehaviour
{
    public GameObject fireBallPrefab;
    [Range(1,128)] public int numberOfFireBall=12;
    public float fireBallspeed=5f;
    public float damage=1f;
    [Min(.1f)] public float projectileLifeTime=2.6f;
    public void CastRadialFireBalls()
    {
        var owner=GetComponent<Health>();
        if(!isActiveAndEnabled||fireBallPrefab==null||(owner!=null&&owner.IsDead))return;
        if(TryGetComponent<EnemySkillStatus>(out var status)&&status.Immobilized)return;
        int count=Mathf.Clamp(numberOfFireBall,1,128);
        var ownerColliders=GetComponentsInChildren<Collider2D>();
        for(int i=0;i<count;i++)
        {
            var rotation=Quaternion.Euler(0,0,i*360f/count);
            var direction=rotation*Vector3.right;
            var ball=Instantiate(fireBallPrefab,transform.position+direction*.4f,rotation);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ball,gameObject.scene);
            var projectile=ball.GetComponent<Projectile>();
            if(projectile!=null){projectile.SetTeam(Team.Enemy);projectile.Initialize(direction,1f,null,fireBallspeed,projectileLifeTime,Mathf.Max(1,Mathf.RoundToInt(damage)));}
            foreach(var shotCollider in ball.GetComponentsInChildren<Collider2D>())
                foreach(var body in ownerColliders)Physics2D.IgnoreCollision(shotCollider,body);
        }
    }
}
