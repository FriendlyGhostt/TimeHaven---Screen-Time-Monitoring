using System.Linq;
using System.Windows;

namespace TimeHaven
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            
            SettingsManager.LoadSettings();
            LocalizationManager.SetLanguage(SettingsManager.Settings.Language);
            NotificationManager.StartTimers();

            
            bool hideWindow = e.Args.Contains("--silent");

            if (hideWindow && Current.MainWindow != null)
            {
                Current.MainWindow.Hide(); 
                Current.MainWindow.WindowState = WindowState.Minimized;
            }
        }
    }
}
