using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;
using cattePrototype;
using System.Linq;

public class UnityView : MonoBehaviour, IGameView
{
    // class này đóng vai trò View, chỉ chuyên lo hiển thị UI.
    // các biến public dưới đây được giữ nguyên để ánh xạ (binding) trong Unity Inspector.
    [Header("Hiệu ứng chia bài bay")]
    public Transform deckTransform;

    [Header("Background FX")]
    [Tooltip("Kéo object phông nền vào đây (San_Dau hoặc Liquid_Background)")]
    public GameObject backgroundObject;
    public Material _bgMaterial;

    private int _currentRound = 1;
    private Coroutine _bgEffectCoroutine;

    [Header("UI Layouts")]
    public Transform playerHandContainer;

    public Transform[] botHandContainers;
    public GameObject cardPrefab;

    public Transform playerPlayedContainer;
    public Transform[] botPlayedContainers;

    [Header("Kho Dữ Liệu Ảnh")]
    public Sprite[] allCardSprites; // Cần đúng 104 hình (52 gốc + 52 contrast)
    public Sprite cardBackSprite;

    [Header("Game Over UI")]
    public GameObject gameOverPanel;
    public Text txtFinalResult;

    private TaskCompletionSource<int> _playerInputTcs;
    private Card _currentHighestCard;

    [Header("Sort Buttons")]
    public Button btnSortRank;
    public Button btnSortSuit;

    // Biến này để "bắt" tay bài của m lại, lát nút Sort mới biết mảng nào mà xử lý
    private Player _localHumanPlayer;

    [Header("Turn Indicator")]
    public TurnIndicator turnArrow;

    [Header("Counters UI")]
    public Text txtPlaysCount;
    public Text txtFoldsCount;
    public Text txtRoundCount;

    private int _playsCount = 0;
    private int _foldsCount = 0;

    private bool _isDealing = false;

    async void Start()
    {
        // Tự động tìm phông nền nếu bạn chưa kéo vào (để khỏi phải kéo thủ công)
        if (backgroundObject == null)
        {
            UnityEngine.UI.Graphic[] allGraphics = FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var g in allGraphics)
            {
                if (g.material != null && g.material.shader != null && g.material.shader.name == "UI/BalatroBackground")
                {
                    backgroundObject = g.gameObject;
                    break;
                }
            }
        }

