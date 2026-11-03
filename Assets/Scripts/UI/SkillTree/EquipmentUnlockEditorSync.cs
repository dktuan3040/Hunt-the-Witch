#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public static class EquipmentUnlockEditorSync
{
    private static double next;
    private static string signature;
    static EquipmentUnlockEditorSync(){EditorApplication.update+=Poll;}
    private static void Poll()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+1;
        string current=string.Join("|",AssetDatabase.FindAssets("t:WeaponData t:PlayerSkinData",new[]{"Assets/Data"}).OrderBy(x=>x).Select(id=>id+EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)))));
        if(current==signature)return;signature=current;Synchronize();
    }
    public static void Synchronize()
    {
        var skins=AssetDatabase.FindAssets("t:PlayerSkinData",new[]{"Assets/Data"}).Select(id=>AssetDatabase.LoadAssetAtPath<PlayerSkinData>(AssetDatabase.GUIDToAssetPath(id))).Where(x=>x!=null).OrderBy(x=>x.SkinName).ToList();
        var staffs=AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/Data"}).Select(id=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(id))).Where(x=>x!=null).OrderBy(x=>x.WeaponName).ToList();
        foreach(var ui in Resources.FindObjectsOfTypeAll<EquipmentUnlockAutoUI>())
        {
            if(!ui.gameObject.scene.IsValid())continue;
            ui.skins=skins.ToList();ui.staffs=staffs.ToList();ui.Rebuild();
            EditorUtility.SetDirty(ui);EditorUtility.SetDirty(ui.panel);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
    }
}
public sealed class EquipmentUnlockBuildSync : IPreprocessBuildWithReport
{
    public int callbackOrder=>1;
    public void OnPreprocessBuild(BuildReport report){EquipmentUnlockEditorSync.Synchronize();}
}
#endif
