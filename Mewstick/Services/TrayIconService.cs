using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Mewstick.Services;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? ShowMainWindowRequested;
    public event Action? QuickAddRequested;
    public event Action? ExitRequested;
    public event Action? HelpRequested;

    public TrayIconService()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mở Mewstick", null, (_, _) => ShowMainWindowRequested?.Invoke());
        menu.Items.Add("Thêm nhanh (Ctrl+Alt+N)", null, (_, _) => QuickAddRequested?.Invoke());
        menu.Items.Add("Hướng dẫn", null, (_, _) => HelpRequested?.Invoke());

        var startupItem = new ToolStripMenuItem("Khởi động cùng Windows")
        {
            Checked = StartupService.IsEnabled(),
            CheckOnClick = true
        };
        startupItem.CheckedChanged += (_, _) => StartupService.SetEnabled(startupItem.Checked);
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Thoát", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Resource", "app.ico")),
            Text = "Mewstick",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => ShowMainWindowRequested?.Invoke();
        _notifyIcon.BalloonTipClicked += (_, _) => ShowMainWindowRequested?.Invoke();
    }

    public void ShowReminder(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        GC.SuppressFinalize(this);
    }
}
