using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;
using TimeHaven;

public static class StartupManager
{
    private const string AppName = "TimeHaven";
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string TaskName = "TimeHaven_AutoStart"; // Имя задачи в Планировщике

    public static bool IsStartupEnabled()
    {
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, false))
        {
            string? path = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(path) && File.Exists(path!.Trim('"'));
        }
    }

    public static bool IsTaskSchedulerEnabled()
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "schtasks";
            process.StartInfo.Arguments = $"/Query /TN \"{TaskName}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains(TaskName);
        }
    }

    public static void EnableStartup()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            MessageBox.Show("Error: Failed to retrieve the executable file path.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, true) ??
                               Registry.CurrentUser.CreateSubKey(RegistryPath, true))
        {
            key?.SetValue(AppName, $"\"{exePath}\"");
        }

        Task.Run(() => EnableTaskSchedulerStartup(exePath));

        SettingsManager.UpdateSetting(true, val => SettingsManager.Settings.RunOnStartup = val);
    }

    private static void EnableTaskSchedulerStartup(string exePath)
    {
        string command = $"schtasks /Create /TN \"{TaskName}\" /TR \"\\\"{exePath}\\\"\" /SC ONLOGON /RL HIGHEST /F";
        ExecuteCommand(command);
    }

    public static void DisableStartup()
    {
        using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
        {
            key?.DeleteValue(AppName, false);
        }

        Task.Run(DisableTaskSchedulerStartup);

        SettingsManager.UpdateSetting(false, val => SettingsManager.Settings.RunOnStartup = val);
    }

    private static void DisableTaskSchedulerStartup()
    {
        string command = $"schtasks /Delete /TN \"{TaskName}\" /F";
        ExecuteCommand(command);
    }

    public static void ToggleStartup()
    {
        try
        {
            bool isEnabled = IsStartupEnabled() || IsTaskSchedulerEnabled();
            string lang = LocalizationManager.CurrentCulture.Name;

            if (isEnabled)
            {
                DisableStartup();
                MessageBox.Show(lang == "ru-RU" ? "Автозапуск выключен" : "Autostart disabled",
                                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                EnableStartup();
                MessageBox.Show(lang == "ru-RU" ? "Автозапуск включен" : "Autostart enabled",
                                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Недостаточно прав для изменения автозапуска. Запустите программу от имени администратора.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public static void SyncWithRegistry()
    {
        bool isStartupEnabled = IsStartupEnabled() || IsTaskSchedulerEnabled();
        if (SettingsManager.Settings.RunOnStartup != isStartupEnabled)
        {
            SettingsManager.Settings.RunOnStartup = isStartupEnabled;
            SettingsManager.SaveSettings();
        }
    }

    private static void ExecuteCommand(string command)
    {
        using (Process process = new Process())
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = $"/c {command}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
        }
    }
}
