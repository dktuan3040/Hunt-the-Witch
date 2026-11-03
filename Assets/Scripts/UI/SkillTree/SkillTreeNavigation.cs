using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Two-axis graph pan, vertical wheel scroll, and pointer-anchored Ctrl-wheel zoom.</summary>
[DisallowMultipleComponent]
public sealed class SkillTreeNavigation : ScrollRect
{
    [Header("Graph zoom")]
    public float minimumZoom = 0.4f;
    public float maximumZoom = 2.5f;
    public float zoomPerNotch = 0.12f;
    public TMP_Text zoomLabel;

    public float Zoom => content != null ? content.localScale.x : 1f;

    public override void OnScroll(PointerEventData eventData)
    {
        var keyboard = Keyboard.current;
        bool ctrl = keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed);
        float delta = Mathf.Clamp(eventData.scrollDelta.y, -1f, 1f);
        if (ctrl)
        {
            ZoomAtScreenPoint(Zoom * Mathf.Exp(delta * zoomPerNotch), eventData.position, eventData.enterEventCamera);
            eventData.Use();
            return;
        }
        var previous = eventData.scrollDelta;
        eventData.scrollDelta = new Vector2(0, delta);
        base.OnScroll(eventData);
        eventData.scrollDelta = previous;
    }

    public void ZoomAtScreenPoint(float zoom, Vector2 screenPoint, Camera eventCamera)
    {
        if (viewport == null || content == null) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, eventCamera, out local)) return;
        ZoomAtViewportPoint(zoom, local);
    }

    public void ZoomAtViewportPoint(float zoom, Vector2 point)
    {
        if (viewport == null || content == null) return;
        zoom = Mathf.Clamp(zoom, minimumZoom, maximumZoom);
        Vector3 world = viewport.TransformPoint(point);
        Vector3 contentPoint = content.InverseTransformPoint(world);
        StopMovement();
        content.localScale = new Vector3(zoom, zoom, 1);
        content.position += world - content.TransformPoint(contentPoint);
        ClampToViewport();
        UpdateZoomLabel();
        Rebuild(CanvasUpdate.PostLayout);
    }

    public void ClampToViewport()
    {
        if (viewport == null || content == null) return;
        var corners = new Vector3[4];
        content.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        foreach (var corner in corners)
        {
            Vector2 p = viewport.InverseTransformPoint(corner);
            min = Vector2.Min(min, p); max = Vector2.Max(max, p);
        }
        Rect area = viewport.rect;
        Vector2 correction = Vector2.zero;
        if (max.x-min.x <= area.width) correction.x = area.center.x-(min.x+max.x)*0.5f;
        else if (min.x > area.xMin) correction.x = area.xMin-min.x;
        else if (max.x < area.xMax) correction.x = area.xMax-max.x;
        if (max.y-min.y <= area.height) correction.y = area.center.y-(min.y+max.y)*0.5f;
        else if (min.y > area.yMin) correction.y = area.yMin-min.y;
        else if (max.y < area.yMax) correction.y = area.yMax-max.y;
        content.position += viewport.TransformVector(correction);
    }

    public void ResetView()
    {
        if (content == null) return;
        StopMovement();
        content.localScale = Vector3.one;
        Canvas.ForceUpdateCanvases();
        normalizedPosition = new Vector2(0.5f, 0.5f);
        ClampToViewport(); UpdateZoomLabel();
        Rebuild(CanvasUpdate.PostLayout);
    }

    public void UpdateZoomLabel()
    {
        if (zoomLabel != null) zoomLabel.text = Mathf.RoundToInt(Zoom * 100) + "%";
    }
}

