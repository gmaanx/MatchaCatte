using System.Collections.Generic;

namespace cattePrototype.AI
{
    public static class BitboardUtility
    {
        public static int GetCardBitIndex(Card c)
        {
            return (int)c.Suit * 13 + (int)c.Rank;
        }

        public static ulong CardToBit(Card c)
        {
            return 1UL << GetCardBitIndex(c);
        }

        // Chuyển một tập hợp Card thành 1 ulong duy nhất
        public static ulong ToBitboard(IEnumerable<Card> cards)
        {
            ulong board = 0;
            foreach (var c in cards)
            {
                board |= CardToBit(c);
            }
            return board;
        }

        // Chuyển ngược lại từ ulong về List<Card>
        public static List<Card> BitboardToCards(ulong board)
        {
            List<Card> cards = new List<Card>();
            for (int i = 0; i < 52; i++)
            {
                if ((board & (1UL << i)) != 0)
                {
                    Suit s = (Suit)(i / 13);
                    Rank r = (Rank)(i % 13);
                    cards.Add(new Card(r, s));
                }
            }
            return cards;
        }

        // Lấy tất cả index của các bit 1
        public static List<int> GetBits(ulong board)
        {
            List<int> bits = new List<int>();
            for (int i = 0; i < 52; i++)
            {
                if ((board & (1UL << i)) != 0)
                {
                    bits.Add(i);
                }
            }
            return bits;
        }

        // Đếm số bit 1
        public static int Popcount(ulong board)
        {
            int count = 0;
            while (board != 0)
            {
                board &= board - 1; // Xóa bit 1 thấp nhất
                count++;
            }
            return count;
        }
    }
}
