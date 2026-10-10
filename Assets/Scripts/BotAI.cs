using System.Collections.Generic;
using System.Threading.Tasks;

namespace cattePrototype
{
    public abstract class BotAI : Player
    {
        protected BotAI(string name) : base(name) { }

        public override async Task<Card> TakeTurn(Card currentHighest, IGameView view, IEnumerable<Card> playedCards)
        {
            await Task.Delay(1000); // Giả lập thời gian bot suy nghĩ

            Card chosen = PlayLogic(currentHighest, playedCards);

            if (chosen == null || (currentHighest != null && !chosen.CanBeat(currentHighest)))
            {
                IsFolded = true;
                // Bot phải úp 1 lá bài bừa (nhỏ nhất)
                chosen = Hand[0];
            }
            
            RemoveCard(chosen);

            view.ShowMessage($"{Name} plays: {(IsFolded ? "folds" : chosen.ToString())}");
            return chosen;
        }

        protected abstract Card PlayLogic(Card currentHighest, IEnumerable<Card> playedCards);
    }
}