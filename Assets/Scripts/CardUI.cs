using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image cardImage;
    public Button cardButton;

    private int _cardIndex;
    public int CardIndex => _cardIndex;
    private Func<int, bool> _onPlayCallback;
    private bool _isPlayerCard;

    private Sprite _normalSprite;
    private Sprite _contrastSprite;

    private bool _isHovered = false;
    private bool _isDragging = false;
    private bool _isPlayable = true;
    private Vector3 _basePos;
    private Quaternion _baseRot;
    private float _hoverTimer = 0f;
    private Vector2 _pointerDragOffset;

    private Coroutine _hoverCoroutine;
    private Coroutine _returnCoroutine;
    
    private RectTransform _hitBoxRect;

    void Awake()
    {
        // Trick của Senior: Tạo một Hitbox ảo (vô hình) 
        // Khi lá bài nảy lên, hitbox này sẽ giãn dài xuống dưới để "níu" lấy con trỏ chuột,
        // giúp chuột không bị rớt xuống lá bài nằm dưới gây ra lỗi nháy (jitter).
        GameObject hitBox = new GameObject("HoverHitBox");
        hitBox.transform.SetParent(transform, false);
        
        _hitBoxRect = hitBox.AddComponent<RectTransform>();
        _hitBoxRect.anchorMin = Vector2.zero;
        _hitBoxRect.anchorMax = Vector2.one;
        _hitBoxRect.offsetMin = Vector2.zero; 
        _hitBoxRect.offsetMax = Vector2.zero;
        
        Image img = hitBox.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // Vô hình
    }

    private void OnEnable()
    {
        SettingsUI.OnSettingsOpened += ForceDrop;
    }

    private void OnDisable()
    {
        SettingsUI.OnSettingsOpened -= ForceDrop;
    }

    private void ForceDrop()
    {
        if (_isHovered)
        {
            _isHovered = false;
            if (!_isDragging)
            {
                if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = StartCoroutine(HoverRoutine(false));
            }
        }

        if (_isDragging)
        {
            _isDragging = false;
            
            if (CursorManager.Instance != null) CursorManager.Instance.SetNormalCursor();

            if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
            _returnCoroutine = StartCoroutine(ReturnToBaseRoutine());
        }
    }

    [Header("Hover Config")]
    public float delayBeforeBounce = 0.5f;
    public float bounceSpeed = 12f;
    public float bounceHeight = 4f;
    public float flashSpeed = 0.3f;

    [Header("Drag Config")]
    public float playThresholdY = 150f;

    public void Setup(Sprite normalSprite, Sprite contrastSprite, int index, Func<int, bool> onPlay, bool isPlayer = false)
    {
        _normalSprite = normalSprite;
        _contrastSprite = contrastSprite;
        cardImage.sprite = normalSprite;
        _cardIndex = index;
        _onPlayCallback = onPlay;
        _isPlayerCard = isPlayer;
        _isPlayable = true;
        cardImage.color = Color.white;

        cardButton.onClick.RemoveAllListeners();
        // Không dùng click nữa, dùng Drag thay thế
    }

    public void SetPlayable(bool isPlayable)
    {
        _isPlayable = isPlayable;
        if (!_isPlayable)
        {
            // Làm tối màu lá bài nhẹ nhàng hơn để không gây nhầm lẫn (0.7 thay vì 0.5)
            cardImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); 
        }
        else
        {
            cardImage.color = Color.white;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (SettingsUI.IsOpen) return;

        if (_isPlayerCard && !_isHovered && !_isDragging)
        {
            _isHovered = true;
            _hoverTimer = 0f;
            
            if (CursorManager.Instance != null) CursorManager.Instance.SetHoverCursor();
            
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCardHover();

            if (_hoverCoroutine == null && _returnCoroutine == null)
            {
                _basePos = transform.localPosition;
                _baseRot = transform.localRotation;
            }
            else
            {
                if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
                if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
            }

            _hoverCoroutine = StartCoroutine(HoverRoutine(true));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isPlayerCard && _isHovered)
        {
            _isHovered = false;
            
            if (!_isDragging && CursorManager.Instance != null) 
            {
                CursorManager.Instance.SetNormalCursor();
            }

            if (!_isDragging)
            {
                if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = StartCoroutine(HoverRoutine(false));
            }
        }
    }

    private IEnumerator HoverRoutine(bool isHovering)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (isHovering)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }
        else
        {
            // Trả lại layer tự nhiên ngay lập tức khi chuột rời đi để lá bài trượt xuống dưới các lá khác, tránh hiện tượng giật/chớp layer ở cuối animation
            canvas.overrideSorting = false;
        }

        if (isHovering)
        {
            if (_hitBoxRect != null) 
            {
                // CHỈ thòng hitbox xuống dưới (dọc theo trục Y) để đỡ chuột.
                // Tuyệt đối KHÔNG phình to ra 2 bên Trái/Phải để bạn có thể lướt chuột mượt mà 
                // từ lá này sang lá khác tạo hiệu ứng lượn sóng (Wave effect).
                _hitBoxRect.offsetMin = new Vector2(0f, -60f); 
                _hitBoxRect.offsetMax = Vector2.zero;   
            }
        }
        else
        {
            // Khi không hover nữa, thu hitbox lại ngay lập tức để không cản trở lá bài dưới
            if (_hitBoxRect != null) 
            {
                _hitBoxRect.offsetMin = Vector2.zero;
                _hitBoxRect.offsetMax = Vector2.zero;
            }
            cardImage.sprite = _normalSprite;
        }

        float duration = 0.15f;
        float elapsed = 0f;

        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        // Trả lại góc xoay -25 độ (như cũ) cho lá bài để có hiệu ứng quạt 
        Vector3 targetPos = isHovering ? _basePos + (_baseRot * new Vector3(0, 40f, 0)) : _basePos;
        Quaternion targetRot = isHovering ? _baseRot * Quaternion.Euler(0, 0, -25f) : _baseRot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = 1f - Mathf.Pow(1f - t, 3);

            transform.localPosition = Vector3.Lerp(startPos, targetPos, ease);
            transform.localRotation = Quaternion.Lerp(startRot, targetRot, ease);

            // BÍ QUYẾT TỐI THƯỢNG: Xoay ngược Hitbox lại!
            // Lá bài xoay -25 độ làm lạng mép trái? Không sao, ta ép Hitbox xoay ngược lại +25 độ.
            // Kết quả: Hình ảnh lá bài xoay rất đẹp, nhưng Hitbox cản chuột thì LUÔN ĐỨNG THẲNG như cũ!
            if (_hitBoxRect != null)
            {
                Quaternion currentHoverRot = Quaternion.Inverse(_baseRot) * transform.localRotation;
                _hitBoxRect.localRotation = Quaternion.Inverse(currentHoverRot);
            }

            yield return null;
        }

        transform.localPosition = targetPos;
        transform.localRotation = targetRot;

        if (_hitBoxRect != null)
        {
            Quaternion currentHoverRot = Quaternion.Inverse(_baseRot) * transform.localRotation;
            _hitBoxRect.localRotation = Quaternion.Inverse(currentHoverRot);
        }

        if (isHovering)
        {
            if (_contrastSprite != null) cardImage.sprite = _contrastSprite;
        }
        else
        {
            // Bay về tới ổ rồi thì tắt cmn quyền ưu tiên đi, trả lại cho Layout Group!
            canvas.overrideSorting = false;
        }

        _hoverCoroutine = null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (SettingsUI.IsOpen) return;

        if (_isPlayerCard)
        {
            _isDragging = true;
            _isHovered = false; // Ngừng hiệu ứng hover khi bắt đầu kéo

            if (CursorManager.Instance != null) CursorManager.Instance.SetDragCursor();

            if (_hoverCoroutine != null) StopCoroutine(_hoverCoroutine);
            if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
            
            cardImage.sprite = _normalSprite;

            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 200; // Ưu tiên hiển thị cao nhất khi đang kéo
            }

            RectTransform parentRect = transform.parent as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                _pointerDragOffset = (Vector2)transform.localPosition - localPointerPosition;
            }
            else
            {
                _pointerDragOffset = Vector2.zero;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SettingsUI.IsOpen) return;

        if (_isDragging)
        {
            RectTransform parentRect = transform.parent as RectTransform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPointerPosition))
            {
                Vector3 targetLocalPos = (Vector3)(localPointerPosition + _pointerDragOffset);
                
                // Tính vận tốc kéo theo trục X (độc lập với frame rate)
                float deltaX = targetLocalPos.x - transform.localPosition.x;
                float velocityX = deltaX / Time.deltaTime;

                transform.localPosition = targetLocalPos;

                // Càng kéo nhanh, bài càng nghiêng nhiều (giới hạn tối đa 35 độ)
                float tiltAngle = -velocityX * 0.015f;
                tiltAngle = Mathf.Clamp(tiltAngle, -35f, 35f);

                // Áp dụng góc nghiêng mục tiêu ngay lập tức
                Quaternion tiltRot = Quaternion.Euler(0, 0, tiltAngle);
                // Dùng Lerp một chút để không bị giật cục
                transform.localRotation = Quaternion.Lerp(transform.localRotation, tiltRot, 40f * Time.deltaTime);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isDragging)
        {
            _isDragging = false;
            
            if (CursorManager.Instance != null) CursorManager.Instance.SetNormalCursor();

            // Nếu kéo qua ngưỡng Y
            if (transform.localPosition.y - _basePos.y > playThresholdY)
            {
                // Hỏi Game Logic xem có được đánh không
                bool success = _onPlayCallback?.Invoke(_cardIndex) ?? false;
                
                if (success)
                {
                    PlayCard();
                }
                else
                {
                    _returnCoroutine = StartCoroutine(ReturnToBaseRoutine());
                }
            }
            else
            {
                _returnCoroutine = StartCoroutine(ReturnToBaseRoutine());
            }
        }
    }

    private void PlayCard()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = false;
            canvas.sortingOrder = 0;
        }
    }

    private IEnumerator ReturnToBaseRoutine()
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        
        float duration = 0.35f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = elapsed / duration;
            // Sử dụng hàm EaseOutBack để tạo hiệu ứng giật nảy nhẹ (bounce)
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
            
            transform.localPosition = Vector3.LerpUnclamped(startPos, _basePos, ease);
            transform.localRotation = Quaternion.LerpUnclamped(startRot, _baseRot, ease);
            yield return null;
        }
        
        transform.localPosition = _basePos;
        transform.localRotation = _baseRot;
        
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = false;
        }
        
        _returnCoroutine = null;
    }

    void Update()
    {
        // 1. Logic nảy (Bounce) khi đang Hover
        if (_isHovered && !_isDragging)
        {
            _hoverTimer += Time.deltaTime;

            if (_hoverCoroutine == null && _hoverTimer > delayBeforeBounce)
            {
                float bounceOffset = Mathf.Sin((_hoverTimer - delayBeforeBounce) * bounceSpeed) * bounceHeight;
                transform.localPosition = _basePos + (_baseRot * new Vector3(0, 40f + bounceOffset, 0));

                if (_contrastSprite != null)
                {
                    if (_hoverTimer % flashSpeed < (flashSpeed / 2f))
                        cardImage.sprite = _contrastSprite;
                    else
                        cardImage.sprite = _normalSprite;
                }
            }
        }

        // 3. Phục hồi góc nghiêng khi dừng kéo (Velocity Tilt recovery)
        if (_isDragging)
        {
            // Khi chuột dừng lại (OnDrag không được gọi hoặc deltaX = 0), bài sẽ tự động xoay thẳng đứng trở lại
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 15f);
        }
    }
}