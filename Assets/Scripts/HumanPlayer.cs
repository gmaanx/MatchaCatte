using System.Collections.Generic;
using System.Threading.Tasks;

namespace cattePrototype
{
    public class HumanPlayer : Player
    {
        public HumanPlayer(string name) : base(name) { }

        public override async Task<Card> TakeTurn(Card currentHighest, IGameView view, IEnumerable<Card> playedCards)
        {
            view.ShowHand(this);

            while (true)
            {
                int choice = await view.GetPlayerInput(this, currentHighest);

                if (choice >= 0 && choice < Hand.Count)
                {
                    Card chosenCard = Hand[choice];
                    RemoveCard(chosenCard); // Rút lá bài khỏi tay bằng protected method

                    // Nếu bài lớn hơn -> Bắt bài (Tồn/Ngửa)
                    if (currentHighest == null || chosenCard.CanBeat(currentHighest))
                    {
                        IsFolded = false;
                    }
                    // Nếu bài nhỏ hơn hoặc khác chất -> Thiệp bài (Úp)
                    else
                    {
                        IsFolded = true;
                    }

                    return chosenCard;
                }
            }
        }
    }
}
