using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DemoBuildTools
{
    private const string PendingKey = "HuntTheWitch.DemoBuildPending";
    [InitializeOnLoadMethod]
    private static void ResumeScheduledBuild()
    {
        if (SessionState.GetBool(PendingKey, false)) EditorApplication.delayCall += Build;
    }
    [MenuItem("Tools/Hunt the Witch/Build Windows Demo")]
    public static void ScheduleBuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        File.WriteAllText("Library/DemoBuildReport.json", "{\"status\":\"scheduled\"}");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.delayCall += Build;
    }
    private static void Build()
    {
        SessionState.SetBool(PendingKey, false);
        File.WriteAllText("Library/DemoBuildReport.json", "{\"status\":\"building\"}");
        try
        {
            string folder = "Builds/Windows-Demo";
            Directory.CreateDirectory(folder);
            var options = new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/MainMenu.unity", ExpeditionFlow.House,
                    ExpeditionFlow.Home, ExpeditionFlow.FirstMap, "Assets/Scenes/Maps/M_F2.unity" },
                locationPathName = folder + "/Hunt the Witch Demo.exe",
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
                extraScriptingDefines = new[] { "HUNT_WITCH_DEMO" }
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            File.WriteAllText("Library/DemoBuildReport.json", JsonUtility.ToJson(new Result {
                status = summary.result.ToString(), path = summary.outputPath,
                errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings }));
            Debug.Log("Windows demo build: " + summary.result + " - " + summary.outputPath);
        }
        catch (Exception error)
        {
            File.WriteAllText("Library/DemoBuildReport.json", JsonUtility.ToJson(new Result {
                status = "Failed", message = error.Message }));
            Debug.LogException(error);
        }
    }
    [Serializable] private sealed class Result
    {
        public string status, path, message;
        public int errors, warnings;
    }
}
