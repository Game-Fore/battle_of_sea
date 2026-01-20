using Xunit;
using BattleOfSea.ViewModels;
using BattleOfSea.Models;
using System.Threading.Tasks;

namespace BattleOfSea.Tests
{
    // Тесты для модели представления игры
    public class GameViewModelTests
    {
        [Fact]
        // Тест выстрела и обновления состояния и ячейки (публичный метод)
        public async Task ShootAt_UpdatesStateAndCell()
        {
            var gvm = new GameViewModel(new Room("R",0,2));
            var target = gvm.Enemy.GetCell(0,0);
            // проверяем, что там есть корабль из конструктора
            Assert.True(target.HasShip);

            await gvm.ShootAt(target);
            Assert.True(target.IsRevealed);
            Assert.True(target.IsHit);
            Assert.Equal(Models.GameState.OpponentTurn, gvm.State);
        }
    }
}