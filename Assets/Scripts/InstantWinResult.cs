using System;
using System.Collections.Generic;
using System.Linq;

namespace cattePrototype
{
    public enum InstantWinType
    {
        None = 0,
        SixCardsUnderSix = 1,
        SixCardsSameSuit = 2,
        FourOfAKind = 3
    }

    public class InstantWinResult : IComparable<InstantWinResult>
    {
        public InstantWinType WinType { get; private set; }
        public Rank HighestRank { get; private set; }

        public InstantWinResult(InstantWinType type, Rank highestRank)
        {
            WinType = type;
            HighestRank = highestRank;
        }

        public int CompareTo(InstantWinResult other)
        {
            if (other == null) return 1;
            
            // So sánh loại Tới Trắng trước
            int typeCompare = this.WinType.CompareTo(other.WinType);
            if (typeCompare != 0)
                return typeCompare;
                
            // Nếu cùng loại, so sánh con bài cao nhất
            return this.HighestRank.CompareTo(other.HighestRank);
        }

        public string GetWinReasonEnglish()
        {
            switch (WinType)
            {
                case InstantWinType.FourOfAKind:
                    return $"FOUR OF A KIND ({HighestRank})";
                case InstantWinType.SixCardsSameSuit:
                    return "6 CARDS SAME SUIT";
                case InstantWinType.SixCardsUnderSix:
                    return "6 CARDS UNDER 6";
                default:
                    return "";
            }
        }

        // Hàm tĩnh để đánh giá bài của một người chơi
        public static InstantWinResult EvaluateHand(List<Card> hand)
        {
            if (hand == null || hand.Count != 6) 
                return new InstantWinResult(InstantWinType.None, Rank.Two);

            // 1. Kiểm tra Tứ Quý
            var rankCounts = hand.GroupBy(c => c.Rank).ToDictionary(g => g.Key, g => g.Count());
            var fourOfAKind = rankCounts.FirstOrDefault(kvp => kvp.Value >= 4);
            if (fourOfAKind.Value >= 4)
            {
                return new InstantWinResult(InstantWinType.FourOfAKind, fourOfAKind.Key);
            }

            // Lấy con bài lớn nhất trong 6 lá để làm Tie-breaker cho 2 trường hợp dưới
            Rank maxRankInHand = hand.Max(c => c.Rank);

            // 2. Kiểm tra 6 lá cùng chất
            var suitCounts = hand.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.Count());
            var sixSameSuit = suitCounts.FirstOrDefault(kvp => kvp.Value == 6);
            if (sixSameSuit.Value == 6)
            {
                return new InstantWinResult(InstantWinType.SixCardsSameSuit, maxRankInHand);
            }

            // 3. Kiểm tra 6 lá nhỏ hơn 6
            // Rank.Six có giá trị int là 4 (Two=0, Three=1, Four=2, Five=3)
            if (hand.All(c => c.Rank < Rank.Six))
            {
                return new InstantWinResult(InstantWinType.SixCardsUnderSix, maxRankInHand);
            }

            return new InstantWinResult(InstantWinType.None, Rank.Two);
        }
    }
}
