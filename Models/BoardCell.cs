using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            set { _isRevealed = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); }
        }

        private bool _isHit;
        public bool IsHit
        {
            get => _isHit;
            set { _isHit = value; OnPropertyChanged(); OnPropertyChanged(nameof(Display)); }
        }

        private bool _isHovered;
        public bool IsHovered
        {
            get => _isHovered;
            set { _isHovered = value; OnPropertyChanged(); }
        }

        public string Display => HasShip && IsRevealed ? "⛵" : (IsRevealed ? (IsHit ? "X" : "·") : string.Empty);

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
