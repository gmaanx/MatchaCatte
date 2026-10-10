using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cattePrototype.AI;

namespace cattePrototype
{
    // Bot Khó: Áp dụng thuật toán Monte Carlo Tree Search (MCTS) kết hợp Bitboard
    public class BotKho : Player
    {
        public BotKho(string name) : base(name) { }

        public override async Task<Card> TakeTurn(Card currentHighest, IGameView view, IEnumerable<Card> playedCards)
        {
            // Build GameState for MCTS
            ulong myHand = BitboardUtility.ToBitboard(Hand.ToList());
            ulong playedCardsBit = BitboardUtility.ToBitboard(playedCards.ToList());
            ulong unknownCards = 0;
            
            // Tính toán những lá chưa biết
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
                {
                    Card c = new Card(r, s);
                    bool inHand = Hand.Any(hc => hc.Suit == c.Suit && hc.Rank == c.Rank);
                    bool isPlayed = playedCards.Any(pc => pc.Suit == c.Suit && pc.Rank == c.Rank);
                    if (!inHand && !isPlayed)
                    {
                        unknownCards |= BitboardUtility.CardToBit(c);
                    }
                }
            }

            ulong currentHighestBit = currentHighest != null ? BitboardUtility.CardToBit(currentHighest) : 0;
            GameState state = new GameState(myHand, playedCardsBit, unknownCards, currentHighestBit, 6);

            // Chạy MCTS trên luồng phụ
            Card chosen = await MCTSSolver.FindBestMoveAsync(state, 5000); // 5000 simulations

            if (chosen == null || (currentHighest != null && !chosen.CanBeat(currentHighest)))
            {
                IsFolded = true;
                if (chosen == null) 
                {
                    chosen = Hand.OrderBy(c => c.Rank).First(); // Úp lá nhỏ nhất nếu MCTS không trả về gì
                }
                RemoveCard(chosen);
            }
            else
            {
                RemoveCard(chosen);
            }
            view.ShowMessage(string.Format("{0} plays: {1}", Name, (IsFolded ? "folds" : chosen.ToString())));

            return chosen;
        }
    }
}
