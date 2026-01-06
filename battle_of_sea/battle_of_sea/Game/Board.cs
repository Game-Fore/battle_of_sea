namespace battle_of_sea.Game
{
    public enum CellState
    {
        Empty,
        Ship,
        Hit,
        Miss
    }

    public class Board
    {
        public const int Size = 10;
        public CellState[,] Cells { get; private set; } = new CellState[Size, Size];

        public Board() { }

        // Разместить корабль по координатам (простая заглушка)
        public void PlaceShip(int x, int y)
        {
            Cells[x, y] = CellState.Ship;
        }

        // Попадание
        public bool Shoot(int x, int y)
        {
            if (Cells[x, y] == CellState.Ship)
            {
                Cells[x, y] = CellState.Hit;
                return true;
            }
            else if (Cells[x, y] == CellState.Empty)
            {
                Cells[x, y] = CellState.Miss;
                return false;
            }

            return false; // если уже было попадание/промах
        }
        public bool IsDefeated()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    if (Cells[i, j] == CellState.Ship)
                        return false; // есть хотя бы один корабль
                }
            }
            return true; // кораблей не осталось
        }

    }
}
