using Avalonia.Data.Converters;
using BattleOfSea.Models;
using System.Globalization;

namespace BattleOfSea.Converters
{
    public class StateToVisibilityConverter : IValueConverter
    {
        private readonly GameState _visibleState;

        public StateToVisibilityConverter() : this(GameState.ReadyToStart)
        {
        }

        public StateToVisibilityConverter(GameState visibleState)
        {
            _visibleState = visibleState;
        }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is GameState gameState)
            {
                return gameState == _visibleState;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter specifically for showing Ready button
    public class StateToReadyVisibleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is GameState gameState)
            {
                bool isVisible = gameState == GameState.ReadyToStart;
                System.Console.WriteLine($"[StateToReadyVisibleConverter] State={gameState}, IsVisible={isVisible}");
                if (isVisible)
                {
                    System.Console.WriteLine($"[StateToReadyVisibleConverter] ✅ Ready button VISIBLE (State = ReadyToStart)");
                }
                return isVisible;
            }
            System.Console.WriteLine($"[StateToReadyVisibleConverter] ❌ Value is not GameState, type={value?.GetType().Name}, value={value}");
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for enabling opponent board when it's YourTurn
    public class StateToOpponentBoardClickableConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is GameState gameState)
            {
                bool isClickable = gameState == GameState.YourTurn;
                return isClickable;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for showing game info during gameplay
    public class StateToGameActiveConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is GameState gameState)
            {
                // Game is active during turns
                bool isActive = gameState == GameState.YourTurn || gameState == GameState.OpponentTurn;
                return isActive;
            }
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            throw new NotImplementedException();
        }
    }
}
