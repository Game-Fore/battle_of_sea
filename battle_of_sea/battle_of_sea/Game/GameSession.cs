using System;

namespace battle_of_sea.Game
{
    public class GameSession
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public string CurrentTurnPlayerId { get; set; }
        public bool IsFinished { get; set; } = false;

        public GameSession(Player p1, Player p2)
        {
            Player1 = p1;
            Player2 = p2;
            CurrentTurnPlayerId = Player1.Id; // первый ход Player1
        }

        public Player GetCurrentPlayer() => Player1.Id == CurrentTurnPlayerId ? Player1 : Player2;
        public Player GetOpponentPlayer() => Player1.Id == CurrentTurnPlayerId ? Player2 : Player1;

        public void SwitchTurn()
        {
            CurrentTurnPlayerId = GetOpponentPlayer().Id;
        }
    }

}
