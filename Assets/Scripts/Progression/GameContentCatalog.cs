using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName="Hunt the Witch/Game Content Catalog")]
public sealed class GameContentCatalog : ScriptableObject
{
    public List<ItemData> items=new List<ItemData>();
    public List<WeaponData> weapons=new List<WeaponData>();
    public List<PlayerSkinData> skins=new List<PlayerSkinData>();
    public List<SkillData> skills=new List<SkillData>();
    public List<SkillData> starterSkills=new List<SkillData>();
    private static GameContentCatalog instance;
    public static GameContentCatalog Instance => instance!=null?instance:instance=Resources.Load<GameContentCatalog>("GameContentCatalog");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){instance=null;}
    public WeaponData Weapon(string id)=>weapons.Find(w=>w!=null&&w.PersistentId==id);
    public PlayerSkinData Skin(string id)=>skins.Find(s=>s!=null&&s.PersistentId==id);
    public string ItemName(string id){var item=items.Find(i=>i!=null&&i.ItemId==id);return item!=null?item.DisplayName:id;}
}
