using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        bool createdNew;
        using (Mutex mutex = new Mutex(true, @"Local\GlobalScreenshotMenu", out createdNew))
        {
            if (!createdNew)
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (ScreenshotApplication application = new ScreenshotApplication())
            {
                Application.Run(application);
            }
        }
    }
}

internal sealed class ScreenshotApplication : ApplicationContext
{
    private const int WhMouseLl = 14;
    private const int WmRightButtonDown = 0x0204;
    private const int WmRightButtonUp = 0x0205;
    private const int VirtualKeyControl = 0x11;
    private const uint MonitorDefaultToNearest = 2;

    private readonly LowLevelMouseProc mouseProc;
    private readonly NotifyIcon trayIcon;
    private readonly ScreenshotPopup popup;
    private readonly System.Windows.Forms.Timer popupTimer;
    private IntPtr mouseHook;
    private bool suppressRightClick;
    private bool popupPending;
    private Point popupLocation;

    internal ScreenshotApplication()
    {
        ToolStripMenuItem captureItem = new ToolStripMenuItem("\u622A\u5C4F");
        captureItem.Click += delegate { StartScreenshot(); };

        ToolStripMenuItem exitItem = new ToolStripMenuItem("\u9000\u51FA");
        exitItem.Click += delegate { ExitThread(); };

        ContextMenuStrip trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add(new ToolStripMenuItem("\u622A\u5C4F", null, delegate { StartScreenshot(); }));
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitItem);

        Icon icon = SystemIcons.Application;
        string snippingTool = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\SnippingTool.exe");
        if (System.IO.File.Exists(snippingTool))
        {
            Icon extracted = Icon.ExtractAssociatedIcon(snippingTool);
            if (extracted != null)
            {
                icon = extracted;
            }
        }

        trayIcon = new NotifyIcon();
        trayIcon.Icon = icon;
        trayIcon.Text = "\u5168\u5C40\u622A\u5C4F\uFF1ACtrl + \u9F20\u6807\u53F3\u952E";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += delegate { StartScreenshot(); };

        popup = new ScreenshotPopup(StartScreenshot);
        popupTimer = new System.Windows.Forms.Timer();
        popupTimer.Interval = 30;
        popupTimer.Tick += delegate
        {
            if (!popupPending)
            {
                return;
            }

            popupPending = false;
            WriteLog("Ctrl+right-click detected");
            popup.ShowAt(popupLocation);
        };
        popupTimer.Start();

        mouseProc = MouseHookCallback;
        mouseHook = SetMouseHook(mouseProc);
        if (mouseHook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to install the global mouse hook.");
        }
        WriteLog("Global helper started");
    }

    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        using (Process process = Process.GetCurrentProcess())
        using (ProcessModule module = process.MainModule)
        {
            return SetWindowsHookEx(WhMouseLl, proc, GetModuleHandle(module.ModuleName), 0);
        }
    }

    private IntPtr MouseHookCallback(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            int mouseMessage = message.ToInt32();
            if (mouseMessage == WmRightButtonDown && IsControlPressed())
            {
                if (IsForegroundWindowFullScreen())
                {
                    suppressRightClick = false;
                    return CallNextHookEx(mouseHook, code, message, data);
                }

                suppressRightClick = true;
                return new IntPtr(1);
            }

            if (mouseMessage == WmRightButtonUp && suppressRightClick)
            {
                suppressRightClick = false;
                popupLocation = Cursor.Position;
                popupPending = true;
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(mouseHook, code, message, data);
    }

    private static bool IsControlPressed()
    {
        return (GetAsyncKeyState(VirtualKeyControl) & 0x8000) != 0;
    }

    private static bool IsForegroundWindowFullScreen()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return false;
        }

        NativeRect windowRect;
        if (!GetWindowRect(foregroundWindow, out windowRect))
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(foregroundWindow, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        NativeMonitorInfo monitorInfo = new NativeMonitorInfo();
        monitorInfo.Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return false;
        }

        NativeRect monitorRect = monitorInfo.Monitor;
        const int tolerance = 2;
        return windowRect.Left <= monitorRect.Left + tolerance
            && windowRect.Top <= monitorRect.Top + tolerance
            && windowRect.Right >= monitorRect.Right - tolerance
            && windowRect.Bottom >= monitorRect.Bottom - tolerance;
    }

    private static void StartScreenshot()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "ms-screenclip:") { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Screenshot", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void WriteLog(string message)
    {
        try
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GlobalScreenshotMenu.log");
            System.IO.File.AppendAllText(path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }
        catch
        {
        }
    }

    protected override void ExitThreadCore()
    {
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();
        popupTimer.Stop();
        popupTimer.Dispose();
        popup.Dispose();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }
        base.Dispose(disposing);
    }

    private delegate IntPtr LowLevelMouseProc(int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, LowLevelMouseProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string moduleName);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRect rectangle);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref NativeMonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }
}

internal sealed class ScreenshotPopup : Form
{
    private readonly Button captureButton;
    private readonly Action captureAction;

    internal ScreenshotPopup(Action action)
    {
        captureAction = action;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(35, 35, 35);
        ClientSize = new Size(116, 42);
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        captureButton = new Button();
        captureButton.BackColor = Color.FromArgb(45, 45, 45);
        captureButton.Dock = DockStyle.Fill;
        captureButton.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        captureButton.FlatStyle = FlatStyle.Flat;
        captureButton.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular);
        captureButton.ForeColor = Color.White;
        captureButton.Text = "\u622A\u5C4F";
        captureButton.Click += delegate
        {
            Hide();
            captureAction();
        };
        Controls.Add(captureButton);

        Deactivate += delegate { Hide(); };
        KeyPreview = true;
        KeyDown += delegate(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Escape)
            {
                Hide();
            }
        };
    }

    internal void ShowAt(Point cursorPosition)
    {
        Rectangle area = Screen.FromPoint(cursorPosition).WorkingArea;
        int x = Math.Min(cursorPosition.X, area.Right - Width);
        int y = Math.Min(cursorPosition.Y, area.Bottom - Height);
        Location = new Point(Math.Max(area.Left, x), Math.Max(area.Top, y));

        if (!Visible)
        {
            Show();
        }
        BringToFront();
        Activate();
        captureButton.Focus();
    }
}
