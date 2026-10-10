using System;
using System.Collections.Generic;
using System.Linq;

namespace cattePrototype.AI
{
    public struct GameState
    {
        public ulong MyHand;
        public ulong PlayedCards;
        public ulong UnknownCards;
        public ulong CurrentHighest;
        public int NumPlayers;

        public GameState(ulong myHand, ulong playedCards, ulong unknownCards, ulong currentHighest, int numPlayers)
        {
            MyHand = myHand;
            PlayedCards = playedCards;
            UnknownCards = unknownCards;
            CurrentHighest = currentHighest;
            NumPlayers = numPlayers;
        }
    }

    public class MCTSNode
    {
        public GameState State { get; private set; }
        public MCTSNode Parent { get; private set; }
        public int MovePlayed { get; private set; } // Bit index của lá bài được đánh để tới trạng thái này
        public List<MCTSNode> Children { get; private set; }
        
        public int Visits { get; set; }
        public float WinScore { get; set; }
        
        public List<int> UntriedMoves { get; private set; } // Các nước đi hợp lệ chưa được thử

        public MCTSNode(GameState state, MCTSNode parent, int movePlayed)
        {
            State = state;
            Parent = parent;
            MovePlayed = movePlayed;
            Children = new List<MCTSNode>();
            Visits = 0;
            WinScore = 0;
            
            // Tìm các nước đi hợp lệ từ State.MyHand
            UntriedMoves = GetValidMoves(state.MyHand, state.CurrentHighest);
        }

        private List<int> GetValidMoves(ulong hand, ulong currentHighest)
        {
            List<int> validMoves = new List<int>();
            
            if (currentHighest == 0)
            {
                // Dẫn bài: có thể đánh bất kỳ lá nào
                return BitboardUtility.GetBits(hand);
            }

            // Theo bài: Phải đánh lá to hơn cùng chất, nếu không có thì thiệp (úp bài)
            int highestIndex = BitboardUtility.GetBits(currentHighest).FirstOrDefault();
            int highestSuit = highestIndex / 13;
            int highestRank = highestIndex % 13;

            bool canFollow = false;
            foreach (int cardIndex in BitboardUtility.GetBits(hand))
            {
                int suit = cardIndex / 13;
                int rank = cardIndex % 13;

                if (suit == highestSuit && rank > highestRank)
                {
                    validMoves.Add(cardIndex);
                    canFollow = true;
                }
            }

            if (!canFollow)
            {
                // Thiệp bài: Có thể chọn bất kỳ lá nào để úp
                return BitboardUtility.GetBits(hand);
            }

            return validMoves;
        }

        public MCTSNode AddChild(int move, GameState nextState)
        {
            MCTSNode childNode = new MCTSNode(nextState, this, move);
            UntriedMoves.Remove(move);
            Children.Add(childNode);
            return childNode;
        }

        public MCTSNode GetBestChildUCT(float explorationParam = 1.41f)
        {
            MCTSNode bestChild = null;
            float bestValue = float.MinValue;

            foreach (var child in Children)
            {
                float uctValue = (child.WinScore / child.Visits) + 
                                 explorationParam * (float)Math.Sqrt(Math.Log(this.Visits) / child.Visits);

                if (uctValue > bestValue)
                {
                    bestValue = uctValue;
                    bestChild = child;
                }
            }

            return bestChild;
        }
    }
}
