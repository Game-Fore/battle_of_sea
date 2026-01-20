// Точка входа приложения
using Avalonia;
using Avalonia.Markup.Xaml;
using System;

namespace BattleOfSea
{
    public partial class App : Application
    {
        // Режим демонстрации: когда true, приложение будет заполнять мок-контентом и автоматически открывать пример игры для предварительного просмотра
        // Можно контролировать через переменную окружения BATTLEOFSEA_DEMO (установите "0" или "false" для отключения)
        public static bool DemoMode { get; } = InitDemoMode();

        // Инициализация режима демонстрации (приватный статический метод)
        private static bool InitDemoMode()
        {
            var v = Environment.GetEnvironmentVariable("BATTLEOFSEA_DEMO");
            if (string.IsNullOrEmpty(v)) return true; // по умолчанию демо ВКЛ для локальной разработки
            return !(v == "0" || v.Equals("false", StringComparison.OrdinalIgnoreCase));
        }

        // Инициализация приложения (публичный метод переопределения)
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // Завершение инициализации фреймворка (публичный метод переопределения)
        public override void OnFrameworkInitializationCompleted()
        {
            // Гарантируем создание главного окна для desktop-приложений, чтобы UI был видим
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                // Гарантируем показ и активацию окна (помогает на macOS, где окна могут оставаться скрытыми)
                try
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"MainWindow show/activate error: {ex.Message}");
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}