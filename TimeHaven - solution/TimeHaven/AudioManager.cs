using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace TimeHaven
{
    public static class AudioManager
    {
        private static readonly MediaPlayer mediaPlayer = new MediaPlayer();

        public static void PlaySelectedAudio()
        {
            if (string.IsNullOrEmpty(SettingsManager.Settings.SelectedAudio))
            {
                return;
            }

            string soundFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sound");
            string filePath = Path.Combine(soundFolderPath, SettingsManager.Settings.SelectedAudio);

            if (!File.Exists(filePath))
            {
                MessageBox.Show("No File", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                mediaPlayer.Open(new Uri(filePath));
                mediaPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ErrorSound: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
