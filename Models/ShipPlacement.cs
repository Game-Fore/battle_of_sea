using System.Collections.Generic;
using System.Linq;

namespace BattleOfSea.Models
{
    public class ShipPlacement
    {
        public int Size { get; }
        public int Count { get; set; }
        public int Placed { get; set; }
        public int Remaining => Count - Placed;

        public ShipPlacement(int size, int count)
        {
            Size = size;
            Count = count;
            Placed = 0;
        }
    }

    public class ShipPlacementManager
    {
        public List<ShipPlacement> Ships { get; } = new List<ShipPlacement>
        {
            new ShipPlacement(4, 1),  // 1 корабль на 4 клетки
            new ShipPlacement(3, 2),  // 2 корабля на 3 клетки
            new ShipPlacement(2, 3),  // 3 корабля на 2 клетки
            new ShipPlacement(1, 4)   // 4 корабля на 1 клетку
        };

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
    }
}

