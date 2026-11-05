using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
public sealed class ScenePortal : MonoBehaviour
{
    [SerializeField] private string destinationScene = "M_F1";
    private bool loading;

    private void Reset()
    {
        var trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.1f, 2.3f);
    }

    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void OnTriggerEnter2D(Collider2D other) => TryTravel(other);
    private void OnTriggerStay2D(Collider2D other) => TryTravel(other);

    public bool TryTravel(Component actor)
    {
        if (!isActiveAndEnabled || loading || actor == null || PauseMenuUI.InteractionInputBlocked) return false;
        var health = actor.GetComponentInParent<Health>();
        if (health == null || health.Team != Team.Player || health.IsDead
            || health.gameObject.scene != gameObject.scene) return false;
        if (string.IsNullOrWhiteSpace(destinationScene) || !Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError("Portal destination must be enabled in the build scene list: " + destinationScene, this);
            return false;
        }

        loading = true;
        var persistence = health.GetComponent<PlayerStatePersistence>();
        if (persistence != null) persistence.Capture();
        SceneManager.LoadScene(destinationScene, LoadSceneMode.Single);
        return true;
    }
}
