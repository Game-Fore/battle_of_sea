// Точка входа приложения
using Avalonia;
using Avalonia.Markup.Xaml;
using System;

namespace BattleOfSea
{
    public partial class App : Application
    {
        // Demo mode: when true, the app will seed mock content and auto-open a sample game for preview
        // Can be controlled via environment variable BATTLEOFSEA_DEMO (set to "0" or "false" to disable)
        public static bool DemoMode { get; } = InitDemoMode();

        private static bool InitDemoMode()
        {
            var v = Environment.GetEnvironmentVariable("BATTLEOFSEA_DEMO");
            if (string.IsNullOrEmpty(v)) return true; // default to demo ON for local dev
            return !(v == "0" || v.Equals("false", StringComparison.OrdinalIgnoreCase));
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // Ensure a main window is created for desktop lifetimes so that the UI is visible
            if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.MainWindow == null)
                {
                    desktop.MainWindow = new MainWindow();
                }

                // Ensure window is shown and activated (helps on macOS when windows remain hidden)
                try
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"MainWindow show/activate error: {ex.Message}");
                }

                // In demo mode, don't show dialog immediately - let user see the main window first
                // User can close demo dialog if it appears, or it will close automatically
                // Commented out for now so main window is immediately visible
                /*
                if (DemoMode && desktop.MainWindow != null)
                {
                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            var dlg = new Views.DemoDialog();
                            // Show as non-modal window so main window is visible
                            dlg.Show(desktop.MainWindow);
                        }
                        catch (System.Exception ex)
                        {
                            System.Console.WriteLine($"Demo dialog error: {ex.Message}");
                        }
                    });
                }
                */
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
