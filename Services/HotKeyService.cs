using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VolatileMemoHUD.Services;

public sealed class HotKeyService : IDisposable
{
    private const int WmHotKey = 0x0312;

    private const int HotKeyIdAltOem5 = 0x1001;
    private const int HotKeyIdAltOem102 = 0x1002;
    private const int HotKeyIdAltShiftOem5 = 0x1003;
    private const int HotKeyIdAltShiftOem102 = 0x1004;

    private const uint ModAlt = 0x0001;
    private const uint ModShift = 0x0004;

    // VK_OEM_5: "\" key on many JP/US layouts.
    private const uint VkOem5 = 0xDC;
    // VK_OEM_102: "< > |" or "\" related key on some keyboard layouts.
    private const uint VkOem102 = 0xE2;

    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly List<int> _registeredIds = new();
    private bool _warningRaised;

    public event EventHandler? HotKeyPressed;
    public event EventHandler<string>? WarningRaised;

    public HotKeyService(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd) ?? throw new InvalidOperationException("Failed to create HwndSource.");
        _source.AddHook(WndProc);
    }

    public bool Register()
    {
        Unregister();

        var pass1Succeeded = false;
        pass1Succeeded |= TryRegisterAndStore(HotKeyIdAltOem5, ModAlt, VkOem5);
        pass1Succeeded |= TryRegisterAndStore(HotKeyIdAltOem102, ModAlt, VkOem102);

        if (pass1Succeeded)
        {
            return true;
        }

        var pass2Succeeded = false;
        pass2Succeeded |= TryRegisterAndStore(HotKeyIdAltShiftOem5, ModAlt | ModShift, VkOem5);
        pass2Succeeded |= TryRegisterAndStore(HotKeyIdAltShiftOem102, ModAlt | ModShift, VkOem102);

        if (pass2Succeeded)
        {
            RaiseWarningOnce(@"Alt+\ の登録に失敗したため Alt+Shift+\ を使用します。");
            return true;
        }

        RaiseWarningOnce(@"グローバルホットキーを登録できませんでした。トレイメニューから開いてください。");
        return false;
    }

    public void Unregister()
    {
        foreach (var id in _registeredIds)
        {
            try
            {
                UnregisterHotKey(_hwnd, id);
            }
            catch
            {
                // Ignore unregister failures.
            }
        }

        _registeredIds.Clear();
    }

    public void Dispose()
    {
        Unregister();
        _source.RemoveHook(WndProc);
    }

    private bool TryRegisterAndStore(int id, uint modifiers, uint vk)
    {
        try
        {
            if (RegisterHotKey(_hwnd, id, modifiers, vk))
            {
                _registeredIds.Add(id);
                return true;
            }
        }
        catch
        {
            // Ignore registration exception and try fallback keys.
        }

        return false;
    }

    private void RaiseWarningOnce(string message)
    {
        if (_warningRaised)
        {
            return;
        }

        _warningRaised = true;
        WarningRaised?.Invoke(this, message);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && _registeredIds.Contains(wParam.ToInt32()))
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
