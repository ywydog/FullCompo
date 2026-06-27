using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;

namespace FullCompo.App.Helpers;

public sealed class GlobalHotkeyHelper : IDisposable
{
    private readonly Action _action;
    private readonly string _shortcut;
    private readonly int _hotkeyId = 1;

    private IntPtr _hwnd = IntPtr.Zero;
    private IntPtr _hInstance = IntPtr.Zero;
    private string? _className;
    private WndProcDelegate? _wndProc;
    private Task? _messageLoopTask;
    private uint _threadId;
    private readonly ManualResetEventSlim _ready = new();
    private bool _disposed;

    public GlobalHotkeyHelper(string shortcut, Action action)
    {
        _shortcut = shortcut;
        _action = action;

        if (!OperatingSystem.IsWindows())
        {
            _ready.Set();
            return;
        }

        _messageLoopTask = Task.Run(RunMessageLoop);
        _ready.Wait();
    }

    private void RunMessageLoop()
    {
        try
        {
            _threadId = GetCurrentThreadId();
            _hInstance = GetModuleHandle(null);
            _className = $"FullCompoHotkeyWnd-{Guid.NewGuid():N}";
            _wndProc = WndProc;

            var wcex = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
                hInstance = _hInstance,
                lpszClassName = _className
            };

            RegisterClassEx(ref wcex);

            _hwnd = CreateWindowEx(
                WS_EX_NOACTIVATE,
                _className,
                "FullCompo Hotkey Window",
                WS_OVERLAPPED,
                0, 0, 0, 0,
                new IntPtr(HWND_MESSAGE),
                IntPtr.Zero,
                _hInstance,
                IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                return;
            }

            if (TryParseShortcut(_shortcut, out var modifiers, out var vk))
            {
                RegisterHotKey(_hwnd, _hotkeyId, modifiers, vk);
            }

            while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        finally
        {
            _ready.Set();
        }
    }

    private IntPtr WndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam)
    {
        if (uMsg == WM_HOTKEY && wParam.ToInt32() == _hotkeyId)
        {
            _action?.Invoke();
            return IntPtr.Zero;
        }

        if (uMsg == WM_CLOSE)
        {
            UnregisterHotKey(hWnd, _hotkeyId);
            DestroyWindow(hWnd);
            return IntPtr.Zero;
        }

        if (uMsg == WM_DESTROY)
        {
            PostQuitMessage(0);
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, uMsg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (!OperatingSystem.IsWindows() || _messageLoopTask == null)
        {
            _ready.Dispose();
            return;
        }

        // Ask the background thread to clean up its own window.
        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        // Fallback: force the message loop to exit if the window is gone.
        if (_threadId != 0)
        {
            PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }

        try
        {
            _messageLoopTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignore
        }

        if (_className != null && _hInstance != IntPtr.Zero)
        {
            UnregisterClass(_className, _hInstance);
        }

        _ready.Dispose();
    }

    private static bool TryParseShortcut(string shortcut, out uint modifiers, out uint vk)
    {
        modifiers = 0;
        vk = 0;

        if (string.IsNullOrWhiteSpace(shortcut))
            return false;

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string? keyPart = null;

        foreach (var part in parts)
        {
            var p = part.ToLowerInvariant();
            switch (p)
            {
                case "ctrl":
                case "control":
                    modifiers |= MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= MOD_ALT;
                    break;
                case "shift":
                    modifiers |= MOD_SHIFT;
                    break;
                case "win":
                case "windows":
                case "cmd":
                case "command":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    keyPart = part;
                    break;
            }
        }

        if (string.IsNullOrEmpty(keyPart))
            return false;

        vk = VirtualKeyFromString(keyPart);
        return vk != 0;
    }

    private static uint VirtualKeyFromString(string key)
    {
        if (Enum.TryParse<Key>(key, true, out var avaloniaKey))
        {
            return VirtualKeyFromAvaloniaKey(avaloniaKey);
        }

        return key.ToUpperInvariant() switch
        {
            "A" => 0x41,
            "B" => 0x42,
            "C" => 0x43,
            "D" => 0x44,
            "E" => 0x45,
            "F" => 0x46,
            "G" => 0x47,
            "H" => 0x48,
            "I" => 0x49,
            "J" => 0x4A,
            "K" => 0x4B,
            "L" => 0x4C,
            "M" => 0x4D,
            "N" => 0x4E,
            "O" => 0x4F,
            "P" => 0x50,
            "Q" => 0x51,
            "R" => 0x52,
            "S" => 0x53,
            "T" => 0x54,
            "U" => 0x55,
            "V" => 0x56,
            "W" => 0x57,
            "X" => 0x58,
            "Y" => 0x59,
            "Z" => 0x5A,
            "0" => 0x30,
            "1" => 0x31,
            "2" => 0x32,
            "3" => 0x33,
            "4" => 0x34,
            "5" => 0x35,
            "6" => 0x36,
            "7" => 0x37,
            "8" => 0x38,
            "9" => 0x39,
            _ => 0
        };
    }

    private static uint VirtualKeyFromAvaloniaKey(Key key)
    {
        return key switch
        {
            >= Key.A and <= Key.Z => (uint)(key - Key.A + 0x41),
            >= Key.D0 and <= Key.D9 => (uint)(key - Key.D0 + 0x30),
            >= Key.F1 and <= Key.F12 => (uint)(key - Key.F1 + 0x70),
            Key.Space => 0x20,
            Key.Return => 0x0D,
            Key.Escape => 0x1B,
            Key.Tab => 0x09,
            Key.Back => 0x08,
            Key.Delete => 0x2E,
            Key.Insert => 0x2D,
            Key.Home => 0x24,
            Key.End => 0x23,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.Left => 0x25,
            Key.Up => 0x26,
            Key.Right => 0x27,
            Key.Down => 0x28,
            _ => 0
        };
    }

    #region Win32

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    private const uint WM_HOTKEY = 0x0312;
    private const uint WM_CLOSE = 0x0010;
    private const uint WM_DESTROY = 0x0002;
    private const uint WM_QUIT = 0x0012;
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const uint WS_OVERLAPPED = 0x00000000;
    private const int HWND_MESSAGE = -3;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public uint cbSize;
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern void PostQuitMessage(int nExitCode);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    #endregion
}
