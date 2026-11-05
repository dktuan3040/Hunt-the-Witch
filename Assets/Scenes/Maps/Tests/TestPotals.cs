using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTests
{
    [TestCase("Assets/Scenes/Maps/Home.unity")]
    [TestCase("Assets/Scenes/Maps/M_F1.unity")]
    [TestCase("Assets/Scenes/Maps/M_F2.unity")]
    [TestCase("Assets/Scenes/Maps/Trong Nhà.unity")]
    [TestCase("Assets/Scenes/MainMenu.unity")]
    public void PortalDestination_IsEnabled(string scenePath)
    {
        bool enabled = EditorBuildSettings.scenes.Any(
            scene => scene.path == scenePath && scene.enabled
        );

        Assert.That(
            enabled,
            Is.True,
            $"Map chưa được bật trong Build Profiles: {scenePath}"
        );
    }

    [TestCase("Assets/Scenes/Maps/Home.unity", "M_F1", false)]
    [TestCase("Assets/Scenes/Maps/M_F1.unity", "M_F2", true)]
    [TestCase("Assets/Scenes/Maps/M_F2.unity", "Home", true)]
    public void Map_HasOnePortal_WithCorrectDestinationAndBossGate(string scenePath, string destination, bool bossGated)
    {
        var original = SceneManager.GetActiveScene();
        var scene = SceneManager.GetSceneByPath(scenePath);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        try
        {
            // Game scripts are in Assembly-CSharp; inspect serialized components without
            // moving them into the test assembly or changing the game's assembly layout.
            var scripts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Where(script => script != null).ToArray();
            var portals = scripts.Where(script => script.GetType().Name == "ExpeditionPortal").ToArray();
            Assert.That(portals.Length, Is.EqualTo(1), "Map phải có đúng một cổng.");
            Assert.That(scripts.Any(script => script.GetType().Name == "ScenePortal"), Is.False,
                "Map phải dùng chung luồng chuyển cảnh và lưu đồ.");
            var portal = new SerializedObject(portals[0]);
            string target = portal.FindProperty("destination").stringValue;
            Assert.That(System.IO.Path.GetFileNameWithoutExtension(target), Is.EqualTo(destination));
            Assert.That(portal.FindProperty("requireBossDefeated").boolValue, Is.EqualTo(bossGated));
            Assert.That(portal.FindProperty("fitTriggerToVisual").boolValue, Is.True);
            if (bossGated)
            {
                var gate = portal.FindProperty("bossGate").objectReferenceValue;
                Assert.That(gate, Is.Not.Null);
                Assert.That(new SerializedObject(gate).FindProperty("boss").objectReferenceValue, Is.Not.Null);
                var chest = scripts.Single(script => script.GetType().Name == "MapRewardChest");
                Assert.That(chest.gameObject.activeInHierarchy, Is.True, "Rương phải hoạt động để theo dõi quái còn lại.");
            }
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(original);
        }
    }

    [Test]
    public void RewardChestPrefab_IsActive_ForMapClearControllerToRun()
    {
        var chest = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/RewardChest.prefab");
        Assert.That(chest, Is.Not.Null);
        Assert.That(chest.activeSelf, Is.True);
    }
}
