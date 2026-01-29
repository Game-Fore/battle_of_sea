// Старт приложения
using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using System.Diagnostics;

namespace BattleOfSea
{
    internal class Program
    {
        private static StreamWriter? _logWriter;

        // Главный метод запуска приложения (публичный статический метод)
        public static void Main(string[] args)
        {
            try
            {
                // Создаем папку логов в текущей директории приложения
                var logsPath = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logsPath);

                var logFilePath = Path.Combine(logsPath, $"client_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
                
                // Открываем файл для логирования
                _logWriter = new StreamWriter(logFilePath, true) { AutoFlush = true };
                
                LogMessage($"✅ Client started at {DateTime.Now:HH:mm:ss.fff}");
                LogMessage($"📝 Logging to: {logFilePath}");
                
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
                
                _logWriter?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Program] Fatal error: {ex.Message}");
                throw;
            }
        }

        public static void LogMessage(string message)
        {
            try
            {
                _logWriter?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                Debug.WriteLine(message);
            }
            catch { /* ignore logging errors */ }
        }

        // Сборка Avalonia приложения (публичный статический метод)
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
    }
}