using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý GameOver Menu trong gameplay:
/// - Lắng nghe sự kiện chết của Player (từ Health).
/// - Chờ animation Dead của Player chạy xong mới kích hoạt popup GameOver.
/// - Khi kích hoạt: dừng Time.timeScale, chặn toàn bộ phím điều khiển / Pause Menu.
/// - Người chơi chỉ có thể tương tác với 2 lựa chọn: Chơi lại (Restart) hoặc Về Menu chính (Main Menu).
/// </summary>
[DisallowMultipleComponent]
public class GameOverMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel con chứa toàn bộ nội dung popup GameOver (Dimmer, Backing, Frame). Để root GameObject luôn active.")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button playButton;

    [Header("Death Animation Settings")]
    [Tooltip("Tên state Dead trong Animator của Player")]
    [SerializeField] private string deadStateName = "Dead";
    [Tooltip("Thời gian chờ mặc định nếu không đọc được độ dài animation")]
    [SerializeField] private float fallbackDeathDelay = 0.6f;
    [Tooltip("Khoảng dừng ngắn sau khi animation kết thúc trước khi bảng GameOver hiện lên")]
    [SerializeField] private float postAnimationDelay = 0.25f;

    [Header("Scene Configuration")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Health playerHealth;
    private Animator playerAnimator;
    private bool isTransitioning = false;

    public static bool IsGameOver { get; private set; }

    private void Awake()
    {
        IsGameOver = false;
        isTransitioning = false;

        // Tự động tìm panel con Content nếu chưa gán
        if (gameOverPanel == null)
        {
            Transform content = transform.Find("Content");
            gameOverPanel = content != null ? content.gameObject : gameObject;
        }

        // Tự động tìm các Button
        AutoFindButtons();

        // Đảm bảo Frame không chặn raycast của các nút bên dưới
        EnsureFrameDoesNotBlockRaycasts();

        // Gán sự kiện click nếu chưa được gán qua Inspector
        if (restartButton != null && !HasPersistentListener(restartButton, nameof(RestartGame)))
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (mainMenuButton != null && !HasPersistentListener(mainMenuButton, nameof(ReturnToMainMenu)))
        {
            mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        if (playButton != null && !HasPersistentListener(playButton, nameof(RestartGame)))
        {
            playButton.onClick.RemoveListener(RestartGame);
            playButton.onClick.AddListener(RestartGame);
        }

        // Đảm bảo panel GameOver luôn ẩn khi bắt đầu màn chơi
        if (gameOverPanel != null && gameOverPanel != gameObject)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void EnsureFrameDoesNotBlockRaycasts()
    {
        Transform frame = transform.Find("Content/Frame") ?? transform.Find("Frame");
        if (frame != null)
        {
            var img = frame.GetComponent<Image>();
            if (img != null)
            {
                img.raycastTarget = false;
            }
        }
    }

    private bool HasPersistentListener(Button btn, string methodName)
    {
        if (btn == null) return false;
        int count = btn.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (btn.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        }
        return false;
    }

    private void Start()
    {
        FindAndSubscribePlayer();
    }

    private void AutoFindButtons()
    {
        Transform backing = transform.Find("Content/Backing") ?? transform.Find("Backing");
        if (backing != null)
        {
            if (restartButton == null)
            {
                Transform tr = backing.Find("RestartButton") ?? backing.Find("PlayAgainButton") ?? backing.Find("RetryButton");
                if (tr != null) restartButton = tr.GetComponent<Button>();
            }

            if (mainMenuButton == null)
            {
                Transform tr = backing.Find("MainMenuButton") ?? backing.Find("MenuButton");
                if (tr != null) mainMenuButton = tr.GetComponent<Button>();
            }

            if (playButton == null)
            {
                Transform tr = backing.Find("PlayButton");
                if (tr != null) playButton = tr.GetComponent<Button>();
            }
        }
    }

    private void FindAndSubscribePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null) player = movement.gameObject;
        }

        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
            playerAnimator = player.GetComponent<Animator>() ?? player.GetComponentInChildren<Animator>();

            if (playerHealth != null)
            {
                playerHealth.OnDied -= HandlePlayerDied;
                playerHealth.OnDied += HandlePlayerDied;

                // Trường hợp nếu lúc khởi tạo player đã chết
                if (playerHealth.IsDead)
                {
                    HandlePlayerDied();
                }
            }
        }
    }

    private void HandlePlayerDied()
    {
        // Vô hiệu hóa PauseMenu ngay khi player chết để tránh người chơi bấm Esc giữa chừng
        var pauseMenu = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            pauseMenu.enabled = false;
        }

        StartCoroutine(WaitForDeathAnimationAndShow());
    }

    private IEnumerator WaitForDeathAnimationAndShow()
    {
        // Chờ 2 frame để Animator tiếp nhận tham số isDead và chuyển sang state Dead
        yield return null;
        yield return null;

        float animDuration = fallbackDeathDelay;

        if (playerAnimator != null && playerAnimator.runtimeAnimatorController != null)
        {
            // Lấy độ dài clip từ state hoặc từ Controller
            AnimatorStateInfo stateInfo = playerAnimator.IsInTransition(0)
                ? playerAnimator.GetNextAnimatorStateInfo(0)
                : playerAnimator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.IsName(deadStateName) && stateInfo.length > 0.05f && !float.IsInfinity(stateInfo.length))
            {
                animDuration = stateInfo.length;
            }
            else
            {
                // Tìm kiếm theo tên clip
                foreach (AnimationClip clip in playerAnimator.runtimeAnimatorController.animationClips)
                {
                    if (clip.name.Equals(deadStateName, StringComparison.OrdinalIgnoreCase))
                    {
                        animDuration = clip.length;
                        break;
                    }
                }
            }

            // Chờ animation hoàn thành (normalizedTime >= 1.0f hoặc hết thời lượng clip)
            float elapsed = 0f;
            while (elapsed < animDuration + 0.1f)
            {
                elapsed += Time.deltaTime;
                AnimatorStateInfo current = playerAnimator.GetCurrentAnimatorStateInfo(0);
                if (current.IsName(deadStateName) && current.normalizedTime >= 1.0f)
                {
                    break;
                }
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(animDuration);
        }

        // Khoảng nghỉ ngắn sau khi animation kết thúc để tạo cảm giác mượt mà
        if (postAnimationDelay > 0f)
        {
            yield return new WaitForSeconds(postAnimationDelay);
        }

        ShowGameOver();
    }

    public void ShowGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        // Vô hiệu hóa PauseMenu nếu còn mở
        var pauseMenu = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
        if (pauseMenu != null)
        {
            if (pauseMenu.IsPaused)
            {
                pauseMenu.Resume();
            }
            pauseMenu.enabled = false;
        }

        // Đảm bảo PlayerInput đã được tắt để tránh nhận input di chuyển/chiến đấu
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerInput playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.enabled)
            {
                playerInput.enabled = false;
            }
        }

        // Bật panel GameOver
        if (gameOverPanel != null && gameOverPanel != gameObject)
        {
            gameOverPanel.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        // Mở con trỏ chuột để bấm nút
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Tự động focus vào nút Restart (hỗ trợ điều khiển bằng Gamepad / Phím)
        if (restartButton != null)
        {
            restartButton.Select();
        }
        else if (playButton != null)
        {
            playButton.Select();
        }
        else if (mainMenuButton != null)
        {
            mainMenuButton.Select();
        }

        // Dừng thời gian thế giới game
        GameplayPause.Acquire(this);
    }

    public void RestartGame()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        IsGameOver = false;
        PlayerSessionState.StartNewGame();
        GameplayPause.Release(this);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void ReturnToMainMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;

        IsGameOver = false;
        PlayerSessionState.StartNewGame();
        GameplayPause.Release(this);

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnDestroy()
    {
        IsGameOver = false;
        GameplayPause.Release(this);

        if (playerHealth != null)
        {
            playerHealth.OnDied -= HandlePlayerDied;
        }
    }
}

