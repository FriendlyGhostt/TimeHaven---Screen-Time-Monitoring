using System;
using System.Windows;

public static class WindowScaler
{
    private const int BaseWidth = 1100;
    private const int BaseHeight = 760;
    private const int ReferenceWidth = 1920;
    private const int ReferenceHeight = 1080;

    public static void ScaleWindow(Window window)
    {
        window.Loaded += (sender, args) =>
        {
            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            if (screenWidth == ReferenceWidth && screenHeight == ReferenceHeight)
            {
                return;
            }

            float scaleX = (float)screenWidth / ReferenceWidth;
            float scaleY = (float)screenHeight / ReferenceHeight;
            float scale = Math.Min(scaleX, scaleY);

            double newWidth = Math.Max(BaseWidth * scale, window.MinWidth);
            double newHeight = Math.Max(BaseHeight * scale, window.MinHeight);

            window.Width = newWidth;
            window.Height = newHeight;
            window.Left = (screenWidth - newWidth) / 2;
            window.Top = (screenHeight - newHeight) / 2;
        };
    }
}
