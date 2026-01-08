using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BattleOfSea.Models
{
    public class ShipPlacement : INotifyPropertyChanged
    {
        public int Size { get; }
        public int Count { get; set; }
        
        private int _placed;
        public int Placed 
        { 
            get => _placed;
            set 
            { 
                _placed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Remaining));
            }
        }
        public int Remaining => Count - Placed;

        public ShipPlacement(int size, int count)
        {
            Size = size;
            Count = count;
            Placed = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ShipPlacementManager : INotifyPropertyChanged
    {
        public List<ShipPlacement> Ships { get; } = new List<ShipPlacement>
        {
            new ShipPlacement(4, 1),  // 1 корабль на 4 клетки
            new ShipPlacement(3, 2),  // 2 корабля на 3 клетки
            new ShipPlacement(2, 3),  // 3 корабля на 2 клетки
            new ShipPlacement(1, 4)   // 4 корабля на 1 клетку
        };

        public ShipPlacementManager()
        {
            // Подписываемся на изменения каждого корабля
            foreach (var ship in Ships)
            {
                ship.PropertyChanged += (s, e) =>
                {
                    OnPropertyChanged(nameof(PlacedShips));
                    OnPropertyChanged(nameof(RemainingShips));
                    OnPropertyChanged(nameof(AllShipsPlaced));
                };
            }
        }

        public int TotalShips => Ships.Sum(s => s.Count);
        public int PlacedShips => Ships.Sum(s => s.Placed);
        public int RemainingShips => Ships.Sum(s => s.Remaining);
        public bool AllShipsPlaced => RemainingShips == 0;

        public bool CanPlaceShip(int size)
        {
            var ship = Ships.FirstOrDefault(s => s.Size == size);
            return ship != null && ship.Remaining > 0;
        }

        public bool PlaceShip(int size)
        {
            var ship = Ships.FirstOrDefault(s => s.Size == size);
            if (ship != null && ship.Remaining > 0)
            {
                ship.Placed++;
                return true;
            }
            return false;
        }

        public void RemoveShip(int size)
        {
            var ship = Ships.FirstOrDefault(s => s.Size == size);
            if (ship != null && ship.Placed > 0)
            {
                ship.Placed--;
            }
        }

        public void Reset()
        {
            foreach (var ship in Ships)
            {
                ship.Placed = 0;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

