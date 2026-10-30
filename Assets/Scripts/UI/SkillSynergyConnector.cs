using UnityEngine;
using UnityEngine.UI;

// The Connect sprite runs vertically (+Y). Follow actual UI geometry, not screen pixels.
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SkillSynergyConnector : MonoBehaviour
{
    [SerializeField] private RectTransform skillQ;
    [SerializeField] private RectTransform skillE;
    [SerializeField, Min(0f)] private float thickness = 26f;
    [SerializeField, Min(0f)] private float edgeOverlap = 4f;
    private RectTransform connection;

    private void OnEnable()
    {
        connection = (RectTransform)transform;
        var image = GetComponent<Image>();
        if (image != null) image.raycastTarget = false;
        Canvas.willRenderCanvases += UpdateGeometry;
        UpdateGeometry();
    }

    private void OnDisable() => Canvas.willRenderCanvases -= UpdateGeometry;
    private void LateUpdate() => UpdateGeometry();

    public void UpdateGeometry()
    {
        if (connection == null) connection = (RectTransform)transform;
        var parent = connection.parent as RectTransform;
        if (parent == null) return;
        if (skillQ == null) skillQ = FindFrame(parent, "SkillQ");
        if (skillE == null) skillE = FindFrame(parent, "SkillE");
        if (skillQ == null || skillE == null) return;

        Vector2 q = parent.InverseTransformPoint(skillQ.TransformPoint(skillQ.rect.center));
        Vector2 e = parent.InverseTransformPoint(skillE.TransformPoint(skillE.rect.center));
        Vector2 delta = e - q;
        float distance = delta.magnitude;
        Vector2 direction = distance > 0.001f ? delta / distance : Vector2.up;
        float qInset = Mathf.Max(0f, EdgeDistance(skillQ, parent, direction) - edgeOverlap);
        float eInset = Mathf.Max(0f, EdgeDistance(skillE, parent, -direction) - edgeOverlap);
        // Overlapping icons have no visible gap to connect.
        float length = Mathf.Max(0f, distance - qInset - eInset);
        Vector2 start = q + direction * qInset;
        Vector2 midpoint = length > 0f ? start + direction * (length * 0.5f) : (q + e) * 0.5f;
        Vector3 position = new Vector3(midpoint.x, midpoint.y, 0f);
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        Vector2 size = new Vector2(thickness, length);

        // Fixed central anchors make local geometry independent of the previous anchors.
        if (connection.anchorMin != Vector2.one * 0.5f) connection.anchorMin = Vector2.one * 0.5f;
        if (connection.anchorMax != Vector2.one * 0.5f) connection.anchorMax = Vector2.one * 0.5f;
        if (connection.pivot != Vector2.one * 0.5f) connection.pivot = Vector2.one * 0.5f;
        if (connection.localScale != Vector3.one) connection.localScale = Vector3.one;
        if (connection.localPosition != position) connection.localPosition = position;
        if (Quaternion.Angle(connection.localRotation, rotation) > 0.001f) connection.localRotation = rotation;
        if (connection.sizeDelta != size) connection.sizeDelta = size;
    }

    private static RectTransform FindFrame(RectTransform parent, string slotName)
    {
        var slot = parent.Find(slotName);
        if (slot == null) return null;
        return (slot.Find("SkillBorder") ?? slot) as RectTransform;
    }

    // Ray/rectangle intersection in the frame's own coordinates also handles UI scaling/rotation.
    private static float EdgeDistance(RectTransform frame, RectTransform parent, Vector2 direction)
    {
        Vector3 localDirection = frame.InverseTransformVector(parent.TransformVector(direction));
        Vector2 half = frame.rect.size * 0.5f;
        float x = Mathf.Abs(localDirection.x) > 0.00001f ? half.x / Mathf.Abs(localDirection.x) : float.PositiveInfinity;
        float y = Mathf.Abs(localDirection.y) > 0.00001f ? half.y / Mathf.Abs(localDirection.y) : float.PositiveInfinity;
        return Mathf.Min(x, y);
    }
}