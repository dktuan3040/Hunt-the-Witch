using System.Collections.Generic;
using UnityEngine;

public static class LootSpawner
{
    private static readonly System.Random random=new System.Random();
    public static List<ItemPickup> Spawn(LootTableData table,ItemPickup prefab,Vector3 position,float scatterRadius=.6f)
    {
        var spawned=new List<ItemPickup>();if(table==null||prefab==null)return spawned;
        foreach(var stack in table.Roll(random))
        {
            Vector2 offset=Random.insideUnitCircle*Mathf.Max(0,scatterRadius);
            var pickup=Object.Instantiate(prefab,position+(Vector3)offset,Quaternion.identity);
            pickup.Initialize(stack.item,stack.amount);pickup.gameObject.SetActive(true);spawned.Add(pickup);
        }
        if(table.scrollLoot!=null)table.scrollLoot.TrySpawn(table.skillScrollChance,random,position+(Vector3)(Random.insideUnitCircle*.5f));
        return spawned;
    }
}
