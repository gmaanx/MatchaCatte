using System.Collections.Generic;
using System.Threading.Tasks;

namespace cattePrototype
{
    // không thể khởi tạo trực tiếp (abstract class).
    public abstract class Player
    {
        public string Name { get; }
        
        public IReadOnlyList<Card> Hand => _hand;
        private readonly List<Card> _hand;
        
        // trạng thái game (cần được GameManager can thiệp)
        public bool HasTung { get; set; }
        public bool IsFolded { get; set; }

        protected Player(string name)
        {
            Name = name;
            _hand = new List<Card>();
            HasTung = false;
            IsFolded = false;
        }

        public void AddCard(Card card)
        {
            if (card != null)
                _hand.Add(card);
        }

        public void RemoveCard(Card card)
        {
            if (card != null && _hand.Contains(card))
            {
                _hand.Remove(card);
            }
        }

        public void SortHandByRank()
        {
            _hand.Sort((a, b) => a.Rank.CompareTo(b.Rank));
        }

        public void SortHandBySuit()
        {
            _hand.Sort((a, b) =>
            {
                int suitCompare = a.Suit.CompareTo(b.Suit);
                if (suitCompare != 0) return suitCompare;
                return a.Rank.CompareTo(b.Rank);
            });
        }

        // Truyền context playedCards vào thay vì dùng biến tĩnh (static) của GameManager.
        public abstract Task<Card> TakeTurn(Card currentHighest, IGameView view, IEnumerable<Card> playedCards);
    }
}
