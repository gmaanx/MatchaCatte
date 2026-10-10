using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SettingsUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    public static event System.Action OnSettingsOpened;

    [Header("UI Panels")]
    public GameObject settingPanel; // Kéo setting_Panel vào đây

    [Header("Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider gameVolumeSlider;

    [Header("Value Texts")]
    public Text masterVolumeText;
    public Text musicVolumeText;
    public Text gameVolumeText;

    [Header("Buttons")]
    public Button backBtn;

    [Header("Tabs (Chuyển trang)")]
    public Button gameTabBtn;
    public Button audioTabBtn;
    public Button ruleTabBtn; // Thêm tab luật chơi
    public GameObject gamePage;
    public GameObject audioPage;
    public GameObject rulePage; // Thêm page luật chơi
    public RectTransform tabPointer;

    [Header("Game Difficulty")]
    public Button btnEasy;
    public Button btnMedium;
    public Button btnHard;

    [Header("Prediction Mode")]
    public Button btnPrediction;
    public Sprite predictOnSprite;
    public Sprite predictOffSprite;

    [Header("Window Mode")]
    public UnityEngine.UI.Text txtWindowMode;
    public Button btnWindowLeft;
    public Button btnWindowRight;

    private int _currentWindowModeIndex = 0;
    private readonly FullScreenMode[] _windowModes = {
        FullScreenMode.ExclusiveFullScreen,
        FullScreenMode.Windowed
    };
    private readonly string[] _windowModeNames = {
        "FULLSCREEN",
        "WINDOW MODE"
    };

    private CanvasGroup _canvasGroup;
    private float _pointerBaseY;
    private float _pointerTargetWorldX;
    private Vector2 _originalPanelPos;
    private Coroutine _animationCoroutine;

    private void Start()
    {
        if (tabPointer != null)
        {
            _pointerBaseY = tabPointer.anchoredPosition.y;
            _pointerTargetWorldX = tabPointer.position.x;
        }

        // Thêm CanvasGroup để ẩn/hiện mà KHÔNG làm tắt GameObject (để Update vẫn chạy)
        if (settingPanel != null)
        {
            _originalPanelPos = settingPanel.GetComponent<RectTransform>().anchoredPosition;
            _canvasGroup = settingPanel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = settingPanel.AddComponent<CanvasGroup>();
            HideSettingsInternal(true);
        }

        // Khởi tạo window mode
        FullScreenMode currentMode = Screen.fullScreenMode;
        _currentWindowModeIndex = System.Array.IndexOf(_windowModes, currentMode);
        if (_currentWindowModeIndex < 0) _currentWindowModeIndex = 0;
        UpdateWindowModeUI();

        // Gán listener cho nút
        if (backBtn != null) 
        {
            backBtn.onClick.RemoveAllListeners();
            backBtn.onClick.AddListener(HideSettings);
        }

        // Đăng ký sự kiện chuyển Tab
        if (gameTabBtn != null) gameTabBtn.onClick.AddListener(ShowGameTab);
        if (audioTabBtn != null) audioTabBtn.onClick.AddListener(ShowAudioTab);
        if (ruleTabBtn != null) ruleTabBtn.onClick.AddListener(ShowRuleTab);

        // Đăng ký sự kiện chuyển Độ khó
        if (btnEasy != null) btnEasy.onClick.AddListener(() => SetDifficulty(0));
        if (btnMedium != null) btnMedium.onClick.AddListener(() => SetDifficulty(1));
        if (btnHard != null) btnHard.onClick.AddListener(() => SetDifficulty(2));

        // Đăng ký sự kiện Prediction Mode (Toggle)
        if (btnPrediction != null) 
        {
            btnPrediction.onClick.AddListener(TogglePredictionMode);
        }

        // Mặc định hiện Audio tab khi mới bật game lần đầu
        ShowAudioTab();

        // Lấy dữ liệu âm lượng và độ khó từ AudioManager
        if (AudioManager.Instance != null)
        {
            UpdateDifficultyUI(AudioManager.Instance.botDifficulty);
            UpdatePredictionUI(AudioManager.Instance.predictionMode);
            
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioManager.Instance.masterVolume;
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                UpdateText(masterVolumeText, AudioManager.Instance.masterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = AudioManager.Instance.musicVolume;
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                UpdateText(musicVolumeText, AudioManager.Instance.musicVolume);
            }

            if (gameVolumeSlider != null)
            {
                gameVolumeSlider.value = AudioManager.Instance.gameVolume;
                gameVolumeSlider.onValueChanged.AddListener(OnGameVolumeChanged);
                UpdateText(gameVolumeText, AudioManager.Instance.gameVolume);
            }
        }
    }

    private void OnDestroy()
    {
        // Phải reset biến static này khi đổi Scene, nếu không sang Scene mới game sẽ tưởng Setting vẫn đang mở và khóa mọi thao tác!
        IsOpen = false;
    }

    private void Update()
    {
        // Nhấn ESC để bật/tắt settings
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_canvasGroup != null)
            {
                if (IsOpen) HideSettings();
                else ShowSettings();
            }
        }

        // Animation cho mũi tên
        if (tabPointer != null && _canvasGroup != null && _canvasGroup.alpha > 0)
        {
            // 1. Lướt ngang mượt mà (Lerp World X)
            float currentX = tabPointer.position.x;
            float newX = Mathf.Lerp(currentX, _pointerTargetWorldX, Time.deltaTime * 15f);
            tabPointer.position = new Vector3(newX, tabPointer.position.y, tabPointer.position.z);

            // 2. Nảy lên xuống nhẹ nhàng (Sin wave trên Anchored Y)
            // Đã giảm độ cao nảy từ 8f xuống 4f để nhìn tinh tế hơn
            tabPointer.anchoredPosition = new Vector2(
                tabPointer.anchoredPosition.x, 
                _pointerBaseY + Mathf.Sin(Time.time * 6f) * 4f
            );
        }
    }

    public void HideSettings()
    {
        HideSettingsInternal(false);
    }

    private void HideSettingsInternal(bool instant)
    {
        IsOpen = false;
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            
            // Áp dụng Window Mode khi người dùng thoát Settings
            ApplyWindowMode();

            if (instant)
            {
                _canvasGroup.alpha = 0f;
                if (settingPanel != null)
                {
                    settingPanel.GetComponent<RectTransform>().anchoredPosition = _originalPanelPos + new Vector2(0f, 800f);
                }
            }
            else
            {
                if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
                _animationCoroutine = StartCoroutine(AnimatePanel(false));
            }
        }
    }

    public void ShowSettings()
    {
        IsOpen = true;
        OnSettingsOpened?.Invoke();
        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            _animationCoroutine = StartCoroutine(AnimatePanel(true));
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBtnClick(); 
        }
    }

    private System.Collections.IEnumerator AnimatePanel(bool showing)
    {
        if (settingPanel == null) yield break;
        RectTransform rect = settingPanel.GetComponent<RectTransform>();
        
        float duration = 0.4f;
        float elapsed = 0f;
        
        Vector2 hiddenPos = _originalPanelPos + new Vector2(0f, 800f); // Trượt từ trên xuống (cách tâm 800 pixel)
        Vector2 visiblePos = _originalPanelPos;
        
        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = showing ? visiblePos : hiddenPos;
        
        float startAlpha = _canvasGroup.alpha;
        float targetAlpha = showing ? 1f : 0f;
        
        // Fix nhảy vị trí nếu đang mở lần đầu
        if (showing && startAlpha == 0f)
        {
            startPos = hiddenPos;
            rect.anchoredPosition = hiddenPos;
        }

        float EaseOutBack(float x) {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Hiện ra thì nảy nhẹ (EaseOutBack), ẩn đi thì vút nhanh (t*t)
            float easeT = showing ? EaseOutBack(t) : t * t; 
            
            rect.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, easeT);
            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            yield return null;
        }
        
        rect.anchoredPosition = targetPos;
        _canvasGroup.alpha = targetAlpha;
    }

    public void ShowGameTab()
    {
        if (gamePage != null) gamePage.SetActive(true);
        if (audioPage != null) audioPage.SetActive(false);
        if (rulePage != null) rulePage.SetActive(false);

        // Di chuyển mũi tên đến vị trí của nút Game (giữ nguyên trục Y và Z)
        if (tabPointer != null && gameTabBtn != null)
        {
            _pointerTargetWorldX = gameTabBtn.transform.position.x;
        }
    }

    public void ShowAudioTab()
    {
        if (gamePage != null) gamePage.SetActive(false);
        if (audioPage != null) audioPage.SetActive(true);
        if (rulePage != null) rulePage.SetActive(false);

        // Đặt mục tiêu để mũi tên lướt tới vị trí nút Audio
        if (tabPointer != null && audioTabBtn != null)
        {
            _pointerTargetWorldX = audioTabBtn.transform.position.x;
        }
    }

    public void ShowRuleTab()
    {
        if (gamePage != null) gamePage.SetActive(false);
        if (audioPage != null) audioPage.SetActive(false);
        if (rulePage != null) rulePage.SetActive(true);

        // Đặt mục tiêu để mũi tên lướt tới vị trí nút Rule
        if (tabPointer != null && ruleTabBtn != null)
        {
            _pointerTargetWorldX = ruleTabBtn.transform.position.x;
        }
    }

    private void SetDifficulty(int level)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetBotDifficulty(level);
        }
        UpdateDifficultyUI(level);
    }

    private void UpdateDifficultyUI(int level)
    {
        // Vô hiệu hóa (interactable = false) nút đang được chọn để hiện Disabled Sprite (cái tick)
        // và bật lại các nút kia.
        if (btnEasy != null) btnEasy.interactable = (level != 0);
        if (btnMedium != null) btnMedium.interactable = (level != 1);
        if (btnHard != null) btnHard.interactable = (level != 2);
    }

    private void TogglePredictionMode()
    {
        if (AudioManager.Instance != null)
        {
            bool newValue = !AudioManager.Instance.predictionMode;
            AudioManager.Instance.SetPredictionMode(newValue);
            UpdatePredictionUI(newValue);

            // Báo cho UnityView biết để cập nhật lại bài trên tay ngay lập tức (nếu đang tới lượt)
            UnityView view = FindFirstObjectByType<UnityView>();
            if (view != null)
            {
                view.RefreshPredictionMode();
            }
        }
    }

    private void UpdatePredictionUI(bool isEnabled)
    {
        if (btnPrediction != null)
        {
            Image btnImg = btnPrediction.GetComponent<Image>();
            if (btnImg != null)
            {
                // Thay đổi trực tiếp hình ảnh (Sprite) của nút
                if (isEnabled && predictOnSprite != null)
                {
                    btnImg.sprite = predictOnSprite;
                    btnImg.color = Color.white;
                }
                else if (!isEnabled && predictOffSprite != null)
                {
                    btnImg.sprite = predictOffSprite;
                    btnImg.color = Color.white;
                }
                else
                {
                    // Fallback nếu chưa kéo Sprite vào Editor: chỉ đổi màu
                    btnImg.color = isEnabled ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
            }
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        UpdateText(masterVolumeText, value);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        UpdateText(musicVolumeText, value);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }
    }

    private void OnGameVolumeChanged(float value)
    {
        UpdateText(gameVolumeText, value);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetGameVolume(value);
        }
    }

    private void UpdateText(Text textComp, float value)
    {
        if (textComp != null)
        {
            // Hiển thị từ 0 đến 100
            textComp.text = Mathf.RoundToInt(value * 100f).ToString();
        }
    }

    // Hàm này dùng để gắn vào sự kiện OnClick của nút Thoát Game
    public void QuitGame()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBtnClick();
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (currentScene == "SampleScene")
        {
            // Nếu đang ở màn chơi, Ẩn setting và trở về Main Menu
            HideSettings();
            
            // Dùng InGameMenuController để kích hoạt hiệu ứng chuyển cảnh Circle
            InGameMenuController igm = FindFirstObjectByType<InGameMenuController>();
            if (igm != null)
            {
                igm.ReturnToMenu();
            }
            else
            {
                // Fallback nếu không tìm thấy script chuyển cảnh
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
        else
        {
            // Đang ở MainMenu thì thoát hẳn game
            Debug.Log("Đang thoát game...");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // --- WINDOW MODE LOGIC ---
    public void OnWindowLeftClicked()
    {
        if (_currentWindowModeIndex > 0)
        {
            _currentWindowModeIndex--;
            UpdateWindowModeUI();
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBtnClick();
    }

    public void OnWindowRightClicked()
    {
        if (_currentWindowModeIndex < _windowModes.Length - 1)
        {
            _currentWindowModeIndex++;
            UpdateWindowModeUI();
        }
        if (AudioManager.Instance != null) AudioManager.Instance.PlayBtnClick();
    }

    private void UpdateWindowModeUI()
    {
        if (txtWindowMode != null)
        {
            txtWindowMode.text = _windowModeNames[_currentWindowModeIndex];
        }
        if (btnWindowLeft != null)
        {
            btnWindowLeft.interactable = _currentWindowModeIndex > 0;
        }
        if (btnWindowRight != null)
        {
            btnWindowRight.interactable = _currentWindowModeIndex < _windowModes.Length - 1;
        }
    }

    private void ApplyWindowMode()
    {
        FullScreenMode targetMode = _windowModes[_currentWindowModeIndex];
        if (Screen.fullScreenMode != targetMode)
        {
            Screen.fullScreenMode = targetMode;
            // Nếu đổi sang Windowed, có thể set resolution mặc định để tránh cửa sổ quá to
            if (targetMode == FullScreenMode.Windowed)
            {
                Screen.SetResolution(1280, 720, false);
            }
            else
            {
                Resolution currentRes = Screen.currentResolution;
                Screen.SetResolution(currentRes.width, currentRes.height, targetMode);
            }
        }
    }
}
