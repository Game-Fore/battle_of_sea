// Точка входа приложения
using Avalonia;
using Avalonia.Markup.Xaml;
using System;

namespace BattleOfSea
{
    public partial class App : Application
    {
        // DEPRECATED: DemoMode удален - все операции требуют реального подключения к серверу
        public static bool DemoMode => false; // Всегда false - нет режима демонстрации

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