        if (backgroundObject != null)
        {
            var graphic = backgroundObject.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                _bgMaterial = new Material(graphic.material);
                graphic.material = _bgMaterial;
            }
        }

        if (btnSortRank != null) btnSortRank.onClick.AddListener(SortByRank);
        if (btnSortSuit != null) btnSortSuit.onClick.AddListener(SortBySuit);
        // Vừa vào game là tự động giấu màng đen
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Khởi tạo Text đếm số về 0 lúc mới bắt đầu
        if (txtPlaysCount != null) txtPlaysCount.text = "0";
        if (txtFoldsCount != null) txtFoldsCount.text = "0";
        if (txtRoundCount != null) txtRoundCount.text = "0/6";

        // Thêm script xếp bài cong vào các tay bài
        if (playerHandContainer != null) playerHandContainer.gameObject.AddComponent<HandCurve>();
        
        // Căn chỉnh lại vị trí và góc quay của 3 Bot sao cho nằm sát 3 cạnh màn hình
        if (botHandContainers != null && botHandContainers.Length >= 3)
        {
            // Bot 1 (Bên trái): Căn lề trái giữa, xoay -90 độ
            RectTransform bot1 = botHandContainers[0] as RectTransform;
            if (bot1 != null)
            {
                bot1.anchorMin = new Vector2(0, 0.5f);
                bot1.anchorMax = new Vector2(0, 0.5f);
                bot1.pivot = new Vector2(0.5f, 0.5f);
                bot1.anchoredPosition = new Vector2(100f, 0); // Cách lề trái 100px
                bot1.localRotation = Quaternion.Euler(0, 0, -90f);
            }

            // Bot 2 (Bên trên): Căn lề trên giữa, xoay 180 độ (lộn ngược)
            RectTransform bot2 = botHandContainers[1] as RectTransform;
            if (bot2 != null)
            {
                bot2.anchorMin = new Vector2(0.5f, 1f);
                bot2.anchorMax = new Vector2(0.5f, 1f);
                bot2.pivot = new Vector2(0.5f, 0.5f);
                bot2.anchoredPosition = new Vector2(0, -100f); // Cách lề trên 100px
                bot2.localRotation = Quaternion.Euler(0, 0, 180f);
            }

            // Bot 3 (Bên phải): Căn lề phải giữa, xoay 90 độ
            RectTransform bot3 = botHandContainers[2] as RectTransform;
            if (bot3 != null)
            {
                bot3.anchorMin = new Vector2(1f, 0.5f);
                bot3.anchorMax = new Vector2(1f, 0.5f);
                bot3.pivot = new Vector2(0.5f, 0.5f);
                bot3.anchoredPosition = new Vector2(-100f, 0); // Cách lề phải 100px
                bot3.localRotation = Quaternion.Euler(0, 0, 90f);
            }

            foreach (var botContainer in botHandContainers)
            {
                if (botContainer != null)
                {
                    HandCurve curve = botContainer.gameObject.AddComponent<HandCurve>();
                    curve.spacing = 35f; // Bot có spacing thấp hơn để không tốn diện tích
                    curve.anglePerCard = 5f;
                }
            }
        }

        int diff = 1;
        if (AudioManager.Instance != null)
        {
            diff = AudioManager.Instance.botDifficulty;
        }

        List<Player> players = new List<Player>
        {
            new HumanPlayer("You"),
            CreateBot("Bot 1", diff),
            CreateBot("Bot 2", diff),
            CreateBot("Bot 3", diff)
        };

        GameManager game = new GameManager(this, players);
        await game.StartGame();
    }

    private Player CreateBot(string name, int difficulty)
    {
        switch (difficulty)
        {
            case 0: return new BotDe(name);
            case 1: return new BotVua(name);
            case 2: return new BotKho(name);
            default: return new BotKho(name);
        }
    }

    public void ShowMessage(string message)
    {
        Debug.Log($"[Game]: {message}");

        // --- BẮT LÉN KẾT QUẢ TỔNG KẾT TỪ VÒNG 1 ĐẾN 5 ---
        if (message.Contains("winner:"))
        {
            UpdateRoundResult(message.Contains("You"));
        }
    }

    // Hàm tổng kết đếm số chuẩn theo luật Catte
    private void UpdateRoundResult(bool iWonRound)
    {
        // Max 6 vòng (Play + Fold)
        if ((_playsCount + _foldsCount) >= 6) return;

        // Chỉ đếm nếu vòng này m CÓ THAM GIA (có vứt bài ra sân)
        // Lỡ m chết tùng ở vòng 4, vòng 5 vòng 6 m không vứt lá nào thì không bị đếm oan
        if (playerPlayedContainer != null && playerPlayedContainer.childCount > 0)
        {
            if (iWonRound)
            {
                _playsCount++;
                if (txtPlaysCount != null) txtPlaysCount.text = _playsCount.ToString();
            }
            else
            {
                _foldsCount++;
                if (txtFoldsCount != null) txtFoldsCount.text = _foldsCount.ToString();
            }
            UpdateBackgroundShader();
        }
    }

    public void ShowHand(Player player)
    {
        ShowHand(player, false, 0);
    }

    public void ShowHand(Player player, bool playAnimation, int playerIndex)
    {
        if (player is HumanPlayer)
        {
            _localHumanPlayer = player;
            foreach (Transform child in playerHandContainer) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            
            HandCurve playerCurve = playerHandContainer.GetComponent<HandCurve>();
            if (playerCurve != null) playerCurve.ForceUpdate();

            for (int i = 0; i < player.Hand.Count; i++)
            {
                GameObject newCard = Instantiate(cardPrefab, playerHandContainer);
                Sprite exactSprite = GetCardSprite(player.Hand[i]);
                Sprite contrastSprite = GetContrastSprite(player.Hand[i]);

                newCard.GetComponent<CardUI>().Setup(exactSprite, contrastSprite, i, OnCardDragAttempt, true);

                CardEntrance entrance = newCard.GetComponent<CardEntrance>();
                if (entrance == null) entrance = newCard.AddComponent<CardEntrance>();

                if (playAnimation && deckTransform != null)
                {
                    _isDealing = true;
                    CancelInvoke(nameof(UnlockActions));
                    Invoke(nameof(UnlockActions), 1.6f); // 1.6s để chờ 24 lá bay xong

                    int dealIndex = GetCenterOutIndex(i, player.Hand.Count);
                    // Giảm hệ số delay để chia bài nhanh lướt hơn (vd 0.04s mỗi lá)
                    float globalDelay = ((dealIndex * 4) + playerIndex) * 0.04f;
                    entrance.PlayFly(globalDelay, deckTransform, cardBackSprite);
                }
                else
                {
                    // Lướt thẳng, không popup lộn xộn lúc update lại tay bài
                    entrance.ShowInstant();
                }
            }
        }
        else
        {
            Transform botContainer = null;
            if (player.Name == "Bot 1") botContainer = botHandContainers[0];
            else if (player.Name == "Bot 2") botContainer = botHandContainers[1];
            else if (player.Name == "Bot 3") botContainer = botHandContainers[2];

            if (botContainer != null)
            {
                foreach (Transform child in botContainer) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                for (int i = 0; i < player.Hand.Count; i++)
                {
                    GameObject newCard = Instantiate(cardPrefab, botContainer);
                    newCard.GetComponent<CardUI>().Setup(cardBackSprite, null, -1, null, false);

                    CardEntrance entrance = newCard.GetComponent<CardEntrance>();
                    if (entrance == null) entrance = newCard.AddComponent<CardEntrance>();

                    if (playAnimation && deckTransform != null)
                    {
                        int dealIndex = GetCenterOutIndex(i, player.Hand.Count);
                        float globalDelay = ((dealIndex * 4) + playerIndex) * 0.04f;
                        entrance.PlayFly(globalDelay, deckTransform, cardBackSprite);
                    }
                    else
                    {
                        entrance.ShowInstant();
                    }
                }
            }
        }
    }

    public void ClearTable()
    {
        List<GameObject> cardsToClear = new List<GameObject>();

        // 1. Thu thập bài trên sân người chơi
        if (playerPlayedContainer != null)
        {
            foreach (Transform child in playerPlayedContainer) cardsToClear.Add(child.gameObject);
        }

        // 2. Thu thập bài sân của 3 Bot
        if (botPlayedContainers != null)
        {
            foreach (Transform container in botPlayedContainers)
            {
                if (container != null)
                {
                    foreach (Transform child in container) cardsToClear.Add(child.gameObject);
                }
            }
        }
        
        if (cardsToClear.Count > 0)
        {
            foreach (var card in cardsToClear)
            {
                if (card != null)
                {
                    UnityEngine.UI.LayoutElement le = card.GetComponent<UnityEngine.UI.LayoutElement>();
                    if (le == null) le = card.AddComponent<UnityEngine.UI.LayoutElement>();
                    le.ignoreLayout = true;
                }
            }
            StartCoroutine(BlackHoleRoutine(cardsToClear));
        }
    }

    private System.Collections.IEnumerator BlackHoleRoutine(List<GameObject> cards)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        // Tâm màn hình
        Vector3 centerPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        
        // Tọa độ gốc của các lá bài
        Vector3[] startPositions = new Vector3[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] != null) startPositions[i] = cards[i].transform.position;
        }
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easeIn = t * t * t; // Lực hút tăng dần
            
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    // Bay về tâm
                    cards[i].transform.position = Vector3.Lerp(startPositions[i], centerPos, easeIn);
                    
                    // Xoay tít mù quanh trục Z
                    cards[i].transform.Rotate(0, 0, 1500f * Time.deltaTime);
                    
                    // Thu nhỏ lại
                    cards[i].transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, easeIn);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Dọn rác
        foreach (GameObject card in cards)
        {
            if (card != null) Destroy(card);
        }
    }

    public void ShowCardOnTable(string playerName, Card card, bool isFolded)
    {
        Transform targetContainer = null;

        // Dò xem ai là người đánh để trỏ vào đúng sân
        if (playerName == "You")
        {
            targetContainer = playerPlayedContainer;
        }
        else if (playerName == "Bot 1")
        {
            targetContainer = botPlayedContainers[0];
        }
        else if (playerName == "Bot 2")
        {
            targetContainer = botPlayedContainers[1];
        }
        else if (playerName == "Bot 3")
        {
            targetContainer = botPlayedContainers[2];
        }

        // Nếu tìm thấy đúng sân thì ném bài vào
        if (targetContainer != null)
        {
            GameObject newCard = Instantiate(cardPrefab, targetContainer);
            Sprite displaySprite = isFolded ? cardBackSprite : GetCardSprite(card);

            newCard.GetComponent<CardUI>().Setup(displaySprite, null, -1, null, false);

            CardEntrance entrance = newCard.GetComponent<CardEntrance>();
            if (entrance == null) entrance = newCard.AddComponent<CardEntrance>();

            // Cung cấp container chứa bài trên tay tương ứng với người đánh
            Transform handContainer = playerName == "You" ? playerHandContainer :
                                      playerName == "Bot 1" ? botHandContainers[0] :
                                      playerName == "Bot 2" ? botHandContainers[1] :
                                      botHandContainers[2];

            // Truyền khay bài (handContainer) vào làm điểm xuất phát bay ra
            entrance.PlayShrinkToTable(0.85f, 0.35f, handContainer);
            
            // --- BỔ SUNG UX: GLITCH MÀN HÌNH & HIT-STOP ---
            if (!isFolded && (card.Rank == Rank.A || card.Rank == Rank.K))
            {
                StartCoroutine(CRTGlitchRoutine(0.4f, 0.8f));
                StartCoroutine(HitStopRoutine(0.15f));
            }
        }
    }

    private System.Collections.IEnumerator CRTGlitchRoutine(float duration, float maxGlitch)
    {
        if (_bgMaterial == null) yield break;
        
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float currentGlitch = Mathf.Lerp(maxGlitch, 0f, elapsed / duration);
            _bgMaterial.SetFloat("_Glitch", currentGlitch);
            
            elapsed += Time.unscaledDeltaTime; 
            yield return null;
        }

        _bgMaterial.SetFloat("_Glitch", 0f);
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f; // Đóng băng thời gian gần như hoàn toàn
        yield return new WaitForSecondsRealtime(duration);
        _bgMaterial.SetFloat("_HitStop", 0f);
        Time.timeScale = 1.0f;
    }

    public void HighlightWinningCard(string playerName)
    {
        Transform targetContainer = null;
        if (playerName == "You") targetContainer = playerPlayedContainer;
        else if (playerName == "Bot 1") targetContainer = botPlayedContainers[0];
        else if (playerName == "Bot 2") targetContainer = botPlayedContainers[1];
        else if (playerName == "Bot 3") targetContainer = botPlayedContainers[2];

        if (targetContainer != null && targetContainer.childCount > 0)
        {
            Transform winningCardTransform = targetContainer.GetChild(0);
            StartCoroutine(WinningGlowRoutine(winningCardTransform));
        }
    }

    private System.Collections.IEnumerator WinningGlowRoutine(Transform cardTransform)
    {
        Vector3 originalPos = cardTransform.localPosition;
        Vector3 originalScale = cardTransform.localScale;
        Quaternion originalRot = cardTransform.localRotation;
        
        Image img = cardTransform.GetComponent<CardUI>()?.cardImage;
        if (img == null) img = cardTransform.GetComponent<Image>();
        Color originalColor = img != null ? img.color : Color.white;
        
        // Setup Outline (Viền chớp sáng)
        UnityEngine.UI.Outline outline = cardTransform.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 1f, 1f, 0f); // Cyan mờ
        outline.effectDistance = new Vector2(10, -10);
        
        // Setup Dynamic Shadow (Bóng 3D)
        UnityEngine.UI.Shadow shadow = cardTransform.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        Vector2 baseShadowDist = new Vector2(4, -4);
        shadow.effectDistance = baseShadowDist;

        float duration = 1.0f; 
        float elapsed = 0f;
        float bumpHeight = 60f; // Búng cao lên 60 pixels
        
        AudioManager.Instance?.PlayHighlightSound();
        
        while (elapsed < duration)
        {
            if (cardTransform == null) yield break;
            
            float t = elapsed;
            float yOffset = 0f;
            float outlineAlpha = 0f;
            float shadowDistMultiplier = 1f;
            float shadowAlpha = 0.6f;
            
            // Giai đoạn 1: Búng lên cực nhanh (0s -> 0.15s)
            if (t < 0.15f) 
            {
                float u = t / 0.15f;
                float easeOut = 1f - (1f - u)*(1f - u);
                yOffset = Mathf.Lerp(0, bumpHeight, easeOut);
                
                // Lóe sáng trắng/cyan (Flash)
                if (img != null) img.color = Color.Lerp(Color.white, originalColor, u);
                
                // Viền sáng dần lên đỉnh điểm
                outlineAlpha = Mathf.Lerp(0f, 1f, u);
                
                // Bóng giãn ra xa và nhạt đi
                shadowDistMultiplier = Mathf.Lerp(1f, 3f, easeOut);
                shadowAlpha = Mathf.Lerp(0.6f, 0.2f, easeOut);
            }
            // Giai đoạn 2: Rớt xuống quá trớn (0.15s -> 0.35s)
            else if (t < 0.35f)
            {
                float u = (t - 0.15f) / 0.2f;
                float easeIn = u * u;
                yOffset = Mathf.Lerp(bumpHeight, -15f, easeIn);
                
                if (img != null) img.color = originalColor;
                
                // Viền mờ đi khi rớt
                outlineAlpha = Mathf.Lerp(1f, 0f, u);
                
                // Bóng thu lại
                shadowDistMultiplier = Mathf.Lerp(3f, 0.7f, easeIn);
                shadowAlpha = Mathf.Lerp(0.2f, 0.7f, easeIn);
            }
            // Giai đoạn 3: Nảy nhẹ về vị trí cũ (OutElastic / OutBack) (0.35s -> 0.5s)
            else if (t < 0.5f)
            {
                float u = (t - 0.35f) / 0.15f;
                float easeOut = 1f - (1f - u)*(1f - u);
                yOffset = Mathf.Lerp(-15f, 0f, easeOut);
                
                outlineAlpha = 0f;
                shadowDistMultiplier = Mathf.Lerp(0.7f, 1f, easeOut);
                shadowAlpha = Mathf.Lerp(0.7f, 0.6f, easeOut);
            }
            // Giai đoạn 4: Nghỉ (0.5s -> 1.0s)
            else
            {
                yOffset = 0f;
                outlineAlpha = 0f;
                shadowDistMultiplier = 1f;
                shadowAlpha = 0.6f;
            }
            
            // Cập nhật Transform & Effects
            cardTransform.localPosition = originalPos + new Vector3(0, yOffset, 0);
            
            // Lắc xoay nghiêng theo tỷ lệ búng (búng lên thì nghiêng 10 độ, rớt xuống lố thì nghiêng nhẹ ngược lại)
            float rotOffset = (yOffset / bumpHeight) * 10f;
            cardTransform.localRotation = originalRot * Quaternion.Euler(0, 0, rotOffset);
            
            outline.effectColor = new Color(0f, 1f, 1f, outlineAlpha);
            shadow.effectDistance = baseShadowDist * shadowDistMultiplier;
            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Đảm bảo dọn dẹp sạch sẽ
        if (cardTransform != null)
        {
            cardTransform.localPosition = originalPos;
            cardTransform.localScale = originalScale;
            cardTransform.localRotation = originalRot;
            if (img != null) img.color = originalColor;
            
            Destroy(outline);
            Destroy(shadow);
        }
    }

    public Task<int> GetPlayerInput(Player player, Card currentHighest)
    {
        _playerInputTcs = new TaskCompletionSource<int>();
        _currentHighestCard = currentHighest;
        
        RefreshPredictionMode();

        return _playerInputTcs.Task;
    }

    public void RefreshPredictionMode()
    {
        // Chỉ chạy nếu đang trong lượt chọn bài của người chơi
        if (_playerInputTcs != null && !_playerInputTcs.Task.IsCompleted)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.predictionMode)
            {
                UpdatePlayability(_currentHighestCard);
            }
            else
            {
                // Reset lại toàn bộ lá bài về bình thường nếu tắt Prediction Mode
                if (_localHumanPlayer == null) return;
                for (int i = 0; i < playerHandContainer.childCount; i++)
                {
                    Transform child = playerHandContainer.GetChild(i);
                    if (child.gameObject.activeSelf)
                    {
                        CardUI cardUI = child.GetComponent<CardUI>();
                        if (cardUI != null) cardUI.SetPlayable(true);
                    }
                }
            }
        }
    }

    public void UpdatePlayability(Card currentHighest)
    {
        if (_localHumanPlayer == null) return;
        
        for (int i = 0; i < playerHandContainer.childCount; i++)
        {
            Transform child = playerHandContainer.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                CardUI cardUI = child.GetComponent<CardUI>();
                if (cardUI != null && cardUI.CardIndex < _localHumanPlayer.Hand.Count)
                {
                    Card card = _localHumanPlayer.Hand[cardUI.CardIndex];
                    bool isPlayable = currentHighest == null || card.CanBeat(currentHighest);
                    cardUI.SetPlayable(isPlayable);
                }
            }
        }
    }

    public Task<int> GetDifficultyChoice()
    {
        return Task.FromResult(1);
    }

    private bool OnCardDragAttempt(int index)
    {
        if (_isDealing) return false;

        if (_playerInputTcs != null && !_playerInputTcs.Task.IsCompleted)
        {
            _playerInputTcs.TrySetResult(index);
            return true;
        }
        return false;
    }

    private void UnlockActions()
    {
        _isDealing = false;
    }

    private int GetCenterOutIndex(int i, int count)
    {
        int[] order = new int[count];
        int left = (count - 1) / 2;
        int right = left + 1;
        int current = 0;
        
        if (count % 2 != 0) {
            order[left] = current++;
            left--;
        }
        while (left >= 0 || right < count) {
            if (right < count) order[right] = current++;
            if (left >= 0) order[left] = current++;
            right++;
            left--;
        }
        return order[i];
    }

    // --- CÁC HÀM XỬ LÝ LOGIC UI ---

    private Sprite GetCardSprite(Card card)
    {
        int row = 0;
        switch (card.Suit)
        {
            case Suit.Spades: row = 0; break;   // Bích hàng 1
            case Suit.Hearts: row = 1; break;   // Cơ hàng 2
            case Suit.Clubs: row = 2; break;    // Chuồn hàng 3
            case Suit.Diamonds: row = 3; break; // Rô hàng 4
        }

        int col = 0;
        // Át cột 0. 
        // Bài 2 (enum 0) + 1 = cột 1. Bài K (enum 11) + 1 = cột 12. ĐÚNG TOÁN!
        if (card.Rank == Rank.A) col = 0;
        else col = (int)card.Rank + 1;

        int spriteIndex = (row * 13) + col;

        if (spriteIndex >= 0 && spriteIndex < allCardSprites.Length)
        {
            return allCardSprites[spriteIndex];
        }

        return cardBackSprite;
    }

    private Sprite GetContrastSprite(Card card)
    {
        int row = 0;
        switch (card.Suit)
        {
            case Suit.Spades: row = 0; break;
            case Suit.Hearts: row = 1; break;
            case Suit.Clubs: row = 2; break;
            case Suit.Diamonds: row = 3; break;
        }

        int col = 0;
        if (card.Rank == Rank.A) col = 0;
        else col = (int)card.Rank + 1;

        // Nhảy xuống 52 lá để lấy contrast
        int contrastIndex = (row * 13) + col + 52;

        if (contrastIndex >= 0 && contrastIndex < allCardSprites.Length)
        {
            return allCardSprites[contrastIndex];
        }

        return null;
    }

    public void ShowGameOver(string finalMessage)
    {
        HideTurnIndicator();

        // --- BẮT LÉN KẾT QUẢ VÒNG 6 (CHUNG KẾT) & XỬ LÝ NẾU BỊ CHẾT TÙNG ---
        if (finalMessage.Contains("WINNER:") || finalMessage.Contains("KNOCKOUT:"))
        {
            UpdateRoundResult(finalMessage.Contains("You"));
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (txtFinalResult != null) txtFinalResult.text = finalMessage;
        
        if (AudioManager.Instance != null)
        {
            if (finalMessage.Contains("You") || finalMessage.Contains("YOU WIN"))
            {
                AudioManager.Instance.PlayWinRound();
            }
            else
            {
                AudioManager.Instance.PlayLoseRound();
            }
        }

        foreach (Transform child in playerHandContainer)
        {
            UnityEngine.UI.Button btn = child.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = false;
        }
    }

    public void OnRestartClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SortByRank()
    {
        if (_isDealing) return;

        // Kiểm tra an toàn: Có người chơi, có bài, và danh sách không rỗng
        if (_localHumanPlayer == null || _localHumanPlayer.Hand == null || _localHumanPlayer.Hand.Count == 0) return;

        _localHumanPlayer.SortHandByRank();

        // Re-render UI (gọi hàm 1 tham số để show instant)
        ShowHand(_localHumanPlayer);
    }

    private void SortBySuit()
    {
        if (_isDealing) return;

        if (_localHumanPlayer == null || _localHumanPlayer.Hand == null || _localHumanPlayer.Hand.Count == 0) return;

        // Sort tại chỗ theo 2 điều kiện: Chất trước, Rank sau
        _localHumanPlayer.SortHandBySuit();

        ShowHand(_localHumanPlayer);
    }

    public void ShowTurnIndicator(string playerName)
    {
        // deckTransform chính là cái "Deck_Visual" m đã kéo vào Unity từ đầu
        if (turnArrow == null || deckTransform == null) return;

        RectTransform deckRect = deckTransform as RectTransform;
        if (deckRect != null)
        {
            // Bắn tên người chơi và vị trí cọc bài cho mũi tên tự xử lý
            turnArrow.PointTo(playerName, deckRect);
        }
    }

    public void HideTurnIndicator()
    {
        if (turnArrow != null) turnArrow.Hide();
    }

    public void UpdateRoundCount(int currentRound, int totalRounds)
    {
        _currentRound = currentRound;
        if (txtRoundCount != null)
        {
            txtRoundCount.text = $"{currentRound}/{totalRounds}";
        }
        UpdateBackgroundShader();
    }

    private void UpdateBackgroundShader()
    {
        if (_bgMaterial == null) return;
        
        float targetGrayscale = 0f;
        float targetGalaxy = 0f;
        
        if (_currentRound >= 5)
        {
            if (_foldsCount >= 4) 
            {
                targetGrayscale = 1f;
            }
            else if (_playsCount != 0)
            {
                targetGalaxy = 1f;
            }
        }
        
        if (_bgEffectCoroutine != null) StopCoroutine(_bgEffectCoroutine);
        _bgEffectCoroutine = StartCoroutine(FadeBackgroundEffect(targetGrayscale, targetGalaxy));
    }

    // --- Hiệu ứng nền ---
    public void UpdateBackgroundEffect()
    {
        UpdateBackgroundShader();
    }

    private System.Collections.IEnumerator FadeBackgroundEffect(float targetGrayscale, float targetGalaxy)
    {
        if (_bgMaterial == null) yield break;

        float startGrayscale = _bgMaterial.GetFloat("_Grayscale");
        float startGalaxy = _bgMaterial.GetFloat("_GalaxyBlend");
        
        // --- Kích hoạt SFX khi bắt đầu hiện Background FX ---
        if (targetGrayscale == 1f && startGrayscale < 0.5f)
        {
            PlayGrayscaleSFX();
        }
        else if (targetGalaxy == 1f && startGalaxy < 0.5f)
        {
            PlayDimensionBreakSFX();
        }

        float elapsed = 0f;
        float duration = 1.0f; // Thời gian chuyển màu 1 giây
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            _bgMaterial.SetFloat("_Grayscale", Mathf.Lerp(startGrayscale, targetGrayscale, t));
            _bgMaterial.SetFloat("_GalaxyBlend", Mathf.Lerp(startGalaxy, targetGalaxy, t));
            
            // Chỉ nhiễu khi LẦN ĐẦU TIÊN chuyển sang Đen Trắng, nếu đang Đen Trắng sẵn (như vòng 6) thì không nhiễu lại
            float glitchAmount = 0f;
            if (targetGrayscale > 0.5f && startGrayscale < 0.5f)
            {
                // Nhiễu chớp nhoáng ở giữa quá trình chuyển màu
                glitchAmount = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 4f) * 0.5f;
            }
            _bgMaterial.SetFloat("_Glitch", glitchAmount);
            
            yield return null;
        }
        
        _bgMaterial.SetFloat("_Grayscale", targetGrayscale);
        _bgMaterial.SetFloat("_GalaxyBlend", targetGalaxy);
        _bgMaterial.SetFloat("_Glitch", 0f); // Tắt hẳn nhiễu sau khi chuyển xong
    }

    public void PlayDimensionBreakSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDimensionBreakSound();
        }
    }

    public void PlayGrayscaleSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGrayscaleSound();
        }
    }
}

