using Xunit;
using BattleOfSea.ViewModels;
using BattleOfSea.Models;
using System.Threading.Tasks;

namespace BattleOfSea.Tests
{
    public class GameViewModelShipTests
    {
        [Fact]
        public async Task YouWin_WhenAllEnemyShipsSunk()
        {
            var room = new Room("R", 0, 2);
            var gvm = new GameViewModel(room);

            // Enemy ships placed in constructor: (0,0),(0,1),(2,3)
            await gvm.ShootAt(new BoardCell(0,0));
            Assert.NotEqual(GameState.YouWin, gvm.State);

            await gvm.ShootAt(new BoardCell(0,1));
            Assert.NotEqual(GameState.YouWin, gvm.State); // still one ship remaining

            await gvm.ShootAt(new BoardCell(2,3));
            Assert.Equal(GameState.YouWin, gvm.State);
        }
    }
}
