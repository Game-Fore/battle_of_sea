// Представление поля
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BattleOfSea.ViewModels
{
    // Модель представления игрового поля
    public class BoardViewModel : INotifyPropertyChanged
    {
        // Собственное поле игрока (публичное свойство)
        public Models.Board Own { get; } = new Models.Board(10);
        // Поле противника (публичное свойство)
        public Models.Board Enemy { get; } = new Models.Board(10);

        // Конструктор модели представления (публичный)
        public BoardViewModel()
        {
            // Пример размещения кораблей (для демонстрации)
            Own.PlaceShip(0, 0);
            Own.PlaceShip(0, 1);
            Own.PlaceShip(2, 3);
        }

        // Обработка клика по ячейке противника (публичный метод)
        public void EnemyCellClick(Models.BoardCell cell)
        {
            Console.WriteLine($"Enemy cell clicked: {cell.Row},{cell.Col}");
            // Демо: открыть ячейку и случайно определить попадание/промах
            cell.IsRevealed = true;
            cell.IsHit = new Random().Next(0, 4) == 0; // ~25% шанс попадания как демо
            Console.WriteLine(cell.IsHit ? "Попал!" : "Промах");
        }

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}