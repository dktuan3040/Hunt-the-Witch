using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu UI: Slider chạy theo máu, TextMeshPro hiển thị "máu hiện tại/máu tối đa".
/// Lắng nghe event Health.OnHealthChanged nên tự cập nhật khi nhận damage, hồi máu hoặc đổi máu tối đa
/// (không cần kiểm tra mỗi frame).
/// Dùng được cho cả Player lẫn enemy: chỉ cần kéo Health tương ứng vào ô Target.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Nguồn máu")]
    [Tooltip("Để trống = tự tìm object có tag Player")]
    [SerializeField] private Health target;

    [Header("UI")]
    [Tooltip("Để trống = tự lấy Slider con")]
    [SerializeField] private Slider slider;
    [Tooltip("Để trống = tự lấy TextMeshPro con")]
    [SerializeField] private TMP_Text hpText;
    [Tooltip("{0} = máu hiện tại, {1} = máu tối đa")]
    [SerializeField] private string textFormat = "{0}/{1}";

    [Header("Hiệu ứng")]
    [Tooltip("Slider chạy mượt tới giá trị mới. Số HP trong text luôn cập nhật ngay.")]
    [SerializeField] private bool smooth = true;
    [Tooltip("Thời gian (giây) để slider chạy hết cả thanh")]
    [SerializeField] private float fullBarTime = 0.5f;

    private float targetValue;
    private bool subscribed;

    private void Awake()
    {
        if (slider == null) slider = GetComponentInChildren<Slider>(true);
        if (hpText == null) hpText = GetComponentInChildren<TMP_Text>(true);

        EnsureCanvasCamera();
    }

    private void EnsureCanvasCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        // Trường hợp đã kéo target trong Inspector
        if (target != null) Subscribe();
    }

    private void Start()
    {
        EnsureCanvasCamera();
        if (target == null) FindTarget();
        if (target == null)
        {
            Debug.LogWarning("[HealthBarUI] Không tìm thấy Health để theo dõi. Kéo Health vào ô Target.", this);
            return;
        }

        Subscribe();
        Apply(target.CurrentHealth, target.MaxHealth, true);
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (slider == null || !smooth) return;
        if (Mathf.Approximately(slider.value, targetValue)) return;

        float speed = slider.maxValue / Mathf.Max(0.01f, fullBarTime);
        slider.value = Mathf.MoveTowards(slider.value, targetValue, speed * Time.unscaledDeltaTime);
    }

    /// <summary>Đổi đối tượng theo dõi lúc chạy game (vd: thanh máu boss).</summary>
    public void SetTarget(Health newTarget)
    {
        Unsubscribe();
        target = newTarget;
        if (target == null) return;

        Subscribe();
        Apply(target.CurrentHealth, target.MaxHealth, true);
    }

    private void FindTarget()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.GetComponentInParent<Health>();
    }

    private void Subscribe()
    {
        if (subscribed || target == null) return;
        target.OnHealthChanged += HandleHealthChanged;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (target != null) target.OnHealthChanged -= HandleHealthChanged;
        subscribed = false;
    }

    private void HandleHealthChanged(int current, int max)
    {
        Apply(current, max, false);
    }

    private void Apply(int current, int max, bool instant)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = max;
            slider.wholeNumbers = false;
            targetValue = current;

            if (instant || !smooth) slider.value = current;
        }

        if (hpText != null)
        {
            hpText.text = string.Format(textFormat, current, max);
        }
    }
}
