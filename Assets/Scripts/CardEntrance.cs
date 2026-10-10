using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class CardEntrance : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Vector3 _originalScale;

    private GameObject _activeDummyObj;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _originalScale = transform.localScale;

        // Vừa đẻ ra là tàng hình liền để chờ bài ảo bay tới, KHÔNG cho bấm vào khi đang tàng hình
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    void OnDestroy()
    {
        // Nếu lá bài bị phá hủy đột ngột (do Sort hoặc chia ván mới) khi đang bay
        // thì phải dọn dẹp cái dummy rác đi để nó không bị kẹt lại vĩnh viễn trên bàn
        if (_activeDummyObj != null)
        {
            Destroy(_activeDummyObj);
        }
    }

    // --- HIỆU ỨNG 1: BAY TỪ DECK RA (Dùng lúc chia bài) ---
    public void PlayFly(float delay, Transform deck, Sprite backSprite)
    {
        StartCoroutine(FlyRoutine(delay, deck, backSprite));
    }

    // --- HIỆU ỨNG 2: NẢY POP-UP ---
    public void PlayPop(float delay = 0f)
    {
        StartCoroutine(PopRoutine(delay));
    }

    // --- HIỆU ỨNG 3: HIỆN NGAY LẬP TỨC (Dùng lúc update lại tay bài, chống giật) ---
    public void ShowInstant()
    {
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true; // Bay xong rồi, cho phép bấm/kéo
        transform.localScale = Vector3.one; // Ép về tỷ lệ chuẩn để chắc chắn không bị tàng hình (scale = 0)
    }

    // --- HIỆU ỨNG 4: NÉM BÀI XUỐNG BÀN (TỪ TAY RA BÀN) ---
    public void PlayShrinkToTable(float targetScale = 0.7f, float duration = 0.45f, Transform startTransform = null, float delay = 0f)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            StartCoroutine(ShrinkAndFlipRoutine(rect, targetScale, duration, startTransform, delay));
        }
    }

    IEnumerator FlyRoutine(float delay, Transform deck, Sprite backSprite)
    {
        _canvasGroup.alpha = 0f;
        if (delay > 0) yield return new WaitForSeconds(delay);

        // BÍ QUYẾT: Đợi thêm 1 frame để Layout Group xếp bài xong, mình mới ăn cắp được đúng tọa độ đích!
        yield return new WaitForEndOfFrame();

        RectTransform realRect = GetComponent<RectTransform>();
        Vector3 targetPos = realRect.position;

        Image realImg = GetComponent<Image>();
        Sprite frontSprite = realImg.sprite; // Lưu lại mặt trước thực tế

        GameObject dummyObj = new GameObject("FlyingDummy");
        _activeDummyObj = dummyObj;
        dummyObj.transform.SetParent(deck.parent);
        dummyObj.transform.SetAsLastSibling();

        Image dummyImg = dummyObj.AddComponent<Image>();
        dummyImg.sprite = backSprite;
        RectTransform dummyRect = dummyObj.GetComponent<RectTransform>();

        dummyRect.sizeDelta = realRect.sizeDelta;
        dummyRect.position = deck.position;
        dummyRect.localScale = Vector3.zero;

        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startPos = deck.position;
        float arcHeight = 60f; // Quỹ đạo cong (arc) mô phỏng dealer quăng bài
        Quaternion targetRot = realRect.rotation;
        
        bool hasPlayedSound = false;
        float randomZSpin = UnityEngine.Random.Range(-40f, 40f); // Spin hỗn loạn tự do

        // Hàm EaseOutBack để tạo hiệu ứng trượt lố đà và nảy lại
        float EaseOutBack(float x) {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Bù trừ độ trễ của file âm thanh
            if (!hasPlayedSound && elapsed >= duration - 0.08f)
            {
                hasPlayedSound = true;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayDealCard();
            }

            float t = elapsed / duration;
            float easeOutBack = EaseOutBack(t);
            float easeOutQuart = 1f - Mathf.Pow(1f - t, 4);

            // 1. Chuyển động theo đường cong (Arc) + Landing Overshoot (trượt lố)
            Vector3 currentPos = Vector3.LerpUnclamped(startPos, targetPos, easeOutBack);
            currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
            dummyRect.position = currentPos;

            // Đồng thời phóng to lố đà rồi thu nhỏ về bình thường
            dummyRect.localScale = _originalScale * easeOutBack;

            // 2. Trục Z (Spin 2D): Xoay nghiêng ngả tự do trong không trung
            float currentZSpin = Mathf.Lerp(randomZSpin, 0f, easeOutQuart);

            // 3. Trục Y (Flip 3D): Lật từ úp sang ngửa (Dùng mánh 0->90->0 để không bị ngược hình)
            float rotY = 0f;
            if (t < 0.5f)
            {
                rotY = Mathf.Lerp(0f, 90f, t / 0.5f);
                dummyImg.sprite = backSprite;
            }
            else
            {
                rotY = Mathf.Lerp(90f, 0f, (t - 0.5f) / 0.5f);
                dummyImg.sprite = frontSprite;
            }

            // Gộp tất cả các góc xoay lại
            dummyRect.rotation = targetRot * Quaternion.Euler(0, rotY, currentZSpin);

            yield return null;
        }

        Destroy(dummyObj);
        _activeDummyObj = null;
        ShowInstant();
        
        // Phòng hờ trường hợp rớt frame dẫn đến chưa chạy if ở trên
        if (!hasPlayedSound && AudioManager.Instance != null) 
        {
            AudioManager.Instance.PlayDealCard();
        }
    }

    IEnumerator PopRoutine(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeOutBack = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t * 1.5f);
            transform.localScale = _originalScale * Mathf.Lerp(0.5f, 1f, easeOutBack);
            yield return null;
        }
        ShowInstant();
    }

    IEnumerator ShrinkAndFlipRoutine(RectTransform rect, float targetScale, float duration, Transform startTransform, float delay)
    {
        // 1. Tạm thời giấu lá bài đi (để Layout Group ngầm sắp xếp chỗ trước)
        _canvasGroup.alpha = 0f;

        if (delay > 0) yield return new WaitForSeconds(delay);

        // BÍ QUYẾT: Đợi 1 frame cho Layout Group tính xong tọa độ đích
        yield return new WaitForEndOfFrame();

        // Giờ mới hiện nó lên để bắt đầu bay
        _canvasGroup.alpha = 1f;

        Vector3 endPos = rect.position;
        Vector3 startPos = endPos;
        if (startTransform != null)
        {
            startPos = startTransform.position;
        }

        Vector3 endScale = new Vector3(targetScale, targetScale, 1f);
        Vector3 startScale = _originalScale;

        float arcHeight = 40f; // Quỹ đạo cong mô phỏng lực ném
        float elapsed = 0f;

        // Lấy Sprite mặt trước hiện tại của bài
        Image img = GetComponent<Image>();
        Sprite frontSprite = img.sprite;
        Sprite backSprite = frontSprite;
        
        // Cố gắng tìm Sprite mặt lưng từ UnityView để thực hiện cú lật đẹp mắt
        UnityView view = FindObjectOfType<UnityView>();
        if (view != null) backSprite = view.cardBackSprite;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float easeOut = 1f - Mathf.Pow(1f - t, 3);

            // Hiệu ứng Chiều sâu: Kích thước thu nhỏ lại một chút
            rect.localScale = Vector3.Lerp(startScale, endScale, easeOut);

            // Quỹ đạo cong (Arc)
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, easeOut);
            float arcY = Mathf.Sin(t * Mathf.PI) * arcHeight;
            currentPos.y += arcY;
            rect.position = currentPos;

            // --- XỬ LÝ XOAY LẬT 360 ĐỘ ---
            // Quay từ 0 -> 360 độ theo trục dọc Y
            float rotY = Mathf.Lerp(0f, 360f, easeOut);

            // Trục Z: Nghiêng nhẹ lúc bay và về 0 khi đáp
            float rotZ = Mathf.Lerp(-15f, 0f, easeOut);
            rect.rotation = Quaternion.Euler(0, rotY, rotZ);

            // Đổi Sprite giữa chừng để người chơi nhìn thấy lướt qua mặt lưng của lá bài
            float modRot = rotY % 360f;
            if (modRot > 90f && modRot < 270f)
            {
                if (img.sprite != backSprite) img.sprite = backSprite;
            }
            else
            {
                if (img.sprite != frontSprite) img.sprite = frontSprite;
            }

            yield return null;
        }

        // Hạ cánh: Chốt sổ đúng vị trí, scale và góc thẳng đứng
        rect.position = endPos;
        rect.localScale = endScale;
        rect.rotation = Quaternion.identity;
        img.sprite = frontSprite; // Hoàn toàn lật ngửa khi hạ cánh
        
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCardSound();
    }
}