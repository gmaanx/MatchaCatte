namespace cattePrototype
{
    // dữ liệu của lá bài (rank, suit) được ẩn đi thông qua property chỉ có get,
    // giúp đối tượng trở nên bất biến (immutable) sau khi được khởi tạo.
    public class Card
    {
        public Rank Rank { get; }
        public Suit Suit { get; }

        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        // thay vì để bên ngoài truy cập thuộc tính rồi tự so sánh.
        public bool CanBeat(Card other)
        {
            if (other == null) return true;
            return this.Suit == other.Suit && this.Rank > other.Rank;
        }

        public override string ToString() => $"{Rank} {Suit}";

        public override bool Equals(object obj)
        {
            if (obj is Card other)
            {
                return this.Suit == other.Suit && this.Rank == other.Rank;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)Suit * 100 + (int)Rank;
        }
    }
}