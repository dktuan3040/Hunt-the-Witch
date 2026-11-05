using UnityEngine;

[DefaultExecutionOrder(-300),DisallowMultipleComponent]
public sealed class ExpeditionScene : MonoBehaviour
{
    public enum SceneKind {House,Hub,Combat,Menu}
    public SceneKind kind;
    public GameObject combatHudPrefab;
    public GameObject gameOverMenuPrefab;
    private void Awake()
    {
        ExpeditionFlow.EnterScene(this);
        if(kind==SceneKind.Combat&&combatHudPrefab!=null)Instantiate(combatHudPrefab);
        if(kind==SceneKind.Combat)gameObject.AddComponent<EnemyCountUI>();
        if (kind != SceneKind.Menu && gameOverMenuPrefab != null)
        {
            var root = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            Instantiate(gameOverMenuPrefab, root.transform, false);
        }
    }
}
