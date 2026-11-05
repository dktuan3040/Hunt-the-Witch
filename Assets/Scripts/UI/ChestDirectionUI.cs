using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(MapRewardChest)), DisallowMultipleComponent]
public sealed class ChestDirectionUI : MonoBehaviour
{
    private MapRewardChest mapChest;
    private RewardChest chest;
    private Health player;
    private Camera worldCamera;
    private GameObject root;
    private RectTransform canvasRect, arrow;
    private Canvas canvas;
    private Texture2D arrowTexture;
    private Sprite arrowSprite;
    private float nextPlayerSearch;

    public bool IsVisible => root != null && root.activeSelf;

    private void Awake()
    {
        mapChest = GetComponent<MapRewardChest>();
        chest = GetComponent<RewardChest>();
    }

    private void LateUpdate()
    {
        if (!mapChest.isActiveAndEnabled || !mapChest.HasSpawned || chest.IsOpened || ExpeditionFlow.IsLoading)
        {
            Hide();
            return;
        }
        if (player == null && Time.unscaledTime >= nextPlayerSearch)
        {
            nextPlayerSearch = Time.unscaledTime + .5f;
            foreach (var actor in FindObjectsByType<Health>(FindObjectsSortMode.None))
                if (actor.Team == Team.Player && actor.gameObject.scene == gameObject.scene)
                {
                    player = actor;
                    break;
                }
        }
        if (worldCamera == null || !worldCamera.isActiveAndEnabled) worldCamera = Camera.main;
        if (player == null || player.IsDead || worldCamera == null)
        {
            Hide();
            return;
        }
        if (root == null) CreateUI();
        root.SetActive(true);

        // Stay inside the camera viewport and device safe area at every resolution.
        var viewport = worldCamera.pixelRect;
        var safe = Screen.safeArea;
        float scale = canvas.scaleFactor;
        float margin = Mathf.Min(60 * scale, Mathf.Min(viewport.width, viewport.height) * .2f);
        var bounds = Rect.MinMaxRect(Mathf.Max(viewport.xMin, safe.xMin) + margin,
            Mathf.Max(viewport.yMin, safe.yMin) + margin,
            Mathf.Min(viewport.xMax, safe.xMax) - margin,
            Mathf.Min(viewport.yMax, safe.yMax) - margin);
        Vector3 projected = worldCamera.WorldToScreenPoint(transform.position);
        Vector2 target = projected;
        Vector2 position = target + Vector2.up * (54 + Mathf.Sin(Time.unscaledTime * 3) * 4) * scale;
        float angle = 180; // Visible chest: point down at it.
        if (projected.z <= 0 || !bounds.Contains(position))
        {
            Vector3 playerScreen = worldCamera.WorldToScreenPoint(player.transform.position);
            Vector2 origin = playerScreen.z > 0 ? (Vector2)playerScreen : bounds.center;
            origin.x = Mathf.Clamp(origin.x, bounds.xMin, bounds.xMax);
            origin.y = Mathf.Clamp(origin.y, bounds.yMin, bounds.yMax);
            Vector2 direction = target - origin;
            if (projected.z <= 0) direction = -direction;
            if (direction.sqrMagnitude < .001f) direction = Vector2.up;
            direction.Normalize();
            float tx = Mathf.Abs(direction.x) < .001f ? float.PositiveInfinity
                : ((direction.x > 0 ? bounds.xMax : bounds.xMin) - origin.x) / direction.x;
            float ty = Mathf.Abs(direction.y) < .001f ? float.PositiveInfinity
                : ((direction.y > 0 ? bounds.yMax : bounds.yMin) - origin.y) / direction.y;
            position = origin + direction * Mathf.Min(tx, ty);
            angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;
        }
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, position, null, out local);
        arrow.anchoredPosition = local;
        arrow.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void CreateUI()
    {
        root = new GameObject("ChestDirectionHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        canvasRect = root.GetComponent<RectTransform>();
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        var marker = new GameObject("ChestArrow", typeof(RectTransform), typeof(Image));
        marker.transform.SetParent(root.transform, false);
        arrow = marker.GetComponent<RectTransform>();
        arrow.sizeDelta = new Vector2(54, 72);
        var image = marker.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = CreateArrowSprite();
    }

    private Sprite CreateArrowSprite()
    {
        // Small pixel arrow with a dark outline, generated without an extra art dependency.
        const int width = 18, height = 24;
        var mask = new bool[width, height];
        for (int y = 3; y <= 21; y++)
            for (int x = 1; x < width - 1; x++)
                mask[x, y] = y >= 10 ? Mathf.Abs(x - 8.5f) <= (21 - y) / 2 + .5f : x >= 7 && x <= 10;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (mask[x, y]) pixels[y * width + x] = y >= 17 || x == 7
                    ? new Color32(255, 249, 209, 255) : new Color32(255, 201, 51, 255);
                else
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height && mask[x + dx, y + dy])
                                pixels[y * width + x] = new Color32(32, 22, 15, 255);
            }
        arrowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        arrowTexture.name = "ChestDirectionArrow";
        arrowTexture.filterMode = FilterMode.Point;
        arrowTexture.wrapMode = TextureWrapMode.Clamp;
        arrowTexture.SetPixels32(pixels);
        arrowTexture.Apply(false, true);
        arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 24);
        return arrowSprite;
    }

    private void Hide() { if (root != null) root.SetActive(false); }
    private void OnDisable() { Hide(); }
    private void OnDestroy()
    {
        if (root != null) Destroy(root);
        if (arrowSprite != null) Destroy(arrowSprite);
        if (arrowTexture != null) Destroy(arrowTexture);
    }
}
