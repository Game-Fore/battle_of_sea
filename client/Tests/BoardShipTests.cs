using Xunit;
using BattleOfSea.Models;

namespace BattleOfSea.Tests
{
    // Тесты для игрового поля и кораблей
    public class BoardShipTests
    {
        [Fact]
        // Тест потопления корабля при поражении всех его ячеек (публичный метод)
        public void ShipIsSunk_WhenAllCellsHit()
        {
            var b = new Board(5);
            // размещаем 2-палубный корабль на позициях (0,0) и (0,1)
            b.PlaceShip(0, 0);
            b.PlaceShip(0, 1);

            // начальное состояние: корабль не потоплен
            Assert.False(b.IsShipSunkAt(0, 0) ?? false);

            // попадание в первую ячейку
            b.ShootAt(0, 0);
            Assert.False(b.IsShipSunkAt(0, 0) ?? false);

            // попадание во вторую ячейку -> корабль потоплен
            b.ShootAt(0, 1);
            Assert.True(b.IsShipSunkAt(0, 0) ?? false);
            Assert.True(b.AllShipsSunk() == false ? false : true); // Все корабли потоплены должно быть true
        }

        [Fact]
        // Тест уменьшения количества оставшихся кораблей при потоплении (публичный метод)
        public void RemainingShipsCount_DecreasesOnSunk()
        {
            var b = new Board(5);
            b.PlaceShip(0, 0);
            b.PlaceShip(0, 1); // один корабль длиной 2
            b.PlaceShip(2, 2); // второй корабль длиной 1

            Assert.Equal(2, b.RemainingShipsCount());

            b.ShootAt(0, 0);
            b.ShootAt(0, 1);
            Assert.Equal(1, b.RemainingShipsCount());

            b.ShootAt(2, 2);
            Assert.Equal(0, b.RemainingShipsCount());
            Assert.True(b.AllShipsSunk());
        }
    }
}