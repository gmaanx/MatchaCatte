using System.Collections.Generic;
using System.Linq;

namespace cattePrototype
{
    public class BotVua : BotAI
    {
        public BotVua(string name) : base(name) { }

        protected override Card PlayLogic(Card currentHighest, IEnumerable<Card> playedCards)
        {
            if (currentHighest == null)
            {
                // Dẫn bài: Heuristic + Card Counting
                // Tìm xem có lá bài nào trên tay đang là "trùm" (những lá to hơn nó cùng chất đều đã đánh ra)
                var bossCards = Hand.Where(c => IsBossCard(c, playedCards)).OrderByDescending(c => c.Rank).ToList();
                
                if (bossCards.Any())
                {
                    // Đánh con trùm to nhất
                    return bossCards.First();
                }

                // Không có con trùm nào, đánh con to nhất để cố gắng giành quyền
                return Hand.OrderByDescending(c => c.Rank).First();
            }

            // Theo bài: Khôn lỏi hơn Bot Dễ một chút.
            // Chọn con NHỎ NHẤT đủ sức ăn (tiết kiệm bài lớn).
            return Hand.Where(c => c.CanBeat(currentHighest))
                       .OrderBy(c => c.Rank)
                       .FirstOrDefault();
        }

        private bool IsBossCard(Card c, IEnumerable<Card> playedCards)
        {
            // Kiểm tra xem tất cả các lá bài lớn hơn lá c cùng chất đã được đánh ra chưa
            foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
            {
                if (r > c.Rank)
                {
                    Card higherCard = new Card(r, c.Suit);
                    if (!playedCards.Contains(higherCard))
                    {
                        // Vẫn còn lá to hơn chưa lộ diện, nên lá c chưa phải trùm
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
