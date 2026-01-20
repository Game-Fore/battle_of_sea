// Игровое поле
using System.Linq;
using System.Collections.ObjectModel;

namespace BattleOfSea.Models
{
    // Класс игрового поля
    public class Board
    {
        // Коллекция ячеек поля
        public ObservableCollection<BoardCell> Cells { get; } = new ObservableCollection<BoardCell>();
        // Размер поля
        public int Size { get; }

        // Создание поля заданного размера
        public Board(int size = 10)
        {
            Size = size;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    Cells.Add(new BoardCell(r, c));
        }

        // Получить ячейку по координатам
        public BoardCell? GetCell(int r, int c) => Cells.FirstOrDefault(x => x.Row == r && x.Col == c);

        // Разместить корабль в ячейке
        public void PlaceShip(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell != null) cell.HasShip = true;
        }

        /// Разместить корабль заданного размера
        public bool PlaceShip(int r, int c, int size, bool horizontal)
        {
            // Проверка возможности размещения
            if (horizontal)
            {
                if (c + size > Size) return false;
                for (int i = 0; i < size; i++)
                {
                    var cell = GetCell(r, c + i);
                    if (cell == null || cell.HasShip) return false;
                    // Проверка соседних клеток
                    if (HasAdjacentShip(r, c + i)) return false;
                }
                // Размещение корабля
                for (int i = 0; i < size; i++)
                {
                    GetCell(r, c + i)!.HasShip = true;
                }
            }
            else
            {
                if (r + size > Size) return false;
                for (int i = 0; i < size; i++)
                {
                    var cell = GetCell(r + i, c);
                    if (cell == null || cell.HasShip) return false;
                    // Проверка соседних клеток
                    if (HasAdjacentShip(r + i, c)) return false;
                }
                // Размещение корабля
                for (int i = 0; i < size; i++)
                {
                    GetCell(r + i, c)!.HasShip = true;
                }
            }
            return true;
        }

        // Проверить наличие соседнего корабля
        private bool HasAdjacentShip(int r, int c)
        {
            // Проверка всех 8 соседей
            var neighbors = new (int r, int c)[]
            {
                (r - 1, c - 1), (r - 1, c), (r - 1, c + 1),
                (r, c - 1),                 (r, c + 1),
                (r + 1, c - 1), (r + 1, c), (r + 1, c + 1)
            };
            foreach (var (nr, nc) in neighbors)
            {
                var cell = GetCell(nr, nc);
                if (cell != null && cell.HasShip) return true;
            }
            return false;
        }

        // Удалить корабль из ячейки
        public void RemoveShip(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell != null && cell.HasShip)
            {
                // Удаление всего корабля
                var ships = GetShips();
                foreach (var ship in ships)
                {
                    if (ship.Contains(cell))
                    {
                        foreach (var shipCell in ship)
                        {
                            shipCell.HasShip = false;
                        }
                        break;
                    }
                }
            }
        }

        /// Найти все корабли на поле
        public System.Collections.Generic.List<System.Collections.Generic.List<BoardCell>> GetShips()
        {
            var ships = new System.Collections.Generic.List<System.Collections.Generic.List<BoardCell>>();
            var visited = new System.Collections.Generic.HashSet<BoardCell>();
            foreach (var cell in Cells.Where(c => c.HasShip))
            {
                if (visited.Contains(cell)) continue;
                var ship = new System.Collections.Generic.List<BoardCell>();
                var stack = new System.Collections.Generic.Stack<BoardCell>();
                stack.Push(cell);
                visited.Add(cell);
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    ship.Add(cur);

                    // Поиск соседних клеток корабля
                    var neighbors = new (int r, int c)[] { (cur.Row - 1, cur.Col), (cur.Row + 1, cur.Col), (cur.Row, cur.Col - 1), (cur.Row, cur.Col + 1) };
                    foreach (var (r, c) in neighbors)
                    {
                        var n = GetCell(r, c);
                        if (n != null && n.HasShip && !visited.Contains(n))
                        {
                            visited.Add(n);
                            stack.Push(n);
                        }
                    }
                }

                ships.Add(ship);
            }

            return ships;
        }

        /// Проверить потоплен ли корабль
        public bool? IsShipSunkAt(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell == null || !cell.HasShip) return null;

            var ships = GetShips();
            foreach (var ship in ships)
            {
                if (ship.Contains(cell))
                {
                    return ship.All(s => s.IsHit);
                }
            }

            return null;
        }

        // Подсчет оставшихся кораблей
        public int RemainingShipsCount()
        {
            return GetShips().Count(s => !s.All(c => c.IsHit));
        }

        // Проверка потопления всех кораблей
        public bool AllShipsSunk() => RemainingShipsCount() == 0;

        /// Выстрел по ячейке
        public bool? ShootAt(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell == null) return null;
            if (cell.IsRevealed) return null;

            cell.IsRevealed = true;
            if (cell.HasShip)
            {
                cell.IsHit = true;
                return true;
            }
            else
            {
                cell.IsHit = false;
                return false;
            }
        }
    }
}