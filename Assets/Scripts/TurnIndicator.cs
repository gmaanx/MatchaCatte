using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class TurnIndicator : MonoBehaviour
{
    [Header("Cấu hình nhấp nhô")]
    public float floatSpeed = 8f;
    public float floatAmplitude = 10f;

    [Header("Khoảng cách từ cọc bài giữa")]
    public float offsetY = 140f; // Chỉnh cho You và Bot 2 (Trên/Dưới) - Số to hơn vì bài dài
    public float offsetX = 100f; // Chỉnh cho Bot 1 và Bot 3 (Trái/Phải) - Số nhỏ hơn vì bài hẹp

    private RectTransform _rectTransform;
    private Vector2 _basePosition;
    private Vector2 _animDir;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        float offset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        _rectTransform.anchoredPosition = _basePosition + (_animDir * offset);
    }

    public void PointTo(string playerName, RectTransform centerDeck)
    {
        if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

        Vector2 centerPos = centerDeck.anchoredPosition;

        if (playerName == "You")
        {
            _basePosition = centerPos + new Vector2(0, -offsetY);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, 0);
            _animDir = new Vector2(0, 1);
        }
        else if (playerName == "Bot 1")
        {
            _basePosition = centerPos + new Vector2(-offsetX, 0);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            _animDir = new Vector2(1, 0);
        }
        else if (playerName == "Bot 2")
        {
            _basePosition = centerPos + new Vector2(0, offsetY);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            _animDir = new Vector2(0, 1);
        }
        else if (playerName == "Bot 3")
        {
            _basePosition = centerPos + new Vector2(offsetX, 0);
            _rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            _animDir = new Vector2(1, 0);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}