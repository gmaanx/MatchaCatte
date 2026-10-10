using System.Threading.Tasks;

namespace cattePrototype
{
    // interface này đóng vai trò như một bản hợp đồng.
    // logic game (GameManager) chỉ gọi hàm qua interface này mà không cần biết
    // hệ thống nào (Unity, Console, v.v.) đang thực thi ở dưới (dependency inversion).
    public interface IGameView
    {
        void ShowMessage(string message);
        void UpdateRoundCount(int currentRound, int totalRounds);
        void ShowHand(Player player); // dành cho lúc đánh bài (update lướt)
        void ShowHand(Player player, bool playAnimation, int playerIndex); // dành cho lúc chia bài đầu game
        Task<int> GetPlayerInput(Player player, Card currentHighest);
        Task<int> GetDifficultyChoice();

        void ClearTable();
        void ShowCardOnTable(string playerName, Card card, bool isFolded);
        void ShowGameOver(string finalMessage);
        void ShowTurnIndicator(string playerName);
        void HideTurnIndicator();
        void UpdatePlayability(Card currentHighest);
        void HighlightWinningCard(string playerName);
    }
}