public class HandCurve : MonoBehaviour
{
    // Thông số cong bài điều chỉnh cho giống với ảnh reference
    public float spacing = 100f; 
    public float anglePerCard = 3f;
    public float yCurve = 3f;

    private int _lastValidCount = -1;
    private int _lastTotalCount = -1;

    public void ForceUpdate()
    {
        _lastValidCount = -1;
        _lastTotalCount = -1;
    }

    private void Awake()
    {
        // Vô hiệu hóa LayoutGroup có sẵn của Unity để script này tự xếp bài cong
        var layout = GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layout != null) layout.enabled = false;
        
        var csf = GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf != null) csf.enabled = false;
    }

    void LateUpdate()
    {
        int validCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).gameObject.activeSelf) validCount++;
        }

        if (validCount == 0) return;

        // CHỈ SẮP XẾP LẠI KHI SỐ LƯỢNG BÀI THAY ĐỔI
        // Để không ghi đè lên hiệu ứng Hover/Drag/Nảy của CardUI đang chạy trong Update
        if (validCount == _lastValidCount && transform.childCount == _lastTotalCount) return;
        
        _lastValidCount = validCount;
        _lastTotalCount = transform.childCount;

        float mid = (validCount - 1) / 2f;
        int activeIndex = 0;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            float dist = activeIndex - mid;
            
            float xPos = dist * spacing;
            float yPos = dist * dist * -yCurve;

            rect.anchoredPosition = new Vector2(xPos, yPos);
            rect.localRotation = Quaternion.Euler(0, 0, dist * -anglePerCard);

            activeIndex++;
        }
    }
}
