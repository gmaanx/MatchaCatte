using System.Collections.Generic;
using System.Linq;

namespace cattePrototype
{
    public class BotDe : BotAI
    {
        public BotDe(string name) : base(name) { }

        // Bot Dễ: Lối chơi ngây ngô, luôn chọn lá bài nhỏ nhất có thể đánh được.
        protected override Card PlayLogic(Card currentHighest, IEnumerable<Card> playedCards)
        {
            if (currentHighest == null)
            {
                // Nếu mình là người đi đầu, đánh lá nhỏ nhất
                return Hand.OrderBy(c => c.Rank).First();
            }
            else
            {
                // Đánh lá bài hợp lệ nhỏ nhất
                var validCards = Hand.Where(c => c.CanBeat(currentHighest)).OrderBy(c => c.Rank);
                return validCards.FirstOrDefault(); // Nếu không có trả về null, báo hiệu sẽ Fold
            }
        }
    }
}