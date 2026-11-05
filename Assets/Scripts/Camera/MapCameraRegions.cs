using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineConfiner2D))]
public sealed class MapCameraRegions : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private PolygonCollider2D f1Bounds;
    [SerializeField] private PolygonCollider2D b1Bounds;
    private CinemachineCamera mapCamera;
    private CinemachineConfiner2D confiner;
    public bool IsInBossRegion { get; private set; }
    public Transform Player => player;

    private void Awake()
    {
        mapCamera = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
        BindPlayer();
        bool startInBoss = player != null && b1Bounds != null && b1Bounds.OverlapPoint(player.position);
        SetRegion(startInBoss);
    }

    private void LateUpdate()
    {
        if (player == null) BindPlayer();
    }

    private void BindPlayer()
    {
        if (player == null)
        {
            foreach (var health in FindObjectsByType<Health>())
            {
                if (health.Team == Team.Player && health.gameObject.scene == gameObject.scene)
                {
                    player = health.transform;
                    break;
                }
            }
        }
        if (player != null) mapCamera.Target.TrackingTarget = player;
    }

    public void SetRegion(bool bossRegion)
    {
        if (confiner == null) confiner = GetComponent<CinemachineConfiner2D>();
        var bounds = bossRegion ? b1Bounds : f1Bounds;
        if (bounds == null) return;
        IsInBossRegion = bossRegion;
        confiner.BoundingShape2D = bounds;
        confiner.InvalidateBoundingShapeCache();
    }
}
