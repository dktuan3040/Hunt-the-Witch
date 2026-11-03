#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
public static class SkillTreeCatalogEditorSync
{
    public const string CatalogPath="Assets/Art/UI/Skill/SkillTreeUI/SkillTreeCatalog.asset";
    private static bool queued, synchronizing;
    private static double nextCapture, nextScan;
    private static string dataSignature;
    static SkillTreeCatalogEditorSync()
    {
        Queue();EditorApplication.projectChanged+=Queue;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened+=(scene,mode)=>Queue();
        Undo.undoRedoPerformed+=Restore;
        EditorApplication.update+=CapturePositions;EditorApplication.update+=DetectDataChanges;EditorApplication.update+=RunQueued;
    }
    public static void Queue()
    {
        if(queued||synchronizing||EditorApplication.isPlayingOrWillChangePlaymode)return;
        queued=true;
    }
    private static void RunQueued()
    {
        if(!queued||synchronizing||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        queued=false;Synchronize();
    }
    private static SkillTreeAutoUI[] Panels()
    { return Resources.FindObjectsOfTypeAll<SkillTreeAutoUI>().Where(p=>p.gameObject.scene.IsValid()&&p.IsReady).ToArray(); }
    public static void Synchronize()
    {
        if(synchronizing||EditorApplication.isPlayingOrWillChangePlaymode)return;
        var catalog=AssetDatabase.LoadAssetAtPath<SkillTreeCatalog>(CatalogPath);
        if(catalog==null)return;
        synchronizing=true;
        try
        {
            var skills=AssetDatabase.FindAssets("t:SkillData",new[]{"Assets/Data"}).OrderBy(g=>g,StringComparer.Ordinal)
                .Select(g=>new {guid=g,skill=AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))}).Where(x=>x.skill!=null).ToArray();
            var recipes=AssetDatabase.FindAssets("t:SkillSynergyData",new[]{"Assets/Data"})
                .Select(g=>AssetDatabase.LoadAssetAtPath<SkillSynergyData>(AssetDatabase.GUIDToAssetPath(g))).Where(r=>r!=null).ToArray();
            string before=EditorJsonUtility.ToJson(catalog);
            var old=catalog.entries.ToDictionary(e=>e.assetGuid,e=>e);
            var entries=new List<SkillTreeCatalog.Entry>();
            var occupied=catalog.entries.Where(e=>skills.Any(s=>s.guid==e.assetGuid)).Select(e=>e.position).ToList();
            foreach(var s in skills)
            {
                SkillTreeCatalog.Entry entry;
                if(!old.TryGetValue(s.guid,out entry))
                {
                    Vector2 p=Vector2.zero;
                    for(int i=0;i<100000;i++)
                    {
                        p=new Vector2(386+(i%6)*160,-286-(i/6)*140);
                        if(!occupied.Any(v=>Mathf.Abs(v.x-p.x)<100&&Mathf.Abs(v.y-p.y)<100))break;
                    }
                    entry=new SkillTreeCatalog.Entry{assetGuid=s.guid,position=p};occupied.Add(p);
                }
                entry.skill=s.skill;entry.recipe=null;entry.invalid=false;entries.Add(entry);
            }
            var errors=new List<string>();
            foreach(var group in entries.GroupBy(e=>e.skill.SkillId).Where(g=>g.Count()>1))
            { errors.Add("Duplicate SkillId: "+group.Key+" ("+string.Join(", ",group.Select(e=>AssetDatabase.GetAssetPath(e.skill)))+")");foreach(var e in group)e.invalid=true; }
            foreach(var recipe in recipes)
            {
                var target=entries.FirstOrDefault(e=>e.skill==recipe.ResultingSynergySkill);
                var matching=recipes.Count(r=>r.ResultingSynergySkill==recipe.ResultingSynergySkill);
                bool valid=target!=null&&matching==1&&recipe.RequiredSkills!=null&&recipe.RequiredSkills.Count>=2&&recipe.RequiredSkills.Distinct().Count()==recipe.RequiredSkills.Count
                    &&recipe.RequiredSkills.All(s=>s!=null&&s!=recipe.ResultingSynergySkill&&entries.Any(e=>e.skill==s&&!e.invalid));
                if(!valid){errors.Add("Invalid recipe: "+AssetDatabase.GetAssetPath(recipe)+" (missing/duplicate result or prerequisites)");if(target!=null)target.invalid=true;}
                else target.recipe=recipe;
            }
            // A flagged synergy without a usable recipe must never become a free ordinary spell.
            foreach(var e in entries.Where(e=>e.skill.IsSynergySkill&&e.recipe==null))
            {e.invalid=true;errors.Add("Synergy has no valid recipe: "+AssetDatabase.GetAssetPath(e.skill));}
            foreach(var e in entries.Where(e=>e.invalid))e.recipe=null;
            catalog.entries=entries;catalog.validationErrors=string.Join("\n",errors);
            if(before!=EditorJsonUtility.ToJson(catalog))
            {EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);if(errors.Count>0)Debug.LogWarning(catalog.validationErrors,catalog);}
            foreach(var p in Panels()) {p.Rebuild();EditorUtility.SetDirty(p.panel);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(p.gameObject.scene);}
        }
        finally{synchronizing=false;}
    }
    private static void DetectDataChanges()
    {
        if(synchronizing||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<nextScan)return;
        nextScan=EditorApplication.timeSinceStartup+1;
        var ids=AssetDatabase.FindAssets("t:SkillData t:SkillSynergyData t:ItemData",new[]{"Assets/Data"}).OrderBy(x=>x);
        var signature=string.Join("|",ids.Select(id=>id+EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)))));
        if(signature!=dataSignature){dataSignature=signature;Queue();}
    }
    private static void Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        foreach(var p in Panels())p.Rebuild();
    }
    private static void CapturePositions()
    {
        if(synchronizing||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.timeSinceStartup<nextCapture)return;
        nextCapture=EditorApplication.timeSinceStartup+.2;
        foreach(var p in Panels())
        {
            bool changed=false;
            foreach(var node in p.nodesRoot.GetComponentsInChildren<SkillTreeNodeView>(true))
            {
                var entry=p.catalog.entries.FirstOrDefault(e=>e.assetGuid==node.assetGuid);if(entry==null)continue;
                var pos=((RectTransform)node.transform).anchoredPosition;
                if(Vector2.Distance(pos,entry.position)<.01f)continue;
                if(!changed)Undo.RecordObject(p.catalog,"Move skill tree node");
                entry.position=pos;changed=true;
            }
            if(changed){EditorUtility.SetDirty(p.catalog);AssetDatabase.SaveAssetIfDirty(p.catalog);}
        }
    }
}
public sealed class SkillTreeDataPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] from)
    {
        if(imported.Concat(deleted).Concat(moved).Concat(from).Any(p=>p.StartsWith("Assets/Data/",StringComparison.Ordinal)))SkillTreeCatalogEditorSync.Queue();
    }
}
public sealed class SkillTreeBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder=>0;
    public void OnPreprocessBuild(BuildReport report)
    {
        SkillTreeCatalogEditorSync.Synchronize();
        var c=AssetDatabase.LoadAssetAtPath<SkillTreeCatalog>(SkillTreeCatalogEditorSync.CatalogPath);
        if(c!=null&&!string.IsNullOrEmpty(c.validationErrors))throw new BuildFailedException(c.validationErrors);
    }
}
#endif

