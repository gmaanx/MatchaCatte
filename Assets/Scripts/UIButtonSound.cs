using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Tự động phát tiếng Click khi bấm. Nếu bạn đã setup trong Unity Event OnClick thì tắt cái này đi để tránh bị kêu 2 lần.")]
    public bool playClickSound = true;
    
    [Tooltip("Phát tiếng Hover khi lia chuột qua")]
    public bool playHoverSound = true;

    private Selectable _selectable;

    void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_selectable != null && _selectable.interactable)
        {
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetHoverCursor();
            }

            // Tạm tắt âm thanh hover theo yêu cầu, giữ nguyên hàm để không gây lỗi logic EventSystem
            /*
            if (playHoverSound && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBtnHover();
            }
            */
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_selectable != null && _selectable.interactable)
        {
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetNormalCursor();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (playClickSound && _selectable != null && _selectable.interactable)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBtnClick();
            }
        }
    }
}
