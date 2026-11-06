using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FirstRunTutorialTools
{
    private const string TestFlag = "HuntTheWitch.TutorialTest";
    private const string ProfileKey = "HuntTheWitch.ProfileTestPath";
    private const string PreviousProfile = "HuntTheWitch.TutorialPreviousProfile";

    static FirstRunTutorialTools()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Hunt the Witch/Tutorial/Start First Run Test")]
    public static void StartTest()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetString(PreviousProfile, SessionState.GetString(ProfileKey, ""));
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/TutorialTests"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".json");
        GameProfile.SetTestSavePath(path);
        SessionState.SetBool(TestFlag, true);
        SessionState.SetInt("HuntTheWitch.TutorialTestProgress", 0);
        EditorSceneManager.OpenScene(ExpeditionFlow.House, OpenSceneMode.Single);
        Debug.Log("Tutorial test: fresh temporary profile. Real save and tutorial preferences are unchanged. Stop Play Mode to return to the normal profile.");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/Hunt the Witch/Tutorial/Start First Run Test", true)]
    private static bool CanStartTest() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(TestFlag, false)) return;
        GameProfile.SetTestSavePath(SessionState.GetString(PreviousProfile, ""));
        SessionState.SetBool(TestFlag, false);
        SessionState.EraseInt("HuntTheWitch.TutorialTestProgress");
        SessionState.EraseString(PreviousProfile);
        Debug.Log("Tutorial test finished. Normal profile restored.");
    }
}
