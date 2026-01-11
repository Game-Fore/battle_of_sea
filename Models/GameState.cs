// Состояние игры
namespace BattleOfSea.Models
{
    public enum GameState
    {
        WaitingForOpponent,
        YourTurn,
        OpponentTurn,
        YouWin,
        YouLose,
        OpponentSurrender,
        Hit,
        Miss,
        Sunk
    }
}
