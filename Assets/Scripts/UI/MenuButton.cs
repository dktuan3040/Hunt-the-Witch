using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum MenuButtonAction
{
    LoadScene, // chuyển sang scene được chọn
    QuitGame   // thoát game
}

/// <summary>
/// Gắn vào Button của menu. Chọn hành động ngay trên Inspector:
/// - LoadScene: KÉO THẢ scene vào ô "Scene Asset" để đổi scene đích (không cần sửa code, không sợ gõ sai tên).
///   Scene được chọn sẽ tự động thêm vào Build Settings nếu chưa có.
/// - QuitGame: thoát game (trong Editor thì dừng Play mode).
/// Màu hover / nhấn chỉnh ở component Button (Transition -> Color Tint).
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    [SerializeField] private MenuButtonAction action = MenuButtonAction.LoadScene;

#if UNITY_EDITOR
    [Tooltip("Kéo scene đích vào đây (chỉ dùng khi Action = LoadScene)")]
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;
#endif

    // Tên scene lưu lại để dùng khi chạy game/build (được cập nhật tự động từ sceneAsset)
    [SerializeField, HideInInspector] private string sceneName;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        switch (action)
        {
            case MenuButtonAction.LoadScene:
                LoadTargetScene();
                break;
            case MenuButtonAction.QuitGame:
                QuitGame();
                break;
        }
    }

    private void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning($"[MenuButton] '{name}' chưa được gán scene đích. Kéo scene vào ô Scene Asset.", this);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[MenuButton] Scene '{sceneName}' chưa có trong Build Settings (hoặc đang bị tắt).", this);
            return;
        }

        Time.timeScale = 1f; // phòng khi trước đó game đang pause
        SceneManager.LoadScene(sceneName);
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset == null)
        {
            sceneName = string.Empty;
            return;
        }

        sceneName = sceneAsset.name;
        EnsureSceneInBuildSettings(UnityEditor.AssetDatabase.GetAssetPath(sceneAsset));
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;

        var scenes = UnityEditor.EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path != scenePath) continue;

            // Đã có nhưng đang bị tắt thì bật lại
            if (!scenes[i].enabled)
            {
                scenes[i].enabled = true;
                UnityEditor.EditorBuildSettings.scenes = scenes;
            }
            return;
        }

        var list = new List<UnityEditor.EditorBuildSettingsScene>(scenes)
        {
            new UnityEditor.EditorBuildSettingsScene(scenePath, true)
        };
        UnityEditor.EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[MenuButton] Đã thêm '{scenePath}' vào Build Settings.");
    }
#endif
}
