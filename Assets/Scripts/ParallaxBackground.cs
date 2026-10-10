using UnityEngine;
using UnityEngine.InputSystem; // Bắt buộc phải có dòng này để xài Input mới

public class ParallaxBackground : MonoBehaviour
{
    [Header("Cài đặt Ly tâm")]
    [Tooltip("Độ trượt tối đa (pixel). Phải nhỏ hơn rìa 150 của m")]
    public float moveAmount = 50f;

    [Tooltip("Độ mượt (Lerp). Số càng nhỏ trượt càng lả lướt")]
    public float smoothSpeed = 5f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
    }

    void Update()
    {
        // Check xem chuột có đang cắm vào máy không (tránh lỗi sập game)
        if (Mouse.current == null) return;

        // Đọc tọa độ chuột bằng hệ thống mới
        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Tính toán tọa độ chuột quy về chuẩn từ -1 đến 1
        float mouseX = (mousePos.x / Screen.width) * 2f - 1f;
        float mouseY = (mousePos.y / Screen.height) * 2f - 1f;

        // Đã bỏ dấu âm (-) để background trượt THEO HƯỚNG con trỏ chuột
        Vector2 targetPosition = startPosition + new Vector2(mouseX * moveAmount, mouseY * moveAmount);

        // Nội suy di chuyển lả lướt
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * smoothSpeed);
    }
}