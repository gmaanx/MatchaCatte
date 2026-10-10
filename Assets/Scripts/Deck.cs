using System;
using System.Collections.Generic;
using System.Linq;

namespace cattePrototype
{
    public class Deck
    {
        private List<Card> _cards;
        private readonly Random _rng = new Random();

        public Deck()
        {
            _cards = new List<Card>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    _cards.Add(new Card(rank, suit));
                }
            }
            Shuffle();
        }

        // cung cấp method public để tương tác thay vì cho phép thao tác trực tiếp list
        public void Shuffle()
        {
            _cards = _cards.OrderBy(x => _rng.Next()).ToList();
        }

        public Card Draw()
        {
            if (_cards.Count == 0) return null;
            Card drawn = _cards[0];
            _cards.RemoveAt(0);
            return drawn;
        }
    }
}