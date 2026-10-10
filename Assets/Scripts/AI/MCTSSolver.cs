using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cattePrototype.AI
{
    public class MCTSSolver
    {
        private static Random _rng = new Random();

        public static async Task<Card> FindBestMoveAsync(GameState initialState, int maxSimulations)
        {
            return await Task.Run(() =>
            {
                MCTSNode root = new MCTSNode(initialState, null, -1);

                for (int i = 0; i < maxSimulations; i++)
                {
                    // 1. Tạo ngẫu nhiên bài cho đối thủ (Determinization)
                    ulong[] opponentHands = DistributeUnknownCards(initialState.UnknownCards, initialState.NumPlayers - 1, BitboardUtility.Popcount(initialState.MyHand));

                    // 2. Selection
                    MCTSNode node = root;
                    GameState tempState = initialState;
                    
                    // Trong khi node không có nước chưa thử và có con
                    while (node.UntriedMoves.Count == 0 && node.Children.Count > 0)
                    {
                        node = node.GetBestChildUCT();
                        // Tính toán lại trạng thái khi đi nước này
                        tempState = SimulateMove(tempState, node.MovePlayed, opponentHands);
                    }

                    // 3. Expansion
                    if (node.UntriedMoves.Count > 0)
                    {
                        int move = node.UntriedMoves[_rng.Next(node.UntriedMoves.Count)];
                        tempState = SimulateMove(tempState, move, opponentHands);
                        node = node.AddChild(move, tempState);
                    }

                    // 4. Simulation (Rollout)
                    float result = Rollout(tempState, opponentHands);

                    // 5. Backpropagation
                    while (node != null)
                    {
                        node.Visits++;
                        node.WinScore += result;
                        node = node.Parent;
                    }
                }

                if (root.Children.Count == 0)
                {
                    // Fallback
                    return null;
                }

                // Chọn nước đi có tỉ lệ thắng cao nhất (nhiều lượt visit nhất)
                MCTSNode bestMoveNode = root.Children.OrderByDescending(c => c.Visits).First();
                
                int bitIndex = bestMoveNode.MovePlayed;
                Suit s = (Suit)(bitIndex / 13);
                Rank r = (Rank)(bitIndex % 13);
                
                return new Card(r, s);
            });
        }

        private static ulong[] DistributeUnknownCards(ulong unknownCards, int numOpponents, int cardsPerOpponent)
        {
            List<int> availableBits = BitboardUtility.GetBits(unknownCards);
            availableBits = availableBits.OrderBy(x => _rng.Next()).ToList();
            
            ulong[] oppHands = new ulong[numOpponents];
            int index = 0;

            for (int i = 0; i < numOpponents; i++)
            {
                ulong hand = 0;
                for (int j = 0; j < cardsPerOpponent; j++)
                {
                    if (index < availableBits.Count)
                    {
                        hand |= (1UL << availableBits[index]);
                        index++;
                    }
                }
                oppHands[i] = hand;
            }

            return oppHands;
        }

        private static GameState SimulateMove(GameState state, int myMove, ulong[] opponentHands)
        {
            // Cập nhật trạng thái sau khi mình đánh 1 lá
            ulong newHand = state.MyHand & ~(1UL << myMove);
            ulong newPlayed = state.PlayedCards | (1UL << myMove);
            
            ulong newHighest = state.CurrentHighest;
            if (state.CurrentHighest == 0) 
            {
                newHighest = 1UL << myMove;
            }
            else 
            {
                int currentHighBit = BitboardUtility.GetBits(state.CurrentHighest).FirstOrDefault();
                if (myMove / 13 == currentHighBit / 13 && myMove % 13 > currentHighBit % 13)
                {
                    newHighest = 1UL << myMove;
                }
            }

            // (Trong thực tế GameState cần phức tạp hơn để theo dõi lượt của đối thủ, 
            // nhưng để Rollout nhanh, ta giả lập đối thủ đánh ngay lập tức sau mình)
            
            return new GameState(newHand, newPlayed, state.UnknownCards, newHighest, state.NumPlayers);
        }

        private static float Rollout(GameState state, ulong[] opponentHands)
        {
            // Giả lập nhanh bằng random
            ulong myHand = state.MyHand;
            ulong currentHighest = state.CurrentHighest;
            
            // Loop đến khi hết bài
            while (myHand != 0)
            {
                // Đối thủ ngẫu nhiên quăng bài
                for (int i = 0; i < opponentHands.Length; i++)
                {
                    if (opponentHands[i] != 0)
                    {
                        List<int> validOppMoves = GetValidMovesRandom(opponentHands[i], currentHighest);
                        if (validOppMoves.Count > 0)
                        {
                            int oppMove = validOppMoves[_rng.Next(validOppMoves.Count)];
                            opponentHands[i] &= ~(1UL << oppMove);
                            
                            if (currentHighest == 0) currentHighest = (1UL << oppMove);
                            else
                            {
                                int currentHighBit = BitboardUtility.GetBits(currentHighest).FirstOrDefault();
                                if (oppMove / 13 == currentHighBit / 13 && oppMove % 13 > currentHighBit % 13)
                                {
                                    currentHighest = (1UL << oppMove);
                                }
                            }
                        }
                    }
                }

                // Tới mình quăng bài
                List<int> validMyMoves = GetValidMovesRandom(myHand, currentHighest);
                if (validMyMoves.Count > 0)
                {
                    int myMove = validMyMoves[_rng.Next(validMyMoves.Count)];
                    myHand &= ~(1UL << myMove);

                    if (currentHighest == 0) currentHighest = (1UL << myMove);
                    else
                    {
                        int currentHighBit = BitboardUtility.GetBits(currentHighest).FirstOrDefault();
                        if (myMove / 13 == currentHighBit / 13 && myMove % 13 > currentHighBit % 13)
                        {
                            currentHighest = (1UL << myMove);
                        }
                    }
                }
                
                // End round, ai cầm bài to nhất được dẫn round sau
                currentHighest = 0; 
            }

            // Vì đây là giả lập siêu đơn giản, hàm Heuristic để chấm điểm (WinScore) 
            // được xác định bằng việc mình còn sống hay không.
            // Để đơn giản ta có thể return 0.5f hoặc random win/loss.
            // Bản đầy đủ của Rollout sẽ chạy qua các vòng (6 rounds) và return 1 (Win), 0 (Loss).
            return _rng.Next(0, 2); 
        }
        
        private static List<int> GetValidMovesRandom(ulong hand, ulong currentHighest)
        {
            List<int> validMoves = new List<int>();
            if (currentHighest == 0) return BitboardUtility.GetBits(hand);

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

            if (!canFollow) return BitboardUtility.GetBits(hand);
            return validMoves;
        }
    }
}
