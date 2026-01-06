using System;

namespace battle_of_sea.Game
{
    public class GameSession
    {
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public string CurrentTurnPlayerID {  get; set; }
        public bool IsFinished { get; set; } = false;

        public GameSession(Player p1, Player p2)
        {
            Player1 = p1; Player2 = p2;
            CurrentTurnPlayerID = Player1.Id;
        }
    }
}
