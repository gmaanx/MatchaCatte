using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [Tooltip("Tên scene của menu chính")]
    public string mainMenuSceneName = "MainMenu";
    
    [Tooltip("Thời gian chuyển cảnh (giây)")]
    public float fadeDuration = 1.0f;

    private bool isTransitioning = false;

    // Hàm này để bạn gắn vào sự kiện OnClick của nút Btn_Home ở màn hình kết thúc game
    public void ReturnToMenu()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        
        StartFadeTransition();
    }

    void StartFadeTransition()
    {
        // Tái sử dụng lại logic Fade mà ta đã viết ở MainMenuController
        GameObject fadeCanvasObj = new GameObject("FadeCanvas");
        DontDestroyOnLoad(fadeCanvasObj);
        
        Canvas canvas = fadeCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; 
        
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvasObj.transform, false);
        Image fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); 
        
        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Tái sử dụng class FadeHelper đã được khai báo ở MainMenuController.cs
        FadeHelper helper = fadeCanvasObj.AddComponent<FadeHelper>();
        helper.StartFade(fadeImage, fadeDuration, mainMenuSceneName);
    }
}
