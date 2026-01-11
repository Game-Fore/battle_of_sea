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
            var gvm = new GameViewModel(room, null, demoMode: false);

            // Enemy ships placed in constructor: (0,0),(0,1),(2,3)
            await gvm.ShootAt(new BoardCell(0,0));
            Assert.NotEqual(GameState.YouWin, gvm.State);
            var after1 = gvm.EnemyRemainingShips;

            await gvm.ShootAt(new BoardCell(0,1));
            Assert.NotEqual(GameState.YouWin, gvm.State); // still one ship remaining
            var after2 = gvm.EnemyRemainingShips;

            var states = new System.Collections.Generic.List<GameState>();
            gvm.PropertyChanged += (s, e) => { if (e.PropertyName == "State") states.Add(gvm.State); };

            await gvm.ShootAt(new BoardCell(2,3));
            var after3 = gvm.EnemyRemainingShips;

            Assert.True(gvm.State == GameState.YouWin, $"Expected YouWin immediately after final shot, but was {gvm.State}; states seq: {string.Join("->", states)}; remaining ships: {after1},{after2},{after3}");

            // Give some time for background tasks that might erroneously change state
            await Task.Delay(1000);

            // Assert that YouWin was observed and that final state remains YouWin
            Assert.Contains(GameState.YouWin, states);
            Assert.True(gvm.State == GameState.YouWin, $"Final expected YouWin but was {gvm.State}; states seq: {string.Join("->", states)}; remaining ships: {after1},{after2},{after3}");
        }
    }
}
