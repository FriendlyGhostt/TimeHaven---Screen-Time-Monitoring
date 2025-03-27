using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class ActiveWindowTracker
{
    private DatabaseManager _dbManager;
    private Thread? _trackingThread;
    private CancellationTokenSource? _cts;

    public ActiveWindowTracker()
    {
        _dbManager = DatabaseManager.Instance;
    }

    public void StartTracking()
    {
        if (_trackingThread == null || !_trackingThread.IsAlive)
        {
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _trackingThread = new Thread(() => TrackActiveWindow(token))
            {
                IsBackground = true
            };
            _trackingThread.Start();
        }
    }

    private void TrackActiveWindow(CancellationToken token)
    {
        string lastProcessName = "";

        while (!token.IsCancellationRequested)
        {
            string processName = GetActiveProcessName();
            if (!string.IsNullOrEmpty(processName) && processName != lastProcessName)
            {
                byte[] iconBytes = GetProcessIcon(processName);
                _dbManager.AddProcessIfNotExists(processName, iconBytes);
                lastProcessName = processName;
            }

            _dbManager.EnsureDailyEntry(processName);
            _dbManager.IncrementTime(processName);

            try
            {
                token.ThrowIfCancellationRequested();
                Thread.Sleep(1000);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void StopTracking()
    {
        _cts?.Cancel();
        _trackingThread?.Join();
        _trackingThread = null;
        _cts = null;
    }

    private string GetActiveProcessName()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return string.Empty;

        GetWindowThreadProcessId(hwnd, out uint pid);
        Process proc = Process.GetProcessById((int)pid);
        return proc.ProcessName;
    }

    private byte[] GetProcessIcon(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return Array.Empty<byte>();

            string? fileName = processes[0].MainModule?.FileName;
            if (string.IsNullOrEmpty(fileName)) return Array.Empty<byte>();

            using (Icon? icon = Icon.ExtractAssociatedIcon(fileName))
            {
                if (icon == null) return Array.Empty<byte>();

                using (Bitmap bmp = icon.ToBitmap())
                {
                    return ImageToByteArray(bmp);
                }
            }
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private byte[] ImageToByteArray(Image img)
    {
        using (var ms = new System.IO.MemoryStream())
        {
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
