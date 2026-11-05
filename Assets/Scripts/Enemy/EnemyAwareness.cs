using UnityEngine;

// Use the gameplay camera, not Renderer.isVisible (which also sees Scene View cameras).
public static class EnemyAwareness
{
    private static readonly Plane[] planes = new Plane[6];
    private static Camera cachedCamera;
    private static Matrix4x4 cachedView, cachedProjection;
    private static int cachedFrame = -1;

    public static bool IsInView(Camera camera, Transform actor, SpriteRenderer visual)
    {
        if (camera == null || !camera.isActiveAndEnabled || actor == null) return false;
        int layer = visual != null ? visual.gameObject.layer : actor.gameObject.layer;
        if ((camera.cullingMask & (1 << layer)) == 0) return false;
        if (visual != null && (!visual.enabled || !visual.gameObject.activeInHierarchy)) return false;
        var view = camera.worldToCameraMatrix;
        var projection = camera.projectionMatrix;
        if (camera != cachedCamera || cachedFrame != Time.frameCount || view != cachedView || projection != cachedProjection)
        {
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            cachedCamera = camera;
            cachedFrame = Time.frameCount;
            cachedView = view;
            cachedProjection = projection;
        }
        var bounds = visual != null ? visual.bounds : new Bounds(actor.position, Vector3.one * .5f);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    public static bool CanDetect(Transform actor, SpriteRenderer visual, Transform player, float fallbackRadius)
    {
        if (actor == null || player == null || actor.gameObject.scene != player.gameObject.scene) return false;
        var camera = Camera.main;
        if (camera != null && camera.isActiveAndEnabled && camera.gameObject.scene == player.gameObject.scene)
            return IsInView(camera, actor, visual);
        // Supports test arenas and scenes whose gameplay camera has not initialized yet.
        return Vector2.Distance(actor.position, player.position) <= fallbackRadius;
    }
}
