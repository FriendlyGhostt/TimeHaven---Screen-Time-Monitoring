using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using Microsoft.Toolkit.Uwp.Notifications;
using System.IO;
using Windows.UI.Notifications;

namespace TimeHaven
{
    public class NotificationManager
    {
        private static DispatcherTimer _periodicTimer = new DispatcherTimer();
        private static DispatcherTimer _reminderTimer = new DispatcherTimer();
        private static string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "Icon.ico");

        public static event Action? OnPaused;

        private const double MinWidth = 350;
        private const double MaxWidth = 450;
        private const double MinHeight = 57;
        private const double MinTextHeight = 50;
        private const double ButtonPanelHeight = 27;
        private const double MaxHeight = 280;

        public static void StartTimers()
        {
            if (SettingsManager.Settings.SwitchNotification)
            {
                if (_periodicTimer == null) _periodicTimer = new DispatcherTimer();

                _periodicTimer.Stop();
                _periodicTimer.Tick -= OnPeriodicTimerTick!;
                _periodicTimer.Interval = TimeSpan.FromMinutes(SettingsManager.Settings.StepTimeMinutes);
                _periodicTimer.Tick += OnPeriodicTimerTick!;
                _periodicTimer.Start();
            }

            if (_reminderTimer == null) _reminderTimer = new DispatcherTimer();

            _reminderTimer.Stop();
            _reminderTimer.Tick -= OnReminderTimerTick!;
            _reminderTimer.Interval = TimeSpan.FromSeconds(10);
            _reminderTimer.Tick += OnReminderTimerTick!;
            _reminderTimer.Start();
        }

        public static void StartTimerPeriod()
        {
            if (SettingsManager.Settings.SwitchNotification)
            {
                if (_periodicTimer == null) _periodicTimer = new DispatcherTimer();

                _periodicTimer.Stop();
                _periodicTimer.Tick -= OnPeriodicTimerTick!;
                _periodicTimer.Interval = TimeSpan.FromMinutes(SettingsManager.Settings.StepTimeMinutes);
                _periodicTimer.Tick += OnPeriodicTimerTick!;
                _periodicTimer.Start();
            }
        }

        public static void UpdateTimerInterval()
        {
            if (_periodicTimer == null)
            {
                _periodicTimer = new DispatcherTimer();
                _periodicTimer.Tick += OnPeriodicTimerTick!;
            }
            _periodicTimer.Stop();
            _periodicTimer.Interval = TimeSpan.FromMinutes(SettingsManager.Settings.StepTimeMinutes);
            _periodicTimer.Start();
        }



        private static void OnPeriodicTimerTick(object sender, EventArgs e)
        {
            ShowNotification();
        }

        private static void OnReminderTimerTick(object sender, EventArgs e)
        {
            CheckReminders();
        }


        private static void CheckReminders()
        {
            DateTime now = DateTime.Now;
            var dueReminders = SettingsManager.Settings.Reminders
                .Where(rt => rt <= now)
                .ToList();

            if (dueReminders.Any())
            {
                Console.WriteLine($"[CheckReminders] {dueReminders.Count} напоминаний вызвано");

                foreach (var reminder in dueReminders)
                {
                    ShowExclusiveNotification(reminder);
                }

                foreach (var reminder in dueReminders)
                {
                    Page3.RemoveReminderStatic(reminder);
                }
                Page3.ReloadRemindersStatic();
            }
        }

        private static void ShowNotification()
        {
            if (!SettingsManager.Settings.SwitchNotification)
                return;


            Application.Current.Dispatcher.Invoke(() =>
            {
                var notificationWindow = new Notification();
                double textHeight = 0;
                string notificationText = "";

                if (SettingsManager.Settings.SwitchSound)
                {
                    AudioManager.PlaySelectedAudio();
                }

                if (SettingsManager.Settings.SwitchText && SettingsManager.Settings.Notifications.Any())
                {
                    Random rand = new Random();
                    notificationText = SettingsManager.Settings.Notifications[rand.Next(SettingsManager.Settings.Notifications.Count)];
                    notificationWindow.NotificationText.Text = notificationText;

                    notificationWindow.NotificationText.UpdateLayout();
                    textHeight = Math.Max(MinTextHeight, notificationWindow.NotificationText.ActualHeight);
                    notificationWindow.NotificationText.Visibility = Visibility.Visible;
                }
                else
                {
                    notificationWindow.NotificationText.Visibility = Visibility.Collapsed;
                    textHeight = 0;
                }

                double newHeight = MinHeight + textHeight + ButtonPanelHeight;
                notificationWindow.Height = Math.Clamp(newHeight, MinHeight, MaxHeight);

                double heightRatio = (notificationWindow.Height - MinTextHeight) / (MaxHeight - MinTextHeight);
                notificationWindow.Width = MinWidth + (MaxWidth - MinWidth) * heightRatio;

                notificationWindow.ShowActivated = false;
                notificationWindow.Topmost = true;
                notificationWindow.ShowInTaskbar = false;
                notificationWindow.Owner = Application.Current.MainWindow;
                notificationWindow.Show();

                ShowToastNotification(notificationText);
            });
        }

        private static void ShowExclusiveNotification(DateTime reminderTime)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                string notificationText = $"{reminderTime:dd.MM.yyyy HH:mm}";

                if (SettingsManager.Settings.SwitchText && SettingsManager.Settings.Notifications.Any())
                {
                    Random rand = new Random();
                    string additionalText = SettingsManager.Settings.Notifications[rand.Next(SettingsManager.Settings.Notifications.Count)];
                    notificationText += $"\n{additionalText}";
                }

                var notificationWindow = new Notification();
                notificationWindow.NotificationText.Text = notificationText;

                notificationWindow.NotificationText.UpdateLayout();
                double textHeight = Math.Max(MinTextHeight, notificationWindow.NotificationText.ActualHeight);
                notificationWindow.NotificationText.Visibility = Visibility.Visible;

                double newHeight = MinHeight + textHeight + ButtonPanelHeight;
                notificationWindow.Height = Math.Clamp(newHeight, MinHeight, MaxHeight);

                double heightRatio = (notificationWindow.Height - MinTextHeight) / (MaxHeight - MinTextHeight);
                notificationWindow.Width = MinWidth + (MaxWidth - MinWidth) * heightRatio;

                notificationWindow.ShowActivated = false;
                notificationWindow.Topmost = true;
                notificationWindow.ShowInTaskbar = false;
                notificationWindow.Owner = Application.Current.MainWindow;
                notificationWindow.Show();

                if (SettingsManager.Settings.SwitchSound)
                {
                    AudioManager.PlaySelectedAudio();
                }
            });
        }


        private static void ShowToastNotification(string notificationText)
        {
            var content = new ToastContentBuilder()
                .AddText("TimeHaven")
                .AddText(notificationText)
                .AddAppLogoOverride(new Uri($"file:///{iconPath}"))
                .GetToastContent();

            var toast = new ToastNotification(content.GetXml())
            {
                SuppressPopup = true
            };

            toast.Tag = "silent";
            toast.Group = "silent";
            ToastNotificationManager.CreateToastNotifier("TimeHaven").Show(toast);
        }

        public static void StopTimer()
        {
            OnPaused?.Invoke();
            _periodicTimer.Stop();
        }
    }
}
