using System.Windows;

namespace PromodoroTimerGemini
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // In App.xaml.cs
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                SetupWindow setupWindow = new SetupWindow();
                this.MainWindow = setupWindow;
                setupWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred and the application must close.\n\nError: {ex.Message}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}