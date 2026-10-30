using UnityEngine;

/// <summary>
/// Giữ UI nằm trong vùng an toàn của màn hình (tai thỏ, bo góc, thanh điều hướng...).
/// Gắn vào một Panel stretch full màn hình, đặt các thành phần HUD làm con của Panel này.
/// Trên PC / màn hình thường: vùng an toàn = toàn màn hình nên không ảnh hưởng gì.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;

        rect.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
        rect.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
