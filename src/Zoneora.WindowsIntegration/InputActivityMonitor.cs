using System.Runtime.InteropServices;
using Zoneora.Services;

namespace Zoneora.WindowsIntegration;

public sealed class InputActivityMonitor : IActivityMonitor, IDisposable
{
    private const int WhMouseLl = 14;
    private const int WhKeyboardLl = 13;
    private const uint WmMouseMove = 0x0200;
    private const uint WmLButtonDown = 0x0201;
    private const uint WmRButtonDown = 0x0204;
    private const uint WmMButtonDown = 0x0207;
    private const uint WmQuit = 0x0012;
    private readonly Thread hookThread;
    private readonly ManualResetEventSlim ready = new(false);
    private readonly HookDelegate mouseCallback;
    private readonly HookDelegate keyboardCallback;
    private uint threadId;
    private nint mouseHook;
    private nint keyboardHook;
    private bool disposed;

    public InputActivityMonitor()
    {
        mouseCallback = MouseHookCallback;
        keyboardCallback = KeyboardHookCallback;
        hookThread = new Thread(RunHookThread)
        {
            IsBackground = true,
            Name = "Zoneora input monitor"
        };
        hookThread.Start();
        ready.Wait();
    }

    public event EventHandler<ActivityKind>? ActivityDetected;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (threadId != 0)
        {
            PostThreadMessage(threadId, WmQuit, 0, 0);
        }
        hookThread.Join(TimeSpan.FromSeconds(1));
        ready.Dispose();
    }

    private void RunHookThread()
    {
        threadId = GetCurrentThreadId();
        using SafeHookHandle? mouse = InstallHook(WhMouseLl, mouseCallback);
        using SafeHookHandle? keyboard = InstallHook(WhKeyboardLl, keyboardCallback);
        mouseHook = mouse?.DangerousGetHandle() ?? 0;
        keyboardHook = keyboard?.DangerousGetHandle() ?? 0;
        ready.Set();
        while (GetMessage(out _, 0, 0, 0) > 0)
        {
        }
    }

    private nint MouseHookCallback(int code, nint wParam, nint lParam)
    {
        if (code >= 0)
        {
            uint message = unchecked((uint)wParam);
            ActivityKind activity = message is WmLButtonDown or WmRButtonDown or WmMButtonDown
                ? ActivityKind.MouseClick
                : ActivityKind.MouseMove;
            ActivityDetected?.Invoke(this, activity);
        }
        return CallNextHookEx(mouseHook, code, wParam, lParam);
    }

    private nint KeyboardHookCallback(int code, nint wParam, nint lParam)
    {
        if (code >= 0)
        {
            ActivityDetected?.Invoke(this, ActivityKind.KeyboardInput);
        }
        return CallNextHookEx(keyboardHook, code, wParam, lParam);
    }

    private static SafeHookHandle? InstallHook(int hookType, HookDelegate callback)
    {
        nint module = GetModuleHandle(null);
        nint handle = SetWindowsHookEx(hookType, callback, module, 0);
        return handle == 0 ? null : new SafeHookHandle(handle);
    }

    private delegate nint HookDelegate(int code, nint wParam, nint lParam);

    private sealed class SafeHookHandle(nint handle) : SafeHandle(handle, true)
    {
        protected override bool ReleaseHandle() => UnhookWindowsHookEx(handle);
        public override bool IsInvalid => handle == 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookDelegate callback, nint module, uint threadId);
    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hook, int code, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(nint hook);
    [DllImport("user32.dll")]
    private static extern int GetMessage(out MSG message, nint window, uint filterMin, uint filterMax);
    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint threadId, uint message, nint wParam, nint lParam);
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern nint GetModuleHandle(string? moduleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint Hwnd;
        public uint Message;
        public nint WParam;
        public nint LParam;
        public uint Time;
        public int X;
        public int Y;
    }
}
