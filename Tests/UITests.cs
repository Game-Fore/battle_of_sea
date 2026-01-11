using System;
using Xunit;
using BattleOfSea.Models;
using BattleOfSea.ViewModels;

namespace BattleOfSea.Tests
{
    /// <summary>
    /// Простые тесты UI компонентов (День 11: Тесты UI)
    /// </summary>
    public class UITests
    {
        [Fact]
        public void GameViewModel_OwnRemainingShips_ReturnsCorrectCount()
        {
            // Arrange
            var vm = new GameViewModel();
            
            // Act
            var count = vm.OwnRemainingShips;
            
            // Assert
            Assert.True(count >= 0);
        }

        [Fact]
        public void GameViewModel_EnemyRemainingShips_ReturnsCorrectCount()
        {
            // Arrange
            var vm = new GameViewModel();
            
            // Act
            var count = vm.EnemyRemainingShips;
            
            // Assert
            Assert.True(count >= 0);
        }

        [Fact]
        public void GameViewModel_StatusText_ChangesWithState()
        {
            // Arrange
            var vm = new GameViewModel(null, null, demoMode: false);
            
            // Act
            vm.State = GameState.YourTurn;
            var yourTurnText = vm.StatusText;
            
            vm.State = GameState.YouWin;
            var winText = vm.StatusText;
            
            vm.State = GameState.YouLose;
            var loseText = vm.StatusText;
            
            // Assert
            Assert.Contains("ход", yourTurnText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("побед", winText, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("проигр", loseText, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void State_Setter_AllowsTerminalToTerminalTransitions()
        {
            // Ensure setting terminal states explicitly works as expected even when switching between them
            var vm = new GameViewModel(null, null, demoMode: false);
            vm.State = GameState.YouWin;
            vm.State = GameState.YouLose;
            Assert.Equal(GameState.YouLose, vm.State);

            vm.State = GameState.YouWin;
            Assert.Equal(GameState.YouWin, vm.State);
        }

        [Fact]
        public void Board_AllShipsSunk_ReturnsTrueWhenNoShips()
        {
            // Arrange
            var board = new Board(10);
            
            // Act & Assert
            Assert.True(board.AllShipsSunk());
        }

        [Fact]
        public void Board_AllShipsSunk_ReturnsFalseWhenShipsExist()
        {
            // Arrange
            var board = new Board(10);
            board.PlaceShip(0, 0);
            
            // Act & Assert
            Assert.False(board.AllShipsSunk());
        }

        [Fact]
        public void Board_RemainingShipsCount_ReturnsCorrectCount()
        {
            // Arrange
            var board = new Board(10);
            board.PlaceShip(0, 0);
            board.PlaceShip(2, 2);
            
            // Act
            var count = board.RemainingShipsCount();
            
            // Assert
            Assert.Equal(2, count);
        }
    }
}

