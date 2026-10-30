using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý Pause Menu trong gameplay:
/// - Lắng nghe phím Escape để Tạm dừng / Tiếp tục
/// - Dừng Time.timeScale và chuyển Input Map để tránh bắn đạn/di chuyển khi đang mở menu
/// - Chuyển sang Settings Menu và quay lại
/// - Chơi lại (Restart) hoặc Về màn hình chính (Main Menu)
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel con chứa toàn bộ nội dung popup Pause (Dimmer, Backing, Frame). Để root GameObject luôn active.")]
    [SerializeField] private GameObject pausePanel;
    [Tooltip("Tham chiếu tới SettingsMenuUI (có thể nằm chung Canvas)")]
    [SerializeField] private SettingsMenuUI settingsMenu;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private PlayerInput playerInput;

    private static bool combatPaused;
    private static bool waitingForCombatRelease;
    private static int combatReleaseFrame = -1;

    // Discard menu input, including the click used to resume the game.
    public static bool CombatInputBlocked
    {
        get
        {
            if (combatPaused || Time.timeScale <= 0f) return true;
            if (waitingForCombatRelease)
            {
                bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
                bool skillHeld = Keyboard.current != null &&
                    (Keyboard.current.qKey.isPressed || Keyboard.current.eKey.isPressed);
                if (mouseHeld || skillHeld) return true;

                waitingForCombatRelease = false;
                combatReleaseFrame = Time.frameCount;
                return true;
            }
            return Time.frameCount <= combatReleaseFrame;
        }
    }

    public bool IsPaused => isPaused;

    private void Awake()
    {
        combatPaused = false;
        waitingForCombatRelease = false;
        combatReleaseFrame = -1;

        if (pausePanel == null)
        {
            var content = transform.Find("Content");
            pausePanel = content != null ? content.gameObject : gameObject;
        }

        // Tự động tìm SettingsMenuUI nếu chưa gán
        if (settingsMenu == null)
        {
            settingsMenu = FindAnyObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
        }

        // Tự động tìm các Button nếu chưa gán
        AutoFindButtons();

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (settingsMenu != null)
        {
            settingsMenu.OnClosed += OnSettingsClosed;
        }

        // Ẩn panel Pause khi bắt đầu game
        if (pausePanel != null && pausePanel != gameObject)
        {
            pausePanel.SetActive(false);
        }
    }

    private void AutoFindButtons()
    {
        Transform backing = transform.Find("Content/Backing") ?? transform.Find("Backing");
        if (backing != null)
        {
            if (resumeButton == null)
            {
                var tr = backing.Find("ResumeButton");
                if (tr != null) resumeButton = tr.GetComponent<Button>();
            }
            if (settingsButton == null)
            {
                var tr = backing.Find("SettingsButton");
                if (tr != null) settingsButton = tr.GetComponent<Button>();
            }
            if (restartButton == null)
            {
                var tr = backing.Find("RestartButton");
                if (tr != null) restartButton = tr.GetComponent<Button>();
            }
            if (mainMenuButton == null)
            {
                var tr = backing.Find("MainMenuButton");
                if (tr != null) mainMenuButton = tr.GetComponent<Button>();
            }
        }
    }

    private void Start()
    {
        FindPlayerInput();
    }

    private void Update()
    {
        // Nhấn Escape để mở/đóng Pause Menu
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapePress();
        }
    }

    public void HandleEscapePress()
    {
        // Nếu Settings Menu đang mở -> đóng Settings trước, quay lại Pause
        if (settingsMenu != null && settingsMenu.IsOpen)
        {
            settingsMenu.Close();
            return;
        }

        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        combatPaused = true;
        waitingForCombatRelease = true;

        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null && pausePanel != gameObject)
        {
            pausePanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        FindPlayerInput();
        if (playerInput != null && playerInput.actions != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
        }
    }

    public void Resume()
    {
        combatPaused = false;
        waitingForCombatRelease = true;
        combatReleaseFrame = Time.frameCount;

        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null && pausePanel != gameObject)
        {
            pausePanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        if (settingsMenu != null) settingsMenu.Close();

        if (playerInput != null && playerInput.actions != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    public void OpenSettings()
    {
        if (pausePanel != null && pausePanel != gameObject)
        {
            pausePanel.SetActive(false);
        }
        if (settingsMenu != null)
        {
            settingsMenu.Open();
        }
    }

    private void OnSettingsClosed()
    {
        // Khi đóng Settings từ Pause Menu, hiển thị lại Pause Menu nếu game vẫn đang pause
        if (isPaused && pausePanel != null && pausePanel != gameObject)
        {
            pausePanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        PlayerSessionState.StartNewGame();
        Time.timeScale = 1f;
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void FindPlayerInput()
    {
        if (playerInput == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInput = player.GetComponent<PlayerInput>();
            }
            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }
        }
    }

    private void OnDestroy()
    {
        combatPaused = false;
        waitingForCombatRelease = false;
        combatReleaseFrame = -1;

        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}
