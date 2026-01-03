using Xunit;
using BattleOfSea.Models;

namespace BattleOfSea.Tests
{
    public class BoardShipTests
    {
        [Fact]
        public void ShipIsSunk_WhenAllCellsHit()
        {
            var b = new Board(5);
            // place a 2-cell ship at (0,0) and (0,1)
            b.PlaceShip(0, 0);
            b.PlaceShip(0, 1);

            // initial: not sunk
            Assert.False(b.IsShipSunkAt(0, 0) ?? false);

            // hit first cell
            b.ShootAt(0, 0);
            Assert.False(b.IsShipSunkAt(0, 0) ?? false);

            // hit second cell -> ship sunk
            b.ShootAt(0, 1);
            Assert.True(b.IsShipSunkAt(0, 0) ?? false);
            Assert.True(b.AllShipsSunk() == false ? false : true); // All ships sunk should be true
        }

        [Fact]
        public void RemainingShipsCount_DecreasesOnSunk()
        {
            var b = new Board(5);
            b.PlaceShip(0, 0);
            b.PlaceShip(0, 1); // one ship length 2
            b.PlaceShip(2, 2); // second ship length 1

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
