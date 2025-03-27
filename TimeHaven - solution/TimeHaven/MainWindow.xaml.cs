using System.IO;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace TimeHaven
{
    public partial class MainWindow : Window
    {
        private static MainWindow ?instance;

        private NotifyIcon notifyIcon;
        private TimeSpan timeElapsed;
        private TimeSpan dbElapsedTime;
        private Button havenButton;
        private Button statisticsButton;
        private Button notificationButton;
        private Button settingsButton;
        private ContentControl mainContent;

        private ToolStripMenuItem  pauseMenuItem;
        private ToolStripMenuItem startMenuItem;

        private Page1 page1 = new Page1();
        private Page2 page2 = new Page2();
        private System.Windows.Controls.UserControl page3 = new Page3();
        private System.Windows.Controls.UserControl page4 = new Page4();

        private DatabaseManager dbManager;
        private ActiveWindowTracker trackerWindow;


        private System.Timers.Timer timer;
        private DateTime startTime;

        private System.Timers.Timer dbUpdateTimer;
        private DateTime lastDbUpdateTime;

        private bool isPaused = false;
        private DateTime pauseStartTime;
        private TimeSpan pausedDuration = TimeSpan.Zero;

        private MainDatabaseManager maindbManager;

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();


        public MainWindow()
        {
            InitializeComponent();
            WindowScaler.ScaleWindow(this);
            InitializeTimer();
            InitializeDbUpdateTimer();
            instance = this;

            statisticsButton = new Button();
            settingsButton = new Button();
            notificationButton = new Button();
            havenButton = new Button();
            mainContent = new ContentControl();

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "Icon.ico");
            notifyIcon = new NotifyIcon
            {
                Icon = new System.Drawing.Icon(iconPath),
                Visible = true,
                Text = "TimeHaven"
            };

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            var showMenuItem = new ToolStripMenuItem("Show");
            pauseMenuItem = new ToolStripMenuItem("Pause");
            startMenuItem = new ToolStripMenuItem("Start");
            var closeMenuItem = new ToolStripMenuItem("Close");

            showMenuItem.Click += (s, e) => ShowWindow();
            pauseMenuItem.Click += (s, e) =>
            {
                PauseTime();
                NotificationManager.StopTimer();
            };
            startMenuItem.Click += (s, e) =>
            {
                StartTime();
                NotificationManager.UpdateTimerInterval();
            };
            startMenuItem.Enabled = false;
            closeMenuItem.Click += (s, e) => ExitApplication();


            contextMenu.Items.Add(showMenuItem);
            contextMenu.Items.Add(pauseMenuItem);
            contextMenu.Items.Add(startMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(closeMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;

            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            this.Loaded += MainWindow_Loaded;


            dbManager = DatabaseManager.Instance;
            maindbManager = MainDatabaseManager.Instance;
            dbManager.ResetDatabase();            ///////////////////////////////////
            dbManager.InsertTestData();          ///////////////////////////////////
            trackerWindow = new ActiveWindowTracker();
            trackerWindow.StartTracking();

            NotificationManager.OnPaused += PauseTime;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            havenButton = this.Template.FindName("HavenButton", this) as Button ?? havenButton;
            statisticsButton = this.Template.FindName("StatisticsButton", this) as Button ?? statisticsButton;
            notificationButton = this.Template.FindName("NotificationButton", this) as Button ?? notificationButton;
            settingsButton = this.Template.FindName("SettingsButton", this) as Button ?? settingsButton;

            mainContent = this.Template.FindName("MainContent", this) as ContentControl
                ?? throw new NullReferenceException("MainContent not found.");

            LoadPage(page1);
        }




        private void LoadPage(System.Windows.Controls.UserControl page)
        {
            mainContent.Content = page;
        }


        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            ResetTabs();
            havenButton.Tag = "Active";
            LoadPage(page1);
            Window_IsVisibleChanged();
        }

        private void ExitApplication()
        {
            timer.Stop();
            dbUpdateTimer.Stop();
            timeElapsed = TimeSpan.Zero;
            dbElapsedTime = TimeSpan.Zero;
            trackerWindow?.StopTracking();
            notifyIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void OnClosing(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Window_IsVisibleChanged();
        }

        private void InitializeTimer()
        {
            startTime = DateTime.Now;
            timer = new System.Timers.Timer(1000);
            if (timer != null)
            {
                timer.Elapsed += Timer_Tick!;
                timer.AutoReset = true;
                timer.Start();
            }
        }

        private void InitializeDbUpdateTimer()
        {
            lastDbUpdateTime = DateTime.Now;
            dbUpdateTimer = new System.Timers.Timer(60000);
            if (dbUpdateTimer != null)
            {
                dbUpdateTimer!.Elapsed += DbUpdateTimer_Tick!;
                dbUpdateTimer.AutoReset = true;
                dbUpdateTimer.Start();
            }
            
        }

        private void Timer_Tick(object sender, ElapsedEventArgs e)
        {
            if (isPaused) return;

            TimeSpan timeElapsed = DateTime.Now - startTime - pausedDuration; 

            string formattedTime;
            if (timeElapsed.Days == 0)
            {
                formattedTime = $"{timeElapsed.Hours:D2}:{timeElapsed.Minutes:D2}:{timeElapsed.Seconds:D2}";
            }
            else
            {
                formattedTime = $"{timeElapsed.Days}:{timeElapsed.Hours:D2}:{timeElapsed.Minutes:D2}:{timeElapsed.Seconds:D2}";
            }

            Dispatcher.Invoke(() =>
            {
                var timerBlock = this.Template.FindName("TimerBlock", this) as TextBlock;
                if (timerBlock != null)
                {
                    timerBlock.Text = formattedTime;
                }
                notifyIcon.Text = $"TimeHaven - {formattedTime}";
            });
        }

        private void DbUpdateTimer_Tick(object sender, ElapsedEventArgs e)
        {
            if (isPaused) return;

            dbElapsedTime = dbElapsedTime.Add(TimeSpan.FromMinutes(1));
            maindbManager.IncrementDayStats();


            if (dbElapsedTime.TotalMinutes >= 6)
            {
                maindbManager.IncrementHistoryStats();
                dbElapsedTime = TimeSpan.Zero;
            }
        }

        private void StartTime()
        {
            if (!isPaused) return;

            isPaused = false;
            pausedDuration += DateTime.Now - pauseStartTime;

            timer?.Start();
            dbUpdateTimer?.Start();
            startMenuItem.Enabled = false;
            pauseMenuItem.Enabled = true;
            trackerWindow?.StartTracking();
        }

        private void PauseTime()
        {
            if (isPaused) return;

            isPaused = true;
            pauseStartTime = DateTime.Now;

            timer?.Stop();
            dbUpdateTimer?.Stop();
            startMenuItem.Enabled = true;
            pauseMenuItem.Enabled = false;
            trackerWindow?.StopTracking();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void OnHavenClicked(object sender, RoutedEventArgs e)
        {
            ShowContent("Haven");
            ResetTabs();
            havenButton.Tag = "Active";
            LoadPage(page1);
        }

        private void OnStatisticsClicked(object sender, RoutedEventArgs e)
        {
            ShowContent("Statistics");
            ResetTabs();
            statisticsButton.Tag = "Active";
            LoadPage(page2);
        }

        private void OnNotificationClicked(object sender, RoutedEventArgs e)
        {
            ShowContent("Notification");
            ResetTabs();
            notificationButton.Tag = "Active";
            LoadPage(page3);
        }

        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            ShowContent("Settings");
            ResetTabs();
            settingsButton.Tag = "Active";
            LoadPage(page4);
        }

        private void ShowContent(string content)
        {
        }

        private void ResetTabs()
        {
            havenButton.Tag = null;
            statisticsButton.Tag = null;
            notificationButton.Tag = null;
            settingsButton.Tag = null;
        }

        private void Window_IsVisibleChanged()
        {
            if (!this.IsVisible)
            {
                page2.Page2Action(false);
                page1.Page1Action(false);
            }
            else if(this.IsVisible)
            {
                page2.Page2Action(true);
                page1.Page1Action(true);
            }
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FocusManager.SetFocusedElement(this, null);
                Keyboard.ClearFocus();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        //TO DO: Перенести таймеры в отдельный класс
    }
}
