using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Only banked loot, unlocks and chosen equipment reach this file. No expedition state.
public static class GameProfile
{
    [Serializable] public sealed class Item {public string id;public int amount;}
    [Serializable] private sealed class SaveData
    {
        public int version=1;
        public List<Item> bank=new List<Item>();
        public List<string> unlocked=new List<string>();
        public string weaponId,skinId;
    }
    private static SaveData data;
    private static string testPath;
    private static bool writable=true;
#if HUNT_WITCH_DEMO
    private const string ProfileFileName = "profile-demo-v1.json";
#else
    private const string ProfileFileName = "profile-v1.json";
#endif
    public static string SavePath => string.IsNullOrEmpty(testPath)?Path.Combine(Application.persistentDataPath,ProfileFileName):testPath;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        data=null;testPath=null;writable=true;
#if UNITY_EDITOR
        testPath=UnityEditor.SessionState.GetString("HuntTheWitch.ProfileTestPath","");
#endif
    }
#if UNITY_EDITOR
    public static void SetTestSavePath(string path){testPath=path;data=null;writable=true;UnityEditor.SessionState.SetString("HuntTheWitch.ProfileTestPath",path??"");}
#endif
    private static bool Read(string path,out SaveData result)
    {
        result=null;
        try {if(!File.Exists(path))return false;result=JsonUtility.FromJson<SaveData>(File.ReadAllText(path));return result!=null&&result.version==1&&result.bank!=null&&result.unlocked!=null;}
        catch(Exception e){Debug.LogWarning("Cannot read save: "+e.Message);return false;}
    }
    private static void Load()
    {
        if(data!=null)return;
        SaveData loaded;
        if(Read(SavePath,out loaded)||Read(SavePath+".bak",out loaded))data=loaded;
        else
        {
            data=new SaveData();
            // Never silently overwrite an unreadable existing save.
            writable=!File.Exists(SavePath)&&!File.Exists(SavePath+".bak");
            if(!writable)Debug.LogError("Save could not be read. Original files have been kept at "+SavePath);
        }
        data.bank.RemoveAll(i=>i==null||string.IsNullOrWhiteSpace(i.id)||i.amount<=0);
#if HUNT_WITCH_DEMO
        EnsureDemoMaterials();
#endif
    }
#if UNITY_EDITOR || HUNT_WITCH_DEMO
    // Restock only the dedicated demo profile; never unlock equipment automatically.
    public static bool EnsureDemoMaterials()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(testPath))
            throw new InvalidOperationException("Select a test profile before granting demo materials in the Editor.");
#endif
        Load();
        var totals = DemoUnlockMaterials.RequiredTotals(GameContentCatalog.Instance);
        bool changed = false;
        foreach (var pair in totals)
        {
            int target = Math.Max(1000, pair.Value);
            if (StoredCount(pair.Key) >= target) continue;
            SetCount(pair.Key, target); changed = true;
        }
        return !changed || Save();
    }
#endif
    private static bool Save()
    {
        if(!writable)return false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temporary=SavePath+".tmp";
            File.WriteAllText(temporary,JsonUtility.ToJson(data,true));
            if(File.Exists(SavePath))File.Replace(temporary,SavePath,SavePath+".bak");
            else File.Move(temporary,SavePath);
            return true;
        }
        catch(Exception e){Debug.LogError("Could not save progress: "+e.Message);return false;}
    }
    public static int StoredCount(string id){Load();var item=data.bank.Find(i=>i.id==id);return item!=null?item.amount:0;}
    public static IReadOnlyList<Item> Bank {get{Load();return data.bank;}}
    public static bool IsUnlocked(string id){Load();return data.unlocked.Contains(id);}
    public static string EquipmentId(ScriptableObject item)
    {
        var w=item as WeaponData;if(w!=null)return "weapon:"+w.PersistentId;
        var s=item as PlayerSkinData;if(s!=null)return "skin:"+s.PersistentId;
        return null;
    }
    private static void SetCount(string id,int amount)
    {
        var item=data.bank.Find(i=>i.id==id);
        if(item==null){if(amount>0)data.bank.Add(new Item{id=id,amount=amount});}
        else if(amount<=0)data.bank.Remove(item);else item.amount=amount;
    }
    private static bool Transaction(Action change)
    {
        Load();string before=JsonUtility.ToJson(data);change();
        if(Save())return true;
        data=JsonUtility.FromJson<SaveData>(before);return false;
    }
    public static bool Deposit(IReadOnlyDictionary<string,int> items)
    {
        Load();foreach(var pair in items)if(pair.Value<0||(long)StoredCount(pair.Key)+pair.Value>int.MaxValue)return false;
        return Transaction(()=>{foreach(var pair in items)SetCount(pair.Key,StoredCount(pair.Key)+pair.Value);});
    }
    public static bool TryUnlock(string id,IReadOnlyDictionary<string,int> costs)
    {
        Load();if(string.IsNullOrWhiteSpace(id)||IsUnlocked(id)||costs==null)return false;
        foreach(var pair in costs)if(pair.Value<0||StoredCount(pair.Key)<pair.Value)return false;
        return Transaction(()=>{foreach(var pair in costs)SetCount(pair.Key,StoredCount(pair.Key)-pair.Value);data.unlocked.Add(id);});
    }
    public static bool GrantUnlock(string id)
    {
        Load();if(IsUnlocked(id))return true;
        return Transaction(()=>data.unlocked.Add(id));
    }
    public static WeaponData SelectedWeapon {get{Load();return GameContentCatalog.Instance!=null?GameContentCatalog.Instance.Weapon(data.weaponId):null;}}
    public static PlayerSkinData SelectedSkin {get{Load();return GameContentCatalog.Instance!=null?GameContentCatalog.Instance.Skin(data.skinId):null;}}
    public static void SelectWeapon(WeaponData weapon)
    {
        Load();if(weapon!=null&&weapon.IsUnlocked&&data.weaponId!=weapon.PersistentId)Transaction(()=>data.weaponId=weapon.PersistentId);
    }
    public static void SelectSkin(PlayerSkinData skin)
    {
        Load();if(skin!=null&&skin.IsUnlocked&&data.skinId!=skin.PersistentId)Transaction(()=>data.skinId=skin.PersistentId);
    }
}
