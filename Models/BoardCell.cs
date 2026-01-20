// Ячейка поля
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace BattleOfSea.Models
{
    // Класс ячейки игрового поля
    public class BoardCell : INotifyPropertyChanged
    {
        // Строка ячейки
        public int Row { get; }
        // Столбец ячейки
        public int Col { get; }

        // Наличие корабля
        public bool HasShip { get; set; }

        private bool _isRevealed;
        // Открыта ли ячейка
        public bool IsRevealed
        {
            get => _isRevealed;
            set { _isRevealed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellBackground)); OnPropertyChanged(nameof(CellBorderBrush)); 
                  OnPropertyChanged(nameof(CellBorderThickness)); }
        }

        private bool _isHit;
        // Попадание в ячейку
        public bool IsHit
        {
            get => _isHit;
            set { _isHit = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellBackground)); OnPropertyChanged(nameof(CellBorderBrush)); }
        }

        private bool _isHovered;
        // Наведение курсора
        public bool IsHovered
        {
            get => _isHovered;
            set { _isHovered = value; OnPropertyChanged(); }
        }

        private bool _isSunk;
        // Потоплен ли корабль
        public bool IsSunk
        {
            get => _isSunk;
            set { _isSunk = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellState)); OnPropertyChanged(nameof(CellBackground)); 
                  OnPropertyChanged(nameof(CellBorderBrush)); OnPropertyChanged(nameof(CellBorderThickness)); }
        }

        // Отображаемый символ
        public string Display => IsSunk ? "💥" : (IsRevealed ? (IsHit ? "✕" : "·") : string.Empty);

        // Состояние ячейки
        public string CellState => IsSunk ? "Sunk" : (IsRevealed ? (IsHit ? "Hit" : "Miss") : (HasShip ? "Ship" : "Empty"));

        // Цвет фона ячейки
        public IBrush CellBackground
        {
            get
            {
                if (IsSunk) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Красный
                if (IsHit) return new SolidColorBrush(Color.FromRgb(252, 165, 165)); // Светло-красный
                if (IsRevealed) return new SolidColorBrush(Color.FromRgb(229, 231, 235)); // Серый
                if (HasShip) return new SolidColorBrush(Color.FromRgb(147, 197, 253)); // Светло-синий
                return new SolidColorBrush(Color.FromRgb(219, 234, 254)); // Очень светло-синий
            }
        }

        // Цвет границы ячейки
        public IBrush CellBorderBrush
        {
            get
            {
                if (IsSunk) return new SolidColorBrush(Color.FromRgb(153, 27, 27)); // Темно-красный
                if (IsHit) return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Красный
                if (IsRevealed) return new SolidColorBrush(Color.FromRgb(156, 163, 175)); // Серый
                return new SolidColorBrush(Color.FromRgb(147, 197, 253)); // Светло-синий
            }
        }

        // Толщина границы ячейки
        public int CellBorderThickness => IsSunk ? 2 : 1;

        // Конструктор ячейки
        public BoardCell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        // Событие изменения свойства
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}