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
    }
}
