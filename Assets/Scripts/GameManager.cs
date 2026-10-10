using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cattePrototype
{
    // class trung tâm điều phối logic game. không hề dính líu đến code của Unity.
    public class GameManager
    {
        public const int TOTAL_ROUNDS = 6;
        public const int FILTER_ROUNDS = 4;
        public const int SHOW_ROUND = 5;
        public const int REVEAL_ROUND = 6;

        private readonly List<Player> _players;
        private readonly Deck _deck;
        private readonly IGameView _view;
        
        private readonly HashSet<Card> _playedCards;
        
        // dùng static để lưu trạng thái người thắng giữa các lần load scene.
        public static string LastWinner = "";

        // constructor áp dụng tiêm phụ thuộc (dependency injection):
        // view được truyền vào từ bên ngoài, giúp GameManager không phụ thuộc vào class UnityView cụ thể.
        public GameManager(IGameView view, List<Player> players)
        {
            _view = view;
            _players = players;
            _deck = new Deck();
            _playedCards = new HashSet<Card>();
        }

        public async Task StartGame()
        {
            _playedCards.Clear();
            await DealCards();
            _view.ShowMessage("=== GAME START ===");

            // Kiểm tra Tới Trắng (Instant Win)
            Player instantWinner = null;
            InstantWinResult bestWin = new InstantWinResult(InstantWinType.None, Rank.Two);

            foreach (var p in _players)
            {
                var result = InstantWinResult.EvaluateHand(p.Hand.ToList());
                if (result.WinType != InstantWinType.None)
                {
                    if (result.CompareTo(bestWin) > 0)
                    {
                        bestWin = result;
                        instantWinner = p;
                    }
                }
            }

            if (instantWinner != null)
            {
                LastWinner = instantWinner.Name;
                string reason = bestWin.GetWinReasonEnglish();
                _view.ShowGameOver($"INSTANT WIN: {instantWinner.Name} won with {reason}!");
                return;
            }

            int leaderIndex = 0;

            if (!string.IsNullOrEmpty(LastWinner))
            {
                var oldWinner = _players.Find(p => p.Name == LastWinner);
                if (oldWinner != null)
                {
                    leaderIndex = _players.IndexOf(oldWinner);
                }
            }

            for (int round = 1; round <= FILTER_ROUNDS; round++)
            {
                _view.UpdateRoundCount(round, TOTAL_ROUNDS);
                _view.ShowMessage($"\n--- ROUND {round} ---");
                leaderIndex = await PlayRound(leaderIndex, _players);
                _players[leaderIndex].HasTung = true;
                _view.ShowMessage($"=> Round {round} winner: {_players[leaderIndex].Name}");
            }

            var alivePlayers = _players.Where(p => p.HasTung).ToList();

            if (alivePlayers.Count <= 1)
            {
                LastWinner = alivePlayers[0].Name;
                string msg = (alivePlayers[0] is HumanPlayer) ? "YOU WIN" : "YOU LOSE";
                _view.ShowGameOver(msg);
                return;
            }

            _view.UpdateRoundCount(SHOW_ROUND, TOTAL_ROUNDS);
            _view.ShowMessage("\n--- ROUND 5 (LEAD) ---");
            int aliveLeaderIndex = alivePlayers.IndexOf(_players[leaderIndex]);
            aliveLeaderIndex = await PlayRound(aliveLeaderIndex, alivePlayers);

            _view.UpdateRoundCount(REVEAL_ROUND, TOTAL_ROUNDS);
            _view.ShowMessage("\n--- ROUND 6 (SHOWDOWN) ---");
            PlayFinalRound(aliveLeaderIndex, alivePlayers);
        }

        private async Task DealCards()
        {
            for (int i = 0; i < 6; i++)
            {
                foreach (var p in _players) p.AddCard(_deck.Draw());
            }

            for (int pIndex = 0; pIndex < _players.Count; pIndex++)
            {
                var p = _players[pIndex];
                p.SortHandByRank();
                _view.ShowHand(p, true, pIndex);
            }

            await Task.Delay(3600);
        }

        private async Task<int> PlayRound(int startIndex, List<Player> participants)
        {
            Card highestCard = null;
            int currentWinner = startIndex;

            _view.ClearTable();
            _view.HideTurnIndicator();

            foreach (var p in participants) p.IsFolded = false;

            for (int i = 0; i < participants.Count; i++)
            {
                int turnIndex = (startIndex + i) % participants.Count;
                Player currentPlayer = participants[turnIndex];

                _view.ShowMessage($"Turn: {currentPlayer.Name}");
                _view.ShowTurnIndicator(currentPlayer.Name);

                // Pass context _playedCards down to TakeTurn for better OOP design (No Static State)
                Card playedCard = await currentPlayer.TakeTurn(highestCard, _view, _playedCards);
                if (playedCard != null)
                {
                    _playedCards.Add(playedCard);
                }
                
                _view.ShowCardOnTable(currentPlayer.Name, playedCard, currentPlayer.IsFolded);
                _view.ShowHand(currentPlayer);
                
                await Task.Delay(700);

                if (!currentPlayer.IsFolded)
                {
                    highestCard = playedCard;
                    currentWinner = turnIndex;
                }
            }
            _view.HighlightWinningCard(participants[currentWinner].Name);
            await Task.Delay(1500);
            return currentWinner;
        }

        private async Task PlayFinalRound(int leaderIndex, List<Player> participants)
        {
            _view.ClearTable();

            Player leader = participants[leaderIndex];
            Card winningCard = leader.Hand[0];
            leader.RemoveCard(winningCard);
            
            Player ultimateWinner = leader;

            _view.ShowTurnIndicator(leader.Name);
            await Task.Delay(500);

            _playedCards.Add(winningCard);
            _view.ShowCardOnTable(leader.Name, winningCard, false);
            _view.ShowHand(leader);
            await Task.Delay(1000);

            for (int i = 1; i < participants.Count; i++)
            {
                int turnIndex = (leaderIndex + i) % participants.Count;
                Player p = participants[turnIndex];

                _view.ShowTurnIndicator(p.Name);
                await Task.Delay(500);

                Card revealedCard = p.Hand[0];
                p.RemoveCard(revealedCard);
                
                _playedCards.Add(revealedCard);
                _view.ShowCardOnTable(p.Name, revealedCard, false);
                _view.ShowHand(p);

                if (revealedCard.CanBeat(winningCard))
                {
                    winningCard = revealedCard;
                    ultimateWinner = p;
                }

                await Task.Delay(1000);
            }

            _view.HighlightWinningCard(ultimateWinner.Name);
            await Task.Delay(1500);

            _view.HideTurnIndicator();

            LastWinner = ultimateWinner.Name;
            string finalMsg = (ultimateWinner is HumanPlayer) ? "YOU WIN" : "YOU LOSE";
            _view.ShowGameOver(finalMsg);
        }
        
        public List<Card> GetRemainingCards(Player forPlayer)
        {
            List<Card> remaining = new List<Card>();
            foreach (Suit s in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank r in System.Enum.GetValues(typeof(Rank)))
                {
                    Card c = new Card(r, s);
                    bool isPlayed = false;
                    foreach(var pc in _playedCards) { if (pc.Suit == c.Suit && pc.Rank == c.Rank) { isPlayed = true; break; } }
                    
                    bool inHand = false;
                    foreach(var hc in forPlayer.Hand) { if (hc.Suit == c.Suit && hc.Rank == c.Rank) { inHand = true; break; } }

                    if (!isPlayed && !inHand)
                    {
                        remaining.Add(c);
                    }
                }
            }
            return remaining;
        }
    }
}
