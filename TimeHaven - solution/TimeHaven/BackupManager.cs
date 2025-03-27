using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TimeHaven
{
    public static class BackupManager
    {
        static BackupManager()
        {
            if (SettingsManager.Settings.BackupEnabled)
            {
                _ = AutoBackup();
            }
        }

        public static void EnableBackup()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SettingsManager.Settings.BackupDirectory = dialog.SelectedPath;
                SettingsManager.Settings.BackupEnabled = true;
                SettingsManager.SaveSettings();

                SettingsManager.ShowMessage(
                    "Авто-бэкап включён!\nФайлы будут сохраняться в: " + SettingsManager.Settings.BackupDirectory,
                    "Бэкап включён",
                    "Auto-backup enabled!\nFiles will be saved in: " + SettingsManager.Settings.BackupDirectory,
                    "Backup enabled"
                );

                PerformBackup();
                _ = AutoBackup();
            }
        }

        private static async Task AutoBackup()
        {
            while (SettingsManager.Settings.BackupEnabled)
            {
                if (DateTime.Now.Date > SettingsManager.Settings.LastBackupTime.Date &&
                    !string.IsNullOrWhiteSpace(SettingsManager.Settings.BackupDirectory))
                {
                    string zipPath = Path.Combine(SettingsManager.Settings.BackupDirectory, $"timehaven_backup_{DateTime.Now:yyyy-MM-dd}.zip");

                    try
                    {
                        using (FileStream zipToCreate = new FileStream(zipPath, FileMode.Create))
                        using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                        {
                            archive.CreateEntryFromFile("datatime.db", "datatime.db");
                            archive.CreateEntryFromFile("timehaven.db", "timehaven.db");
                        }

                        SettingsManager.Settings.LastBackupTime = DateTime.Now;
                        SettingsManager.SaveSettings();
                    }
                    catch (Exception ex)
                    {
                        SettingsManager.ShowMessage(
                            $"Ошибка авто-бэкапа: {ex.Message}", "Ошибка",
                            $"Auto-backup error: {ex.Message}", "Error"
                        );
                    }
                }
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        public static void DisableBackup()
        {
            if (ShowConfirmation(
                "Отключить резервное копирование?", "Подтверждение",
                "Disable backup?", "Confirmation"))
            {
                SettingsManager.Settings.BackupEnabled = false;
                SettingsManager.SaveSettings();
                SettingsManager.ShowMessage("Авто-бэкап отключён.", "Отключено", "Auto-backup disabled.", "Disabled");
            }
        }

        public static void RestoreBackup()
        {
            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "ZIP Archives (*.zip)|*.zip",
                Title = SettingsManager.GetCurrentLanguage() == "ru" ? "Выберите файл резервной копии" : "Select backup file"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string zipPath = dialog.FileName;

                try
                {
                    using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            string destinationPath = Path.Combine(Directory.GetCurrentDirectory(), entry.Name);
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }

                    SettingsManager.ShowMessage("Бэкап успешно восстановлен!", "Восстановление", "Backup successfully restored!", "Restore");
                }
                catch (Exception ex)
                {
                    SettingsManager.ShowMessage($"Ошибка при восстановлении: {ex.Message}", "Ошибка", $"Restore error: {ex.Message}", "Error");
                }
            }
        }

        private static void PerformBackup()
        {
            if (!string.IsNullOrWhiteSpace(SettingsManager.Settings.BackupDirectory))
            {
                string zipPath = Path.Combine(SettingsManager.Settings.BackupDirectory, $"timehaven_backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip");

                try
                {
                    using (FileStream zipToCreate = new FileStream(zipPath, FileMode.Create))
                    using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile("datatime.db", "datatime.db");
                        archive.CreateEntryFromFile("timehaven.db", "timehaven.db");
                    }

                    SettingsManager.Settings.LastBackupTime = DateTime.Now;
                    SettingsManager.SaveSettings();
                }
                catch (Exception ex)
                {
                    SettingsManager.ShowMessage($"Ошибка резервного копирования: {ex.Message}", "Ошибка", $"Backup error: {ex.Message}", "Error");
                }
            }
        }

        private static bool ShowConfirmation(string textRu, string titleRu, string textEn, string titleEn)
        {
            string lang = SettingsManager.GetCurrentLanguage();
            MessageBoxResult result = MessageBox.Show(lang == "ru" ? textRu : textEn, lang == "ru" ? titleRu : titleEn, MessageBoxButton.YesNo, MessageBoxImage.Warning);
            return result == MessageBoxResult.Yes;
        }

        public static void ResetDatabase()
        {
            if (ShowConfirmation(
                "Вы уверены, что хотите очистить все данные?", "Подтверждение",
                "Are you sure you want to clear all data?", "Confirmation"))
            {
                MainDatabaseManager.Instance.ClearTestData();
                DatabaseManager.Instance.ClearTestData();
                SettingsManager.ShowMessage("Данные успешно очищены.", "Очистка", "Data successfully cleared.", "Cleared");
            }
        }

        public static void CheckBackupOnStartup()
        {
            if (!SettingsManager.Settings.BackupEnabled)
                return;

            DateTime lastBackupDate = SettingsManager.Settings.LastBackupTime.Date;
            DateTime currentDate = DateTime.Now.Date;

            if (currentDate > lastBackupDate) 
            {
                PerformBackup();
            }
        }
    }
}
