using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace TimeHaven
{

    public partial class Page3 : System.Windows.Controls.UserControl
    {
        private bool switchNotification;
        private bool switchSound;
        private bool switchText;

        private readonly string soundFolderPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sound");
        private string selectedAudio;

        string[] extensions = { "*.wav", "*.mp3", "*.wma", "*.aac", "*.m4a", "*.flac" };
        public ObservableCollection<string> SoundFiles { get; set; } = new ObservableCollection<string>();

        private string stepTime;
        private int stepTimeMinutes;

        private readonly string[] notificationsdefault =
{
    "Перегрев процессора! Твой мозг работает на максимальных оборотах...",
    "Графика размывается! Похоже, твоё зрение устало от пикселей...",
    "Уведомление: Уведомляю :)",
    "Ошибка рендеринга! Глаза устали обрабатывать столько информации. Отключись на пару минут: поморгай, разомни шею и взгляни в сторону природы.",
    "Пора выглянуть в окно?"
};

        private static List<string> ?notifications;
        public ObservableCollection<DateTime> reminders;

        string currentLang = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;

        private static Page3 ?instance;

        public Page3()
        {
            InitializeComponent();
            DataContext = this;
            instance = this;
            LoadSoundFiles();
            WatchSoundFolder();

            LoadSettingsToFields();
            UpdateMarginBasedOnLanguage();

            LocalizationManager.LanguageChanged += UpdateMarginBasedOnLanguage;

            //LocalizationManager.LanguageChanged += () =>
            //{
            //    stepTime = $"20{LocalizationManager.GetTimeSuffix()}";
            //};
        }

        private void ShowTooltip(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.ToolTip is System.Windows.Controls.ToolTip toolTip)
            {
                toolTip.IsOpen = true;
            }
        }


        private void ToggleSwitchNotification_Click(object sender, MouseButtonEventArgs e)
        {
            switchNotification = !switchNotification;
            SettingsManager.UpdateSetting(switchNotification, val => SettingsManager.Settings.SwitchNotification = val);
            if (switchNotification)
            {
                ToggleSwitchNotification(switchNotification);
                NotificationOffOn(switchNotification);
            }
            else
            {
                ToggleSwitchNotification(switchNotification);
                NotificationOffOn(switchNotification);
            }
            NotificationManager.UpdateTimerInterval();
        }

        private void ToggleSwitchNotification(bool swtchNoty)
        {
            if (swtchNoty)
            {
                toggleSwitchNotification.Background = new SolidColorBrush(Colors.DodgerBlue);
                toggleKnobNotification.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            }
            else
            {
                toggleSwitchNotification.Background = new SolidColorBrush(Colors.Gray);
                toggleKnobNotification.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            }
        }

        private void NotificationOffOn(bool notyonoff)
        {
            notifyPanel.IsEnabled = notyonoff;

            if (!notyonoff)
            {
                switchSound = false;
                switchText = false;

                ToggleSwitchSound_Bool(false);
                ToggleSwitchText_Bool(false);

                SoundBox.SelectedIndex = -1;

                OpenSoundFolderButton.Opacity = 0.5;
                AddSoundButton.Opacity = 0.5;
            }
            else
            {
                OpenSoundFolderButton.Opacity = 1;
                AddSoundButton.Opacity = 1;
            }
            SoundBox.IsEnabled = notyonoff && switchSound;
            textNotificationBox.IsEnabled = notyonoff && switchText;
            OpenSoundFolderButton.IsEnabled = notyonoff;
            AddSoundButton.IsEnabled = notyonoff;
            TextComboTime.IsEnabled = notyonoff;
        }

        private void ToggleSwitchSound_Click(object sender, MouseButtonEventArgs e)
        {
            switchSound = !switchSound;
            ToggleSwitchSound_Bool(switchSound);
            SettingsManager.UpdateSetting(switchSound, val => SettingsManager.Settings.SwitchSound = val);
            if (switchSound)
            {
                SoundBox.SelectedItem = SettingsManager.Settings.SelectedAudio;
            }
        }
        private void ToggleSwitchSound_Bool(bool swtch)
        {
            switchSound = swtch;
            SettingsManager.UpdateSetting(switchSound, val => SettingsManager.Settings.SwitchSound = val);
            ToggleSwitchSound();
        }

        private void ToggleSwitchSound()
        {
            if (switchSound)
            {
                toggleSwitchSound.Background = new SolidColorBrush(Colors.DodgerBlue);
                toggleKnobSound.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                SoundBox.IsEnabled = true;
            }
            else
            {
                toggleSwitchSound.Background = new SolidColorBrush(Colors.Gray);
                toggleKnobSound.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                SoundBox.SelectedIndex = -1;
                SoundBox.IsEnabled = false;
            }
        }

        private void ToggleSwitchText_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            switchText = !switchText;
            SettingsManager.UpdateSetting(switchText, val => SettingsManager.Settings.SwitchText = val);
            ToggleSwitchText();
        }
        private void ToggleSwitchText_Bool(bool swtch)
        {
            switchText = swtch;
            SettingsManager.UpdateSetting(switchText, val => SettingsManager.Settings.SwitchText = val);
            ToggleSwitchText();
        }
        private void ToggleSwitchText()
        {
            if (switchText)
            {
                toggleKnobText.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                toggleSwitchText.Background = System.Windows.Media.Brushes.DodgerBlue;
                textNotificationBox.IsEnabled = true;
            }
            else
            {
                toggleKnobText.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                toggleSwitchText.Background = System.Windows.Media.Brushes.Gray;
                textNotificationBox.IsEnabled = false;
            }
        }


        private void SoundBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SoundBox.SelectedItem is string selected)
            {
                selectedAudio = System.IO.Path.Combine(soundFolderPath, selected);

                SettingsManager.Settings.SelectedAudio = selected;
                SettingsManager.SaveSettings();
            }
        }



        private void LoadSoundFiles()
        {
            if (!Directory.Exists(soundFolderPath))
                Directory.CreateDirectory(soundFolderPath);

            SoundFiles.Clear();

            foreach (var ext in extensions)
            {
                var files = Directory.GetFiles(soundFolderPath, ext);
                foreach (var file in files)
                {
                    SoundFiles.Add(System.IO.Path.GetFileName(file));
                }
            }
        }

        private void WatchSoundFolder()
        {
            var watcher = new FileSystemWatcher(soundFolderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "*.*"
            };

            watcher.Created += (s, e) =>
            {
                if (extensions.Any(ext => e.FullPath.EndsWith(ext.TrimStart('*'), StringComparison.OrdinalIgnoreCase)))
                    Dispatcher.Invoke(LoadSoundFiles);
            };
            watcher.Deleted += (s, e) =>
            {
                if (extensions.Any(ext => e.FullPath.EndsWith(ext.TrimStart('*'), StringComparison.OrdinalIgnoreCase)))
                    Dispatcher.Invoke(LoadSoundFiles);
            };

            watcher.EnableRaisingEvents = true;

        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AddSound_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog 
            {
                Filter = "Audio Files (*.wav;*.mp3;*.wma;*.aac;*.m4a;*.flac)|*.wav;*.mp3;*.wma;*.aac;*.m4a;*.flac", 
                Multiselect = false,
            };
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string destinationPath = System.IO.Path.Combine(soundFolderPath, System.IO.Path.GetFileName(openFileDialog.FileName));
                File.Copy(openFileDialog.FileName, destinationPath, true);
                Dispatcher.Invoke(LoadSoundFiles);
            }
        }

        private void OpenSoundFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", soundFolderPath);
        }

        private void ComboBoxTime_LostFocus(object sender, RoutedEventArgs e)
        {
            if (comboBoxTime.Text.Trim() != stepTime)
            {
                UpdateTimeText();
                UpdateStepTimeText();
            }
        }


        private void ComboBoxTime_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                UpdateTimeText();
                UpdateStepTimeText();
                FocusManager.SetFocusedElement(this, null);
                Keyboard.ClearFocus();
            }
        }

        private void UpdateTimeText()
        {
            string enteredText = comboBoxTime.Text.Trim();
            string numberOnly = Regex.Replace(enteredText, @"\D", "");

            if (int.TryParse(enteredText, out int timeInMinutes))
            {
                stepTimeMinutes = timeInMinutes;
                UpdateStepTimeText(false);
                SaveStepTime();
            }
        }

        private void UpdateStepTimeText(bool updateTimer = true)
        {
            string currentLang = LocalizationManager.CurrentCulture.Name;
            string suffix = (currentLang == "ru-RU") ? " мин" : " min";

            stepTime = $"{stepTimeMinutes}{suffix}";
            comboBoxTime.Text = stepTime;
            SaveStepTime();

            if (updateTimer)
                NotificationManager.UpdateTimerInterval();
        }


        private void ComboBoxTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxTime.SelectedItem != null)
            {
                string selectedText = (comboBoxTime.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                string numberOnly = Regex.Replace(selectedText, @"\D", "");

                if (int.TryParse(numberOnly, out int parsedMinutes))
                {
                    stepTimeMinutes = parsedMinutes;
                    UpdateTimeText();
                }
            }
        }
        
        private void SaveStepTime()
        {
            SettingsManager.UpdateSetting(stepTime, val => SettingsManager.Settings.StepTime = val);
            SettingsManager.UpdateSetting(stepTimeMinutes, val => SettingsManager.Settings.StepTimeMinutes = val);
        }







        private void SetReminder_Click(object sender, RoutedEventArgs e)
        {
            if (datePicker.SelectedDate == null || timePicker.Value == null)
            {
                MessageBox.Show("Выберите дату и время!");
                return;
            }

            TimeSpan timeWithoutSeconds = new TimeSpan(timePicker.Value.Value.Hour, timePicker.Value.Value.Minute, 0);
            DateTime reminderTime = datePicker.SelectedDate.Value.Date + timeWithoutSeconds;

            if (!SettingsManager.Settings.Reminders.Contains(reminderTime))
            {
                SettingsManager.Settings.Reminders.Add(reminderTime);
                SettingsManager.SaveSettings();
                UpdateReminderList();
            }
        }



        private void LoadReminders()
        {
            reminderList.ItemsSource = SettingsManager.Settings.Reminders;
        }


        public static void ReloadRemindersStatic()
        {
            instance?.LoadReminders();
        }

        private void UpdateReminderList()
        {
            reminderList.ItemsSource = null;
            reminderList.ItemsSource = SettingsManager.Settings.Reminders;
            SettingsManager.SaveSettings();
            reminderList.Items.Refresh();
        }
        private void ListBoxReminders_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && reminderList.SelectedItem is DateTime selectedReminder)
            {
                if (SettingsManager.Settings.Reminders.Remove(selectedReminder))
                {
                    // ObservableCollection
                    SettingsManager.SaveSettings();
                    UpdateReminderList();
                }
                else
                {
                    MessageBox.Show("Error Delete!");
                }

            }

        }


        public static void RemoveReminderStatic(DateTime reminderTime)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (SettingsManager.Settings.Reminders.Remove(reminderTime))
                {
                    instance?.UpdateReminderList();
                }
            });
        }





        private void DefaultText_Click(object sender, RoutedEventArgs e)
        {
            textNotificationBox.Clear();
            foreach (var notification in notificationsdefault)
            {
                textNotificationBox.AppendText(notification + ";" + Environment.NewLine);
            }
            notifications = new List<string>(notificationsdefault);
            SettingsManager.UpdateSetting(notifications, val => SettingsManager.Settings.Notifications = new List<string>(val));
        }

        private void ClearText_Click(object sender, RoutedEventArgs e)
        {
            textNotificationBox.Clear();
            notifications?.Clear();
            SettingsManager.UpdateSetting(notifications, val =>
                SettingsManager.Settings.Notifications = new List<string>(val ?? Enumerable.Empty<string>()));
        }

        private void textNotificationBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateNotifications(textNotificationBox.Text);
        }

        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int caretPos = textNotificationBox.CaretIndex;

                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    textNotificationBox.Text = textNotificationBox.Text.Insert(caretPos, Environment.NewLine);
                    textNotificationBox.CaretIndex = caretPos + Environment.NewLine.Length;
                }
                else
                {
                    textNotificationBox.Text = textNotificationBox.Text.TrimEnd();

                    if (!textNotificationBox.Text.EndsWith(";"))
                    {
                        textNotificationBox.Text += ";";
                    }

                    textNotificationBox.Text += Environment.NewLine;
                    textNotificationBox.CaretIndex = textNotificationBox.Text.Length;
                }

                e.Handled = true;
            }
        }


        private void UpdateNotifications(string text)
        {
            notifications?.Clear();
            notifications?.AddRange(text
                .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
            );

            SettingsManager.UpdateSetting(notifications, val =>
                SettingsManager.Settings.Notifications = new List<string>(val ?? Enumerable.Empty<string>()));

        }


        private void NotificatiosLoad()
        {
            var formattedNotifications = notifications?
                .Select(n => n.TrimEnd(';'))
                .Select(n => n + ";")        
                .ToList();

            textNotificationBox.Text = formattedNotifications != null
                    ? string.Join(Environment.NewLine, formattedNotifications)
                    : string.Empty;
        }



        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string oldText = stepTime;
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();

            if (comboBoxTime.Text != oldText)
            {
                UpdateTimeText();
                UpdateStepTimeText();
            }
        }

        private void LoadSettingsToFields()
        {
            switchNotification = SettingsManager.Settings.SwitchNotification;
            switchSound = SettingsManager.Settings.SwitchSound;
            switchText = SettingsManager.Settings.SwitchText;
            ToggleSwitchText();
            ToggleSwitchSound();
            NotificationOffOn(switchNotification);
            ToggleSwitchNotification(switchNotification);

            string fileName = SettingsManager.Settings.SelectedAudio;

            if (!string.IsNullOrEmpty(fileName))
            {
                string fullPath = System.IO.Path.Combine(soundFolderPath, fileName);

                if (File.Exists(fullPath))
                {
                    selectedAudio = fullPath;
                    SoundBox.SelectedItem = fileName;
                }
                else
                {
                    selectedAudio = "";
                }
            }
            stepTime = SettingsManager.Settings.StepTime;
            comboBoxTime.Text = stepTime.ToString();
            stepTimeMinutes = SettingsManager.Settings.StepTimeMinutes;
            notifications = SettingsManager.Settings.Notifications ?? new List<string>();
            reminders = SettingsManager.Settings.Reminders ?? new ObservableCollection<DateTime>();
            NotificatiosLoad();
            LoadReminders();
            //MessageBox.Show($"Settings file path: {SettingsManager.GetSettingsFilePath()}");
        }

        public void UpdateMarginBasedOnLanguage()
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            if (lang == "ru")
                StackPanelToggle.Margin = new Thickness(46, 0, 0, 0);
            else
                StackPanelToggle.Margin = new Thickness(140, 0, 0, 0);
        }
        private void Page3_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationManager.LanguageChanged -= UpdateMarginBasedOnLanguage;
        }
    }
}
