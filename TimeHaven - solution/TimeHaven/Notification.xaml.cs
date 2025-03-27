using System;
using System.Windows;

namespace TimeHaven
{
    public partial class Notification : Window
    {
        
        public Notification()
        {
            InitializeComponent();
        }

        private void CloseNotification(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void PauseTimer(object sender, RoutedEventArgs e)
        {
            NotificationManager.StopTimer();
            this.Hide();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int offsetX = 20; 
            int offsetY = 40; 

  
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            var windowWidth = this.ActualWidth;
            var windowHeight = this.ActualHeight;

            this.Left = screenWidth - windowWidth - offsetX;
            this.Top = screenHeight - windowHeight - offsetY;
        }

    }
}
