using Xunit;
using BattleOfSea.Models;

namespace BattleOfSea.Tests
{
    // Тесты для игрового поля
    public class BoardTests
    {
        [Fact]
        // Тест промаха при выстреле (публичный метод)
        public void ShootAt_Miss_SetsRevealedAndNotHit()
        {
            var b = new Board(5);
            var res = b.ShootAt(1, 1);
            Assert.False(res == true);
            var c = b.GetCell(1, 1);
            Assert.True(c.IsRevealed);
            Assert.False(c.IsHit);
        }

        [Fact]
        // Тест попадания при выстреле (публичный метод)
        public void ShootAt_Hit_SetsRevealedAndHit()
        {
            var b = new Board(5);
            b.PlaceShip(2, 2);
            var res = b.ShootAt(2, 2);
            Assert.True(res == true);
            var c = b.GetCell(2, 2);
            Assert.True(c.IsRevealed);
            Assert.True(c.IsHit);
        }

        [Fact]
        // Тест выстрела в уже открытую ячейку (публичный метод)
        public void ShootAt_AlreadyRevealed_ReturnsNull()
        {
            var b = new Board(5);
            b.ShootAt(0, 0);
            var res = b.ShootAt(0, 0);
            Assert.Null(res);
        }
    }
}