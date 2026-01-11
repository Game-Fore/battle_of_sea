// Игровое поле
using System.Linq;
using System.Collections.ObjectModel;

namespace BattleOfSea.Models
{
    public class Board
    {
        public ObservableCollection<BoardCell> Cells { get; } = new ObservableCollection<BoardCell>();
        public int Size { get; }

        public Board(int size = 10)
        {
            Size = size;
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                    Cells.Add(new BoardCell(r, c));
        }

        public BoardCell? GetCell(int r, int c) => Cells.FirstOrDefault(x => x.Row == r && x.Col == c);

        public void PlaceShip(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell != null) cell.HasShip = true;
        }

        /// <summary>
        /// Place a ship of given size starting at (r, c) in given direction (true = horizontal, false = vertical)
        /// Returns true if placement was successful
        /// </summary>
        public bool PlaceShip(int r, int c, int size, bool horizontal)
        {
            // Check if ship fits on board
            if (horizontal)
            {
                if (c + size > Size) return false;
                for (int i = 0; i < size; i++)
                {
                    var cell = GetCell(r, c + i);
                    if (cell == null || cell.HasShip) return false;
                    // Check adjacent cells (no touching ships)
                    if (HasAdjacentShip(r, c + i)) return false;
                }
                // Place the ship
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
                    // Check adjacent cells (no touching ships)
                    if (HasAdjacentShip(r + i, c)) return false;
                }
                // Place the ship
                for (int i = 0; i < size; i++)
                {
                    GetCell(r + i, c)!.HasShip = true;
                }
            }
            return true;
        }

        private bool HasAdjacentShip(int r, int c)
        {
            // Check all 8 neighbors (including diagonals)
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

        public void RemoveShip(int r, int c)
        {
            var cell = GetCell(r, c);
            if (cell != null && cell.HasShip)
            {
                // Remove entire ship (connected cells)
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

        /// <summary>
        /// Find contiguous groups of ship cells (4-way adjacency) representing individual ships.
        /// </summary>
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

                    // neighbors: up/down/left/right
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

        /// <summary>
        /// Returns true if the ship that contains the given cell coordinates is fully hit/sunk.
        /// Returns false if not sunk, or null if the cell is not a ship cell.
        /// </summary>
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

        public int RemainingShipsCount()
        {
            return GetShips().Count(s => !s.All(c => c.IsHit));
        }

        public bool AllShipsSunk() => RemainingShipsCount() == 0;

        /// <summary>
        /// Shoot at a given cell. Returns true if hit, false if miss, null if already revealed or invalid.
        /// </summary>
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
