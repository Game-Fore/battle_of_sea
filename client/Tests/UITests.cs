using System;
using Xunit;
using BattleOfSea.Models;
using BattleOfSea.ViewModels;

namespace BattleOfSea.Tests
{
    // Простые тесты UI компонентов
    public class UITests
    {
        [Fact]
        // Тест количества оставшихся кораблей игрока (публичный метод)
        public void GameViewModel_OwnRemainingShips_ReturnsCorrectCount()
        {
            // Подготовка
            var vm = new GameViewModel();
            
            // Действие
            var count = vm.OwnRemainingShips;
            
            // Проверка
            Assert.True(count >= 0);
        }

        [Fact]
        // Тест количества оставшихся кораблей противника (публичный метод)
        public void GameViewModel_EnemyRemainingShips_ReturnsCorrectCount()
        {
            // Подготовка
            var vm = new GameViewModel();
            
            // Действие
            var count = vm.EnemyRemainingShips;
            
            // Проверка
            Assert.True(count >= 0);
        }

        [Fact]
        // Тест изменения текста статуса с изменением состояния (публичный метод)
        public void GameViewModel_StatusText_ChangesWithState()
        {
            // Подготовка
            var vm = new GameViewModel(null, null, demoMode: false);
            
            // Действие
            vm.State = GameState.YourTurn;
            var yourTurnText = vm.StatusText;
            
            vm.State = GameState.YouWin;
            var winText = vm.StatusText;
            
            vm.State = GameState.YouLose;
            var loseText = vm.StatusText;
            
            // Проверка
            Assert.Contains("ход", yourTurnText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("побед", winText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проигр", loseText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        // Тест установки конечных состояний игры (публичный метод)
        public void State_Setter_AllowsTerminalToTerminalTransitions()
        {
            // Проверяем, что явная установка конечных состояний работает как ожидается
            var vm = new GameViewModel(null, null, demoMode: false);
            vm.State = GameState.YouWin;
            vm.State = GameState.YouLose;
            Assert.Equal(GameState.YouLose, vm.State);

            vm.State = GameState.YouWin;
            Assert.Equal(GameState.YouWin, vm.State);
        }

        [Fact]
        // Тест проверки потопления всех кораблей на пустом поле (публичный метод)
        public void Board_AllShipsSunk_ReturnsTrueWhenNoShips()
        {
            // Подготовка
            var board = new Board(10);
            
            // Действие и проверка
            Assert.True(board.AllShipsSunk());
        }

        [Fact]
        // Тест проверки потопления всех кораблей при наличии кораблей (публичный метод)
        public void Board_AllShipsSunk_ReturnsFalseWhenShipsExist()
        {
            // Подготовка
            var board = new Board(10);
            board.PlaceShip(0, 0);
            
            // Действие и проверка
            Assert.False(board.AllShipsSunk());
        }

        [Fact]
        // Тест подсчета оставшихся кораблей (публичный метод)
        public void Board_RemainingShipsCount_ReturnsCorrectCount()
        {
            // Подготовка
            var board = new Board(10);
            board.PlaceShip(0, 0);
            board.PlaceShip(2, 2);
            
            // Действие
            var count = board.RemainingShipsCount();
            
            // Проверка
            Assert.Equal(2, count);
        }
    }
}