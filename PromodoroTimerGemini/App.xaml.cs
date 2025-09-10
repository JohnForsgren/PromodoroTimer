using System.Windows;

namespace PromodoroTimerGemini
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create and show the initial setup window
            // FIX: The class name is 'SetupWindow', not 'PomodoroWindows'.
            SetupWindow setupWindow = new SetupWindow();

            // Assign it as the main window so the app knows when to shut down
            this.MainWindow = setupWindow;
            setupWindow.Show();
        }
    }
}