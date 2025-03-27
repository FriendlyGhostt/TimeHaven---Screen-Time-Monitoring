using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;

namespace TimeHaven.ViewModels
{
    internal class ChartViewModel : IDisposable
    {
        private MainDatabaseManager maindbManager;
        public ChartViewModel()
        {
            maindbManager = MainDatabaseManager.Instance;
            maindbManager.DayUpdate += OnDataUpdated;
            maindbManager.HistoryUpdate += OnHistoryUpdated;

            ChartSeries = new SeriesCollection
            {
                new LineSeries { Title = "Activity:", Values = new ChartValues<int>() }
            };
            HourLabels = new ObservableCollection<string>();
            FormatAxisYLabels = value => $"{value} m";
            FormatAxisYLabelsHours = value => $"{value} h";

            WeekChartSeries = new SeriesCollection
            {
                new LineSeries { Title = "WeekActivity:", Values = new ChartValues<double>() }
            };
            WeekLabels = new ObservableCollection<string>();

            MonthChartSeries = new SeriesCollection
            {
                new LineSeries { Title = "MonthActivity:", Values = new ChartValues<double>() }
            };
            MonthLabels = new ObservableCollection<string>();

            _ = DelayedFirstUpdate().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Console.WriteLine(task.Exception);
                }
            }, TaskScheduler.Default);

        }

        public SeriesCollection ChartSeries { get; }
        public ObservableCollection<string> HourLabels { get; }
        public Func<double, string> FormatAxisYLabels { get; }
        public Func<double, string> FormatAxisYLabelsHours { get; }

        public SeriesCollection WeekChartSeries { get; }
        public ObservableCollection<string> WeekLabels { get; }

        public SeriesCollection MonthChartSeries { get; }
        public ObservableCollection<string> MonthLabels { get; }


        private void OnDataUpdated()
        {
            _ = UpdateChartAsync();
        }

        private void OnHistoryUpdated()
        {
            _ = UpdateHistoryChartsAsync();
        }

        private async Task DelayedFirstUpdate()
        {
            await Task.Delay(2000);
            _ = UpdateChartAsync();
            _ = UpdateHistoryChartsAsync();
        }

        public async Task UpdateChartAsync()
        {
            var last24HoursData = maindbManager.GetLast24HoursData();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                HourLabels.Clear();
                if (ChartSeries.Count > 0 && ChartSeries[0] is LineSeries lineSeries)
                {
                    var newValues = new ChartValues<int>();

                    foreach (var (hour, minutes) in last24HoursData)
                    {
                        HourLabels.Add($"{hour}:00");
                        newValues.Add(minutes);
                    }
                    lineSeries.Values = newValues;
                }
            });
        }

        public async Task UpdateHistoryChartsAsync()
        {

            var last7DaysData = maindbManager.GetLastNDaysData(7);
            var last30DaysData = maindbManager.GetLastNDaysData(30);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                WeekLabels.Clear();
                MonthLabels.Clear();


                if (WeekChartSeries.Count > 0 && WeekChartSeries[0] is LineSeries weekSeries)
                {
                    var newValues = new ChartValues<double>();

                    foreach (var (day, hours) in last7DaysData)
                    {
                        WeekLabels.Add(day);
                        newValues.Add(hours);
                    }
                    weekSeries.Values = newValues;
                }

                if (MonthChartSeries.Count > 0 && MonthChartSeries[0] is LineSeries monthSeries)
                {
                    var newValues = new ChartValues<double>();

                    foreach (var (day, hours) in last30DaysData)
                    {
                        MonthLabels.Add(day);
                        newValues.Add(hours);
                    }
                    monthSeries.Values = newValues;
                }
            });
        }

        public void ChartAction(bool check_visible)
        {
            if (check_visible)
            {
                var eventField = typeof(DatabaseManager)
                    .GetField("DayUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var eventDelegate = eventField?.GetValue(maindbManager) as Delegate;

                if (eventDelegate == null || !eventDelegate.GetInvocationList().Contains((Delegate)OnDataUpdated))
                {
                    maindbManager.DayUpdate += OnDataUpdated;
                }

                var eventField2 = typeof(DatabaseManager)
                    .GetField("HistoryUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var eventDelegate2 = eventField2?.GetValue(maindbManager) as Delegate;

                if (eventDelegate2 == null || !eventDelegate2.GetInvocationList().Contains((Delegate)OnHistoryUpdated))
                {
                    maindbManager.DayUpdate += OnHistoryUpdated;
                }

                OnDataUpdated();
                OnHistoryUpdated();
            }
            else
            {
                maindbManager.DayUpdate -= OnDataUpdated;
                maindbManager.HistoryUpdate -= OnHistoryUpdated;
            }
        }


        public void Dispose()
        {
            maindbManager.DayUpdate -= OnDataUpdated;
            maindbManager.HistoryUpdate -= OnHistoryUpdated;
        }
    }
}