using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="NewLootTable",menuName="Hunt the Witch/Items/Loot Table")]
public sealed class LootTableData : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public ItemData item;
        [Tooltip("Final chance = item's rarity base chance x multiplier.")]
        [Min(0)] public float chanceMultiplier=1;
        [Range(1,999)] public int minAmount=1,maxAmount=3;
        [Tooltip("Each next quantity has this fraction of the previous quantity's weight.")]
        [Range(.01f,.99f)] public float quantityDecay=.5f;
        public int RollAmount(System.Random random)
        {
            int min=Mathf.Clamp(minAmount,1,999),max=Mathf.Clamp(maxAmount,min,999);
            double ratio=Mathf.Clamp(quantityDecay,.01f,.99f),sum=(1-Math.Pow(ratio,max-min+1))/(1-ratio);
            double roll=random.NextDouble()*sum,weight=1;
            for(int n=min;n<max;n++){if(roll<weight)return n;roll-=weight;weight*=ratio;}return max;
        }
    }
    [Serializable] public struct Stack
    {
        public ItemData item;public int amount;
        public Stack(ItemData i,int n){item=i;amount=n;}
    }
    public List<Entry> entries=new List<Entry>();
    public bool guaranteeAtLeastOne;
    [Header("Extremely rare unlocked skill scroll")]
    public SkillScrollLootData scrollLoot;
    [Range(0,1)] public float skillScrollChance=.001f;
    public List<Stack> Roll(System.Random random)
    {
        if(random==null)throw new ArgumentNullException(nameof(random));
        var result=new List<Stack>();var eligible=new List<Entry>();
        foreach(var e in entries)
        {
            if(e==null||e.item==null||e.chanceMultiplier<=0)continue;eligible.Add(e);
            if(random.NextDouble()<Mathf.Clamp01(e.item.BaseDropChance*e.chanceMultiplier))result.Add(new Stack(e.item,e.RollAmount(random)));
        }
        if(result.Count==0&&guaranteeAtLeastOne&&eligible.Count>0)
        {
            double total=0;foreach(var e in eligible)total+=Mathf.Clamp01(e.item.BaseDropChance*e.chanceMultiplier);
            double roll=random.NextDouble()*total;
            foreach(var e in eligible){roll-=Mathf.Clamp01(e.item.BaseDropChance*e.chanceMultiplier);if(roll<=0){result.Add(new Stack(e.item,e.RollAmount(random)));break;}}
        }
        return result;
    }
}
