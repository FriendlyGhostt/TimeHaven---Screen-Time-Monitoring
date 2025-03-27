using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Newtonsoft.Json;

namespace TimeHaven
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static AppSettings Settings { get; private set; }

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    Settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings { Language = AppSettings.GetSystemLanguage() };
                }
                catch
                {
                    Settings = new AppSettings { Language = AppSettings.GetSystemLanguage() };
                }
            }
            else
            {
                Settings = new AppSettings { Language = AppSettings.GetSystemLanguage() };
            }

            LocalizationManager.SetLanguage(Settings.Language);
            SaveSettings();
            StartupManager.SyncWithRegistry();
            BackupManager.CheckBackupOnStartup();

            Task.Delay(2000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() => CleanupExpiredReminders());
            });
        }


        public static void SaveSettings()
        {
            string json = JsonConvert.SerializeObject(Settings, Newtonsoft.Json.Formatting.Indented);
            if (!File.Exists(SettingsFilePath) || File.ReadAllText(SettingsFilePath) != json)
            {
                File.WriteAllText(SettingsFilePath, json);
            }
        }
        public static void UpdateSetting<T>(T newValue, Action<T> updateProperty)
        {
            updateProperty(newValue);
            SaveSettings();
        }

        public static string GetSettingsFilePath()
        {
            return SettingsFilePath;
        }

        private static void CleanupExpiredReminders()
        {
            if (Settings.Reminders == null || Settings.Reminders.Count == 0)
                return;

            DateTime now = DateTime.Now;
            List<DateTime> expiredReminders = Settings.Reminders.Where(r => r < now).ToList();

            if (expiredReminders.Count > 0)
            {
                string remindersRu = "Просроченные напоминания:\n" + string.Join("\n", expiredReminders.Select(r => r.ToString("g")));
                string remindersEn = "Expired reminders:\n" + string.Join("\n", expiredReminders.Select(r => r.ToString("g")));

                foreach (var reminder in expiredReminders.ToList())
                {
                    Settings.Reminders.Remove(reminder);
                }

                SaveSettings();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowMessage(remindersRu, "Напоминания", remindersEn, "Reminders");
                });
            }
        }


        public static void ShowMessage(string textRu, string titleRu, string textEn, string titleEn)
        {
            string lang = GetCurrentLanguage();
            MessageBox.Show(lang == "ru" ? textRu : textEn, lang == "ru" ? titleRu : titleEn, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static string GetCurrentLanguage()
        {
            return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        }

    }
}
