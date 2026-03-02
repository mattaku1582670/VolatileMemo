using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using VolatileMemoHUD.Services;
using Forms = System.Windows.Forms;

namespace VolatileMemoHUD;

public partial class MainWindow : Window
{
    private readonly TrayService _trayService;
    private HotKeyService? _hotKeyService;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _trayService = new TrayService(AppContext.BaseDirectory);
        _trayService.OpenRequested += (_, _) => ShowHudNearCursor();
        _trayService.ClearRequested += (_, _) => ClearMemo();
        _trayService.ExitRequested += (_, _) => ExitApplication();

        Loaded += (_, _) => Hide();
        SourceInitialized += OnSourceInitialized;
        KeyDown += OnKeyDown;
        Closing += OnClosing;

        EnsureWindowHandleCreated();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (_hotKeyService is not null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(this).Handle;
        _hotKeyService = new HotKeyService(hwnd);
        _hotKeyService.HotKeyPressed += (_, _) => ToggleHud();
        _hotKeyService.WarningRaised += (_, message) =>
        {
            _trayService.ShowBalloon("VolatileMemoHUD", message, Forms.ToolTipIcon.Warning);
        };
        _hotKeyService.Register();
    }

    private void EnsureWindowHandleCreated()
    {
        var interop = new WindowInteropHelper(this);
        if (interop.Handle == IntPtr.Zero)
        {
            interop.EnsureHandle();
        }
    }

    private void ToggleHud()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
            ShowHudNearCursor();
            return;
        }

        if (IsVisible)
        {
            DiscardAndHide();
            return;
        }

        ShowHudNearCursor();
    }

    private void ShowHudNearCursor()
    {
        PositionNearCursor();

        if (!IsVisible)
        {
            Show();
        }

        Activate();
        MemoTextBox.Focus();
        MemoTextBox.CaretIndex = MemoTextBox.Text.Length;
    }

    private void PositionNearCursor()
    {
        const double offset = 16;

        if (!GetCursorPos(out var point))
        {
            point = new POINT { X = 100, Y = 100 };
        }

        var width = Math.Max(ActualWidth, Width);
        var height = Math.Max(ActualHeight, Height);

        var left = point.X + offset;
        var top = point.Y + offset;

        var minX = SystemParameters.VirtualScreenLeft;
        var minY = SystemParameters.VirtualScreenTop;
        var maxX = minX + SystemParameters.VirtualScreenWidth - width;
        var maxY = minY + SystemParameters.VirtualScreenHeight - height;

        Left = Clamp(left, minX, maxX);
        Top = Clamp(top, minY, maxY);
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            DiscardAndHide();
            return;
        }

        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            e.Handled = true;
            CopyAndHide();
        }
    }

    private void CopyAndHide()
    {
        try
        {
            System.Windows.Clipboard.SetText(MemoTextBox.Text ?? string.Empty);
        }
        catch (Exception ex)
        {
            _trayService.ShowBalloon("VolatileMemoHUD", $"コピーに失敗しました: {ex.Message}", Forms.ToolTipIcon.Warning);
        }

        DiscardAndHide();
    }

    private void DiscardAndHide()
    {
        ClearMemo();
        Hide();
    }

    private void ClearMemo()
    {
        MemoTextBox.Clear();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_isExiting)
        {
            return;
        }

        e.Cancel = true;
        DiscardAndHide();
    }

    private void ExitApplication()
    {
        _isExiting = true;
        _hotKeyService?.Dispose();
        _trayService.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _hotKeyService?.Dispose();
        _trayService.Dispose();
        base.OnClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }
}
