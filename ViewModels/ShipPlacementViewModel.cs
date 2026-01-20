// Логика расстановки
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Linq;
using BattleOfSea.Models;

namespace BattleOfSea.ViewModels
{
    // Модель представления расстановки кораблей
    public class ShipPlacementViewModel : INotifyPropertyChanged
    {
        private Models.Board _board;
        private ShipPlacementManager _shipManager;
        private int? _selectedShipSize;
        private bool _isHorizontal = true;

        // Игровое поле (публичное свойство)
        public Models.Board Board
        {
            get => _board;
            set { _board = value; OnPropertyChanged(); }
        }

        // Менеджер размещения кораблей (публичное свойство)
        public ShipPlacementManager ShipManager
        {
            get => _shipManager;
            set { _shipManager = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShipStatusText)); }
        }

        // Выбранный размер корабля (публичное свойство)
        public int? SelectedShipSize
        {
            get => _selectedShipSize;
            set { _selectedShipSize = value; OnPropertyChanged(); }
        }

        // Горизонтальная ориентация корабля (публичное свойство)
        public bool IsHorizontal
        {
            get => _isHorizontal;
            set { _isHorizontal = value; OnPropertyChanged(); }
        }

        // Текст статуса кораблей (публичное свойство)
        public string ShipStatusText
        {
            get
            {
                if (_shipManager == null) return "";
                var text = $"Корабли: {_shipManager.PlacedShips}/{_shipManager.TotalShips}\n";
                foreach (var ship in _shipManager.Ships)
                {
                    text += $"{ship.Size}-палубных: {ship.Placed}/{ship.Count}\n";
                }
                return text.TrimEnd();
            }
        }

        // Можно ли начать игру (публичное свойство)
        public bool CanStartGame => _shipManager?.AllShipsPlaced ?? false;

        // Команды управления расстановкой (публичные свойства)
        public ICommand PlaceShipCommand { get; }
        public ICommand RemoveShipCommand { get; }
        public ICommand RotateShipCommand { get; }
        public ICommand ClearBoardCommand { get; }

        // Конструктор модели представления (публичный)
        public ShipPlacementViewModel(Models.Board board)
        {
            _board = board;
            _shipManager = new ShipPlacementManager();
            
            // Инициализация команды размещения корабля
            PlaceShipCommand = new Utils.RelayCommand(o =>
            {
                if (o is BoardCell cell && SelectedShipSize.HasValue)
                {
                    PlaceShip(cell.Row, cell.Col, SelectedShipSize.Value);
                }
            });

            // Инициализация команды удаления корабля
            RemoveShipCommand = new Utils.RelayCommand(o =>
            {
                if (o is BoardCell cell && cell.HasShip)
                {
                    RemoveShip(cell.Row, cell.Col);
                }
            });

            // Инициализация команды поворота корабля
            RotateShipCommand = new Utils.RelayCommand(_ =>
            {
                IsHorizontal = !IsHorizontal;
            });

            // Инициализация команды очистки поля
            ClearBoardCommand = new Utils.RelayCommand(_ =>
            {
                ClearBoard();
            });
        }

        // Разместить корабль (приватный метод)
        private void PlaceShip(int r, int c, int size)
        {
            if (!_shipManager.CanPlaceShip(size)) return;

            if (_board.PlaceShip(r, c, size, IsHorizontal))
            {
                _shipManager.PlaceShip(size);
                OnPropertyChanged(nameof(ShipStatusText));
                OnPropertyChanged(nameof(CanStartGame));
            }
        }

        // Удалить корабль (приватный метод)
        private void RemoveShip(int r, int c)
        {
            var ships = _board.GetShips();
            foreach (var ship in ships)
            {
                var cell = ship.FirstOrDefault(ce => ce.Row == r && ce.Col == c);
                if (cell != null)
                {
                    _board.RemoveShip(r, c);
                    _shipManager.RemoveShip(ship.Count);
                    OnPropertyChanged(nameof(ShipStatusText));
                    OnPropertyChanged(nameof(CanStartGame));
                    break;
                }
            }
        }

        // Очистить поле (приватный метод)
        private void ClearBoard()
        {
            foreach (var cell in _board.Cells)
            {
                cell.HasShip = false;
            }
            _shipManager.Reset();
            OnPropertyChanged(nameof(ShipStatusText));
            OnPropertyChanged(nameof(CanStartGame));
        }

        // Событие изменения свойства (публичное событие)
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства (приватный метод)
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}