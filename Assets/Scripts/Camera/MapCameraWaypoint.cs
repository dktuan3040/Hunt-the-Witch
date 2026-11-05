using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class MapCameraWaypoint : MonoBehaviour
{
    [SerializeField] private MapCameraRegions cameraRegions;
    [SerializeField] private bool switchToBossRegion = true;

    private void OnTriggerEnter2D(Collider2D other) => TrySwitch(other);
    private void OnTriggerStay2D(Collider2D other) => TrySwitch(other);

    public bool TrySwitch(Component actor)
    {
        if (!isActiveAndEnabled || actor == null || cameraRegions == null) return false;
        var health = actor.GetComponentInParent<Health>();
        if (health == null || health.Team != Team.Player || health.IsDead) return false;
        if (health.gameObject.scene != gameObject.scene) return false;
        if (cameraRegions.IsInBossRegion != switchToBossRegion)
            cameraRegions.SetRegion(switchToBossRegion);
        return true;
    }

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
}
