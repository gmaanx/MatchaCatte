using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo thả Legacy Text StartGame vào đây")]
    public Text startGameText;
    
    [Header("Settings")]
    public float blinkSpeed = 1.5f;
    public float minAlpha = 0.2f;
    public float maxAlpha = 1.0f;
    
    [Tooltip("Tên scene sẽ chuyển sang khi bấm")]
    public string nextSceneName = "SampleScene";

    [Tooltip("Thời gian chuyển cảnh đen (giây)")]
    public float fadeDuration = 1.0f;

    private bool isTransitioning = false;

    void Update()
    {
        // Nếu đang trong quá trình chuyển cảnh thì không nhận thêm input
        if (isTransitioning) return;

        // Bỏ qua mọi lệnh bấm nếu Settings đang được mở
        if (SettingsUI.IsOpen) return;

        // Hiệu ứng chớp nháy (PingPong giữa minAlpha và maxAlpha)
        if (startGameText != null)
        {
            Color color = startGameText.color;
            float pingPongValue = Mathf.PingPong(Time.time * blinkSpeed, 1.0f);
            color.a = Mathf.Lerp(minAlpha, maxAlpha, pingPongValue);
            startGameText.color = color;
        }

        // Kiểm tra xem có input nào không từ New Input System
        bool hasInput = false;

        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                hasInput = true;
            }
        }
            
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            hasInput = true;
            
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            hasInput = true;

        if (hasInput)
        {
            isTransitioning = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTransitionSound();
            StartFadeTransition();
        }
    }

    void StartFadeTransition()
    {
        // Tạo một Canvas tạm thời phủ toàn màn hình để làm hiệu ứng đen
        GameObject fadeCanvasObj = new GameObject("FadeCanvas");
        DontDestroyOnLoad(fadeCanvasObj); // Giữ Canvas này khi qua Scene mới
        
        Canvas canvas = fadeCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Đặt lên trên cùng tất cả UI khác
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvasObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Bắt đầu hoàn toàn trong suốt
        
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Thêm một helper script vào Canvas này để chạy Coroutine
        // (Vì MainMenuController sẽ bị xóa khi qua scene mới làm dừng Coroutine)
        FadeHelper helper = fadeCanvasObj.AddComponent<FadeHelper>();
        helper.StartFade(fadeImage, fadeDuration, nextSceneName);
    }
}

public class FadeHelper : MonoBehaviour
{
    public void StartFade(Image fadeImage, float fadeDuration, string nextSceneName)
    {
        StartCoroutine(FadeRoutine(fadeImage, fadeDuration, nextSceneName));
    }

    IEnumerator FadeRoutine(Image fadeImage, float fadeDuration, string nextSceneName)
    {
        fadeImage.color = Color.black; // Màu gốc là đen tuyền
        Shader shader = Shader.Find("Hidden/UI_CircleTransition");
        Material mat = null;
        
        if (shader != null)
        {
            mat = new Material(shader);
            fadeImage.material = mat;
        }

        if (mat != null)
        {
            // 1. Zoom In (Đóng vòng tròn lại)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;
                // Hàm ease in-out để mượt hơn
                float easeT = t * t * (3f - 2f * t);
                mat.SetFloat("_Radius", Mathf.Lerp(1.5f, 0f, easeT));
                yield return null;
            }
            mat.SetFloat("_Radius", 0f); // Đen hoàn toàn
        }
        else
        {
            // Fallback: Chuyển cảnh Fade mờ dần nếu không tìm thấy Shader
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, timer / fadeDuration);
                yield return null;
            }
            fadeImage.color = new Color(0, 0, 0, 1f);
        }

        // 2. Tải Scene mới
        yield return SceneManager.LoadSceneAsync(nextSceneName);

        if (mat != null)
        {
            // 3. Zoom Out (Mở vòng tròn ra)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / fadeDuration;
                float easeT = t * t * (3f - 2f * t);
                mat.SetFloat("_Radius", Mathf.Lerp(0f, 1.5f, easeT));
                yield return null;
            }
            mat.SetFloat("_Radius", 1.5f);
        }
        else
        {
            // Fallback Fade In
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeImage.color = new Color(0, 0, 0, 1f - (timer / fadeDuration));
                yield return null;
            }
        }
        
        // Xóa Canvas tạm thời sau khi hoàn tất hiệu ứng
        Destroy(gameObject);
    }
}
