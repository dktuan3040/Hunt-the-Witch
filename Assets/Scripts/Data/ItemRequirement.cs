using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public sealed class ItemRequirement
{
    public ItemData item;
    [Min(1)] public int amount=1;
}
public static class ItemUnlockProgress
{
    private static readonly HashSet<ScriptableObject> unlocked=new HashSet<ScriptableObject>();
    private static int generation=-1;
    private static bool spending;
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){unlocked.Clear();generation=-1;Changed=null;}
    private static void CheckSession(){if(generation==PlayerSessionState.Generation)return;generation=PlayerSessionState.Generation;unlocked.Clear();}
    private static bool Costs(IReadOnlyList<ItemRequirement> requirements,out Dictionary<string,int> totals)
    {
        totals=new Dictionary<string,int>();if(requirements==null)return false;
        foreach(var r in requirements){if(r==null||r.item==null||r.amount<1)return false;int old;totals.TryGetValue(r.item.ItemId,out old);long sum=(long)old+r.amount;if(sum>int.MaxValue)return false;totals[r.item.ItemId]=(int)sum;}
        return true;
    }
    public static bool IsUnlocked(ScriptableObject data,IReadOnlyList<ItemRequirement> costs)
    {CheckSession();return data!=null&&(costs!=null&&costs.Count==0||unlocked.Contains(data));}
    public static bool CanUnlock(ScriptableObject data,IReadOnlyList<ItemRequirement> requirements)
    {if(spending||data==null||IsUnlocked(data,requirements))return false;Dictionary<string,int> costs;if(!Costs(requirements,out costs))return false;foreach(var p in costs)if(PlayerSessionState.GetItemCount(p.Key)<p.Value)return false;return true;}
    public static bool TryUnlock(ScriptableObject data,IReadOnlyList<ItemRequirement> requirements)
    {
        if(!CanUnlock(data,requirements))return false;Dictionary<string,int> costs;
        if(!Costs(requirements,out costs))return false;
        spending=true;
        try {if(!PlayerSessionState.TrySpendItems(costs))return false;unlocked.Add(data);}
        finally {spending=false;}
        Changed?.Invoke();return true;
    }
}
