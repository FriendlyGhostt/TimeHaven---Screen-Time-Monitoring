using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TimeHaven
{
    /// <summary>
    /// Логика взаимодействия для Page4.xaml
    /// </summary>
    public partial class Page4 : System.Windows.Controls.UserControl
    {

        public Page4()
        {
            InitializeComponent();
            LocalizationManager.LanguageChanged += OnLanguageChanged;
            LocalizationManager.LanguageChanged += UpdateStartupButton;

            ChangeLanguageButton.Content = LocalizationManager.CurrentCulture.Name == "ru-RU" ? "RU" : "EN";
            
            UpdateStartupButton();
        }

        private void Backup_Click(object sender, RoutedEventArgs e)
        {
            BackupManager.EnableBackup();
        }

        private void DisableBackup_Click(object sender, RoutedEventArgs e)
        {
            BackupManager.DisableBackup();
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            BackupManager.RestoreBackup();
        }

        private void ChangeLanguage_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            if (button.Content.ToString() == "RU")
            {
                LocalizationManager.SetLanguage("en-US");
                button.Content = "EN";
            }
            else
            {
                LocalizationManager.SetLanguage("ru-RU");
                button.Content = "RU";
            }
        }
        private void RunOnWindows_Click(object sender, RoutedEventArgs e)
        {
            StartupManager.ToggleStartup();
            UpdateStartupButton();
        }

        private void UpdateStartupButton()
        {
            string lang = LocalizationManager.CurrentCulture.Name;
            RunOnWindowsButton.Content = StartupManager.IsStartupEnabled()
                ? (lang == "ru-RU" ? "Отключить автозапуск" : "Disable Autostart")
                : (lang == "ru-RU" ? "Включить автозапуск" : "Enable Autostart");
        }

        private void OnLanguageChanged()
        {
            ChangeLanguageButton.Content = ChangeLanguageButton.Content.ToString() == "RU" ? "EN" : "RU";
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            BackupManager.ResetDatabase();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
            LocalizationManager.LanguageChanged -= UpdateStartupButton;
        }

    }
}
