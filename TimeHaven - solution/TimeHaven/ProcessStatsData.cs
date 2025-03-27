using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class ProcessStatsData
{
    public byte[] Icon { get; set; } = Array.Empty<byte>();
    public string Name { get; set; } = string.Empty;
    public int Today { get; set; }      
    public int Last7Days { get; set; }   
    public int Last30Days { get; set; }  
    public int Total { get; set; }       
    public string DisplayTime { get; set; } = string.Empty;

    public ImageSource IconImage
    {
        get
        {
            if (Icon == null || Icon.Length == 0)
                return new BitmapImage();

            BitmapImage image = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(Icon))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            return image;
        }
    }


    public string TodayFormatted => Today >= 3600
        ? $"{(Today / 3600)}:{(Today % 3600) / 60:D2}:{(Today % 60):D2}"
        : $"{(Today / 60):D2}:{(Today % 60):D2}";
    public string Last7DaysFormatted => (Math.Floor(Last7Days / 360.0) / 10).ToString("0.0");
    public string Last30DaysFormatted => (Math.Floor(Last30Days / 360.0) / 10).ToString("0.0");

    public string TotalFormatted => $"{(Total / 86400)}:{(Total % 86400) / 3600:D2}:{(Total % 3600) / 60:D2}";
}
