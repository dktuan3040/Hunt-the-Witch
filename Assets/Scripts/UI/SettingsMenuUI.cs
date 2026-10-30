using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Quản lý giao diện Cài đặt (Settings Menu), có thể dùng độc lập ở MainMenu hoặc trong Pause Menu khi chơi game.
/// Tự động lưu và tải cấu hình âm thanh, đồ họa từ PlayerPrefs.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel con chứa toàn bộ nội dung popup Settings (Dimmer, Backing, Frame). Để root GameObject luôn active.")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Audio Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeText;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private TMP_Text bgmVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeText;

    [Header("Display Controls")]
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Buttons")]
    [SerializeField] private Button backButton;

    public System.Action OnClosed;

    private const string PREF_MASTER_VOL = "Settings_MasterVol";
    private const string PREF_BGM_VOL = "Settings_BgmVol";
    private const string PREF_SFX_VOL = "Settings_SfxVol";
    private const string PREF_FULLSCREEN = "Settings_Fullscreen";

    public bool IsOpen => settingsPanel != null ? settingsPanel.activeSelf : gameObject.activeSelf;

    private void Awake()
    {
        if (settingsPanel == null)
        {
            var content = transform.Find("Content");
            settingsPanel = content != null ? content.gameObject : gameObject;
        }

        // Tự động tìm nút Back nếu chưa gán
        if (backButton == null)
        {
            var backTr = transform.Find("Content/Backing/BackButton") ?? transform.Find("Backing/BackButton");
            if (backTr != null) backButton = backTr.GetComponent<Button>();
        }

        // Gắn listener cho các nút & slider
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        if (backButton != null) backButton.onClick.AddListener(Close);

        LoadSettings();

        // Ẩn nội dung khi bắt đầu nếu dùng panel con
        if (settingsPanel != null && settingsPanel != gameObject)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Nhấn Escape để đóng Settings nếu đang mở
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Open()
    {
        if (settingsPanel != null && settingsPanel != gameObject)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        LoadSettings();
    }

    public void Close()
    {
        PlayerPrefs.Save();

        if (settingsPanel != null && settingsPanel != gameObject)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        OnClosed?.Invoke();
    }

    private void LoadSettings()
    {
        // 1. Master Volume (0f .. 1f, mặc định 0.8f)
        float masterVol = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 0.8f);
        if (masterVolumeSlider != null) masterVolumeSlider.value = masterVol;
        UpdateMasterVolumeDisplay(masterVol);

        // 2. BGM Volume
        float bgmVol = PlayerPrefs.GetFloat(PREF_BGM_VOL, 0.8f);
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = bgmVol;
        UpdateBgmVolumeDisplay(bgmVol);

        // 3. SFX Volume
        float sfxVol = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        UpdateSfxVolumeDisplay(sfxVol);

        // 4. Fullscreen
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;
        Screen.fullScreen = isFullscreen;
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, value);
        UpdateMasterVolumeDisplay(value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_BGM_VOL, value);
        UpdateBgmVolumeDisplay(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(PREF_SFX_VOL, value);
        UpdateSfxVolumeDisplay(value);
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
    }

    private void UpdateMasterVolumeDisplay(float val)
    {
        if (masterVolumeText != null) masterVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    private void UpdateBgmVolumeDisplay(float val)
    {
        if (bgmVolumeText != null) bgmVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }

    private void UpdateSfxVolumeDisplay(float val)
    {
        if (sfxVolumeText != null) sfxVolumeText.text = $"{Mathf.RoundToInt(val * 100)}%";
    }
}
