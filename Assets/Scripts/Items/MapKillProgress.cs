using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapKillProgress
{
    private sealed class Counts
    {
        public readonly Dictionary<EnemyTypeData,int> byType=new Dictionary<EnemyTypeData,int>();
        public readonly HashSet<Health> counted=new HashSet<Health>();
    }
    private static readonly Dictionary<Scene,Counts> scenes=new Dictionary<Scene,Counts>();
    private static int generation;
    public static event Action<Scene> Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){scenes.Clear();Changed=null;generation=PlayerSessionState.Generation;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install(){generation=PlayerSessionState.Generation;SceneManager.sceneUnloaded-=OnUnloaded;SceneManager.sceneUnloaded+=OnUnloaded;}
    private static void OnUnloaded(Scene scene){scenes.Remove(scene);}
    private static void CheckSession(){if(generation==PlayerSessionState.Generation)return;scenes.Clear();generation=PlayerSessionState.Generation;}
    public static int GetCount(Scene scene,EnemyTypeData type)
    {CheckSession();Counts counts;int value;return type!=null&&scenes.TryGetValue(scene,out counts)&&counts.byType.TryGetValue(type,out value)?value:0;}
    public static bool RecordKill(Health enemy,EnemyTypeData type)
    {
        CheckSession();if(enemy==null||type==null||enemy.Team!=Team.Enemy||!enemy.IsDead||!enemy.gameObject.scene.IsValid())return false;
        Scene scene=enemy.gameObject.scene;Counts counts;if(!scenes.TryGetValue(scene,out counts)){counts=new Counts();scenes[scene]=counts;}
        if(!counts.counted.Add(enemy))return false;
        int old;counts.byType.TryGetValue(type,out old);counts.byType[type]=old==int.MaxValue?old:old+1;
        Changed?.Invoke(scene);return true;
    }
}
