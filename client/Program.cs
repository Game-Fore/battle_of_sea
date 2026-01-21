// Старт приложения
using Avalonia;
using Avalonia.ReactiveUI;

namespace BattleOfSea
{
    internal class Program
    {
        // Главный метод запуска приложения (публичный статический метод)
        public static void Main(string[] args)
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Сборка Avalonia приложения (публичный статический метод)
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}