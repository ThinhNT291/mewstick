using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Mewstick.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

/// <summary>
/// Đăng ký một phím tắt toàn cục qua Win32 API. Mỗi instance dùng 1 ID riêng
/// (truyền vào constructor) để có thể đăng ký nhiều hotkey độc lập trong cùng app.
/// </summary>
public class HotkeyService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private readonly int _hotkeyId;

    private HwndSource? _source;
    private IntPtr _handle = IntPtr.Zero;

    public event Action? HotkeyPressed;

    public HotkeyService(int hotkeyId)
    {
        _hotkeyId = hotkeyId;
    }

    public bool Register(Window window, HotkeyModifiers modifiers, uint virtualKey)
    {
        _handle = new WindowInteropHelper(window).Handle;
        if (_handle == IntPtr.Zero)
            _handle = new WindowInteropHelper(window).EnsureHandle();

        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(HwndHook);

        return RegisterHotKey(_handle, _hotkeyId, (uint)modifiers, virtualKey);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
            UnregisterHotKey(_handle, _hotkeyId);
        _source?.RemoveHook(HwndHook);
        GC.SuppressFinalize(this);
    }
}