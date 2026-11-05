using System.Linq;
using NUnit.Framework;
using UnityEditor;

public class PortalTests
{
    [TestCase("Assets/Scenes/Maps/Home.unity")]
    [TestCase("Assets/Scenes/Maps/M_F1.unity")]
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
}