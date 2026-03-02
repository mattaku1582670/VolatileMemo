using System;
using System.Drawing;
using System.IO;
using Forms = System.Windows.Forms;

namespace VolatileMemoHUD.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _disposed;

    public event EventHandler? OpenRequested;
    public event EventHandler? ClearRequested;
    public event EventHandler? ExitRequested;

    public TrayService(string appDirectory)
    {
        var openItem = new Forms.ToolStripMenuItem("開く（表示）");
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);

        var clearItem = new Forms.ToolStripMenuItem("クリア");
        clearItem.Click += (_, _) => ClearRequested?.Invoke(this, EventArgs.Empty);

        var exitItem = new Forms.ToolStripMenuItem("終了");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(clearItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(appDirectory),
            Text = "VolatileMemoHUD",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowBalloon(string title, string text, Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(2500);
        }
        catch
        {
            // Ignore tray notification failures.
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private static Icon LoadIcon(string appDirectory)
    {
        var iconPath = Path.Combine(appDirectory, "AppIcon.ico");
        try
        {
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // Fall through to system icon.
        }

        return SystemIcons.Application;
    }
}
