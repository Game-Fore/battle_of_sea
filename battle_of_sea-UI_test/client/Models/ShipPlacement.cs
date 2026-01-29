using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BattleOfSea.Models
{
    // Класс размещения корабля
    public class ShipPlacement : INotifyPropertyChanged
    {
        // Размер корабля
        public int Size { get; }
        // Количество кораблей
        public int Count { get; set; }
        
        private int _placed;
        // Количество размещенных кораблей
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
        // Количество оставшихся кораблей
        public int Remaining => Count - Placed;

        // Конструктор корабля
        public ShipPlacement(int size, int count)
        {
            Size = size;
            Count = count;
            Placed = 0;
        }

        // Событие изменения свойства
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Менеджер размещения кораблей
    public class ShipPlacementManager : INotifyPropertyChanged
    {
        // Список кораблей для размещения
        public List<ShipPlacement> Ships { get; } = new List<ShipPlacement>
        {
            new ShipPlacement(4, 1),  // 1 корабль на 4 клетки
            new ShipPlacement(3, 2),  // 2 корабля на 3 клетки
            new ShipPlacement(2, 3),  // 3 корабля на 2 клетки
            new ShipPlacement(1, 4)   // 4 корабля на 1 клетку
        };

        // Конструктор менеджера
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

        // Общее количество кораблей
        public int TotalShips => Ships.Sum(s => s.Count);
        // Количество размещенных кораблей
        public int PlacedShips => Ships.Sum(s => s.Placed);
        // Количество оставшихся кораблей
        public int RemainingShips => Ships.Sum(s => s.Remaining);
        // Все ли корабли размещены
        public bool AllShipsPlaced => RemainingShips == 0;

        // Проверка возможности размещения корабля
        public bool CanPlaceShip(int size)
        {
            var ship = Ships.FirstOrDefault(s => s.Size == size);
            return ship != null && ship.Remaining > 0;
        }

        // Размещение корабля
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

        // Удаление корабля
        public void RemoveShip(int size)
        {
            var ship = Ships.FirstOrDefault(s => s.Size == size);
            if (ship != null && ship.Placed > 0)
            {
                ship.Placed--;
            }
        }

        // Сброс размещения
        public void Reset()
        {
            foreach (var ship in Ships)
            {
                ship.Placed = 0;
            }
        }

        // Событие изменения свойства
        public event PropertyChangedEventHandler? PropertyChanged;
        // Уведомление об изменении свойства
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}