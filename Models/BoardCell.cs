using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media;

namespace BattleOfSea.Models
{
    public class BoardCell : INotifyPropertyChanged
    {
        public int Row { get; }
        public int Col { get; }

        public bool HasShip { get; set; }

        private bool _isRevealed;
        public bool IsRevealed
        {
            get => _isRevealed;
            set { _isRevealed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellBackground)); OnPropertyChanged(nameof(CellBorderBrush)); 
                  OnPropertyChanged(nameof(CellBorderThickness)); }
        }

        private bool _isHit;
        public bool IsHit
        {
            get => _isHit;
            set { _isHit = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellBackground)); OnPropertyChanged(nameof(CellBorderBrush)); }
        }

        private bool _isHovered;
        public bool IsHovered
        {
            get => _isHovered;
            set { _isHovered = value; OnPropertyChanged(); }
        }

        private bool _isSunk;
        public bool IsSunk
        {
            get => _isSunk;
            set { _isSunk = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); 
                  OnPropertyChanged(nameof(CellState)); OnPropertyChanged(nameof(CellBackground)); 
                  OnPropertyChanged(nameof(CellBorderBrush)); OnPropertyChanged(nameof(CellBorderThickness)); }
        }

        public string Display => IsSunk ? "💥" : (IsRevealed ? (IsHit ? "✕" : "·") : string.Empty);

        public string CellState => IsSunk ? "Sunk" : (IsRevealed ? (IsHit ? "Hit" : "Miss") : (HasShip ? "Ship" : "Empty"));

        public IBrush CellBackground
        {
            get
            {
                if (IsSunk) return new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red
                if (IsHit) return new SolidColorBrush(Color.FromRgb(252, 165, 165)); // Light Red
                if (IsRevealed) return new SolidColorBrush(Color.FromRgb(229, 231, 235)); // Gray
                if (HasShip) return new SolidColorBrush(Color.FromRgb(147, 197, 253)); // Light Blue
                return new SolidColorBrush(Color.FromRgb(219, 234, 254)); // Very Light Blue
            }
        }

        public IBrush CellBorderBrush
        {
            get
            {
                if (IsSunk) return new SolidColorBrush(Color.FromRgb(153, 27, 27)); // Dark Red
                if (IsHit) return new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red
                if (IsRevealed) return new SolidColorBrush(Color.FromRgb(156, 163, 175)); // Gray
                return new SolidColorBrush(Color.FromRgb(147, 197, 253)); // Light Blue
            }
        }

        public int CellBorderThickness => IsSunk ? 2 : 1;

        public BoardCell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
