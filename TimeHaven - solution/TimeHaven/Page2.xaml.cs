using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Linq;

namespace TimeHaven
{
    public partial class Page2 : UserControl
    {
        private DatabaseManager dbManager;
        private string currentSortMode = "today";

        public Page2()
        {
            InitializeComponent();

            dbManager = DatabaseManager.Instance;
            dbManager.DatabaseUpdated += OnDatabaseUpdated;

            currentSortMode = "today";
            OnDatabaseUpdated();

            DelayedUpdate();
        }

        private void OnDatabaseUpdated()
        {
            _ = UpdateTableAsync();
        }

        private async Task UpdateTableAsync()
        {
            await Dispatcher.InvokeAsync(() =>
            {
                List<ProcessStatsData> data = dbManager.GetAllProcessStats();
                SortData(currentSortMode); 
            }, DispatcherPriority.Background);
        }

        private void SortData(string mode)
        {
            currentSortMode = mode;
            List<ProcessStatsData> data = dbManager.GetAllProcessStats();

            switch (mode)
            {
                case "today":
                    data = data.OrderByDescending(x => x.Today).ToList();
                    break;
                case "7days":
                    data = data.OrderByDescending(x => x.Last7Days).ToList();
                    break;
                case "30days":
                    data = data.OrderByDescending(x => x.Last30Days).ToList();
                    break;
                case "all":
                    data = data.OrderByDescending(x => x.Total).ToList();
                    break;
            }

            _ = SortAndDisplayData(data, mode);
            UpdateButtonStyles(currentSortMode);
        }


        private async Task SortAndDisplayData(List<ProcessStatsData> data, string mode)
        {
            foreach (var item in data)
            {
                switch (mode)
                {
                    case "today":
                        item.DisplayTime = FormatTime(item.Today);
                        break;
                    case "7days":
                        item.DisplayTime = $"{(item.Last7Days / 3600.0):0.0}ч";
                        break;
                    case "30days":
                        item.DisplayTime = $"{(item.Last30Days / 3600.0):0.0}ч";
                        break;
                    case "all":
                        item.DisplayTime = FormatTotalTime(item.Total);
                        break;
                }
            }

            await Dispatcher.InvokeAsync(() =>
            {
                ProcessDataGrid.ItemsSource = null;
                ProcessDataGrid.Items.Clear();
                ProcessDataGrid.ItemsSource = data;
            }, DispatcherPriority.Background);
        }



        private string FormatTime(int minutes)
        {
            return $"{minutes / 60:D2}:{minutes % 60:D2}";
        }

        private string FormatTotalTime(int minutes)
        {
            int days = minutes / 1440;
            int hours = (minutes % 1440) / 60;
            int mins = minutes % 60;
            return $"{days}д {hours:D2}ч {mins:D2}м";
        }

        private void UpdateButtonStyles(string activeMode)
        {
            Dispatcher.Invoke(() =>
            {
                SolidColorBrush defaultBrush = new SolidColorBrush(Colors.Transparent);
                SolidColorBrush activeBrush = new SolidColorBrush(Colors.DodgerBlue);

                BtnToday.Background = activeMode == "today" ? activeBrush : defaultBrush;
                Btn7Days.Background = activeMode == "7days" ? activeBrush : defaultBrush;
                Btn30Days.Background = activeMode == "30days" ? activeBrush : defaultBrush;
                BtnAllTime.Background = activeMode == "all" ? activeBrush : defaultBrush;
            });
        }


        private void BtnToday_Click(object sender, RoutedEventArgs e) => SortData("today");
        private void Btn7Days_Click(object sender, RoutedEventArgs e) => SortData("7days");
        private void Btn30Days_Click(object sender, RoutedEventArgs e) => SortData("30days");
        private void BtnAllTime_Click(object sender, RoutedEventArgs e) => SortData("all");

        private async void DelayedUpdate()
        {
            await Task.Delay(5000);

            if (currentSortMode == "today")
            {
                OnDatabaseUpdated();
            }
        }
        public void Page2Action(bool check_visible)
        {
            if (check_visible)
            {
                var eventField = typeof(DatabaseManager)
                    .GetField("DatabaseUpdated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var eventDelegate = eventField?.GetValue(dbManager) as Delegate;

                if (eventDelegate == null || !eventDelegate.GetInvocationList().Contains((Delegate)OnDatabaseUpdated))
                {
                    dbManager.DatabaseUpdated += OnDatabaseUpdated;
                }
                OnDatabaseUpdated();
            }
            else
            {
                dbManager.DatabaseUpdated -= OnDatabaseUpdated;
            }
        }

    }
}
