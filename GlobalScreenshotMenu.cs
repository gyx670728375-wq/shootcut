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

    private readonly LowLevelMouseProc mouseProc;
    private readonly ContextMenuStrip screenshotMenu;
    private readonly NotifyIcon trayIcon;
    private IntPtr mouseHook;
    private bool suppressRightClick;

    internal ScreenshotApplication()
    {
        ToolStripMenuItem captureItem = new ToolStripMenuItem("\u622A\u5C4F");
        captureItem.Click += delegate { StartScreenshot(); };

        ToolStripMenuItem exitItem = new ToolStripMenuItem("\u9000\u51FA");
        exitItem.Click += delegate { ExitThread(); };

        screenshotMenu = new ContextMenuStrip();
        screenshotMenu.Items.Add(captureItem);

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

        mouseProc = MouseHookCallback;
        mouseHook = SetMouseHook(mouseProc);
        if (mouseHook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to install the global mouse hook.");
        }
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
                suppressRightClick = true;
                return new IntPtr(1);
            }

            if (mouseMessage == WmRightButtonUp && suppressRightClick)
            {
                suppressRightClick = false;
                screenshotMenu.Show(Cursor.Position);
                return new IntPtr(1);
            }
        }

        return CallNextHookEx(mouseHook, code, message, data);
    }

    private static bool IsControlPressed()
    {
        return (GetAsyncKeyState(VirtualKeyControl) & 0x8000) != 0;
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

    protected override void ExitThreadCore()
    {
        if (mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(mouseHook);
            mouseHook = IntPtr.Zero;
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();
        screenshotMenu.Dispose();
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
}
