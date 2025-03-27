using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Windows.UI.Notifications;
using System.Collections.ObjectModel;

namespace TimeHaven
{
    public class AppSettings
    {
        public string Language { get; set; } = "en-US";
        public string StepTime { get; set; } = "20 min";
        public int StepTimeMinutes { get; set; } = 20;
        public bool SwitchNotification { get; set; } = true;
        public bool SwitchSound { get; set; } = true;
        public bool SwitchText { get; set; } = true;
        public bool RunOnStartup { get; set; } = true;
        public bool BackupEnabled { get; set; } = false;
        public string BackupDirectory { get; set; } = "";
        public DateTime LastBackupTime { get; set; } = DateTime.MinValue;
        public string SelectedAudio { get; set; } = "";
        private List<string> _notifications = new List<string>();
        public List<string> Notifications
        {
            get => _notifications;
            set => _notifications = value ?? new List<string>();
        }
        public ObservableCollection<DateTime> Reminders { get; set; } = new ObservableCollection<DateTime>();

        public static string GetSystemLanguage()
        {
            return Thread.CurrentThread.CurrentUICulture.Name;
        }
    }

}
