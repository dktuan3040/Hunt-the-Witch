using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// First-run hints are independent of temporary expedition inventory.
public sealed class FirstRunTutorial : MonoBehaviour
{
    private const string ProgressKey = "HuntTheWitch.TutorialProgress.v1";
    private static int ReadProgress()
    {
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("HuntTheWitch.TutorialTest", false))
            return UnityEditor.SessionState.GetInt("HuntTheWitch.TutorialTestProgress", 0);
#endif
        return PlayerPrefs.GetInt(ProgressKey, 0);
    }
    private static void WriteProgress(int progress)
    {
#if UNITY_EDITOR
        if (UnityEditor.SessionState.GetBool("HuntTheWitch.TutorialTest", false))
        {
            UnityEditor.SessionState.SetInt("HuntTheWitch.TutorialTestProgress", progress);
            return;
        }
#endif
        PlayerPrefs.SetInt(ProgressKey, progress);
        PlayerPrefs.Save();
    }
    private static bool eligible;
    private GameObject panel;
    private float remaining;
    private bool showing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        eligible = !File.Exists(GameProfile.SavePath) && !File.Exists(GameProfile.SavePath + ".bak")
            && ReadProgress() < 2;
        if (!eligible) return;
        var root = new GameObject("FirstRunTutorial");
        DontDestroyOnLoad(root);
        root.AddComponent<FirstRunTutorial>();
    }
    private void Awake() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (panel != null) Destroy(panel);
        showing = false;
        int progress = ReadProgress();
        bool house = scene.name == "Trong Nhà" && progress == 0;
        bool combat = (scene.name == "M_F1" || scene.name == "M_F2") && progress == 1;
        if (!eligible || (!house && !combat)) return;
        var canvasObject = new GameObject("TutorialCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        panel = canvasObject;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var backing = new GameObject("Hints", typeof(RectTransform), typeof(Image));
        backing.transform.SetParent(panel.transform, false);
        var rect = backing.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(28, 28); rect.sizeDelta = new Vector2(620, house ? 220 : 100);
        var image = backing.GetComponent<Image>(); image.color = new Color(.02f, .025f, .04f, .88f); image.raycastTarget = false;
        var labelObject = new GameObject("Instructions", typeof(RectTransform));
        labelObject.SetActive(false); labelObject.transform.SetParent(backing.transform, false);
        var text = labelObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.fontSize = 28; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        var tr = text.rectTransform; tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(14, 12); tr.offsetMax = new Vector2(-14, -12);
        text.text = house ? "MOVE\n        [ W / UP ]\n[ A / LEFT ]   [ S / DOWN ]   [ D / RIGHT ]\n[ SPACE ]  INTERACT" : "[ LEFT MOUSE BUTTON ]  SHOOT";
        labelObject.SetActive(true);
        remaining = house ? 18f : 12f; showing = true;
        WriteProgress(house ? 1 : 2);
    }
    private void Update()
    {
        if (!showing || Time.timeScale <= 0 || GameOverMenuUI.IsGameOver) return;
        remaining -= Time.deltaTime;
        if (remaining > 0) return;
        showing = false;
        if (panel != null) Destroy(panel);
    }
}
