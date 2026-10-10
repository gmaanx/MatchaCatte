using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor Settings")]
    [Tooltip("Chuột bình thường")]
    public Texture2D normalCursor;
    
    [Tooltip("Chuột khi hover (lướt qua) lá bài hoặc nút bấm")]
    public Texture2D hoverCursor;
    
    [Tooltip("Chuột khi đang nắm kéo lá bài")]
    public Texture2D dragCursor;
    
    [Tooltip("Tọa độ điểm nhấp chuột (thường là góc trên cùng bên trái: 0, 0)")]
    public Vector2 hotSpot = Vector2.zero;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetNormalCursor(); // Khởi tạo chuột mặc định
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Chuyển sang con trỏ chuột bình thường
    /// </summary>
    public void SetNormalCursor()
    {
        if (normalCursor != null)
        {
            Cursor.SetCursor(normalCursor, hotSpot, CursorMode.Auto);
        }
        else 
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); // Reset về chuột Windows
        }
    }

    /// <summary>
    /// Chuyển sang con trỏ chuột dạng "Nắm kéo"
    /// </summary>
    public void SetDragCursor()
    {
        if (dragCursor != null)
        {
            Cursor.SetCursor(dragCursor, hotSpot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// Chuyển sang con trỏ chuột khi hover qua vật thể
    /// </summary>
    public void SetHoverCursor()
    {
        if (hoverCursor != null)
        {
            Cursor.SetCursor(hoverCursor, hotSpot, CursorMode.Auto);
        }
    }
}
