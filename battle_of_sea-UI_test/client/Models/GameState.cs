// Состояние игры
namespace BattleOfSea.Models
{
    // Перечисление состояний игры
    public enum GameState
    {
        // Ожидание противника
        WaitingForOpponent,
        // Ваш ход
        YourTurn,
        // Ход противника
        OpponentTurn,
        // Вы победили
        YouWin,
        // Вы проиграли
        YouLose,
        // Противник сдался
        OpponentSurrender,
        // Попадание
        Hit,
        // Промах
        Miss,
        // Корабль потоплен
        Sunk
    }
}