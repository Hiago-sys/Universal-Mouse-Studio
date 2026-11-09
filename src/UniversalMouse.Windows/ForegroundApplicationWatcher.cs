using System.Diagnostics;
using System.Runtime.InteropServices;
namespace UniversalMouse.Windows;

/// <summary>Foreground-change notifications only. Does not poll or inspect typed keys.</summary>
public sealed class ForegroundApplicationWatcher : IDisposable
{
    readonly WinEventProc callback;
    IntPtr hook;
    int pending;
    volatile bool disposed;
    public event Action<string>? ApplicationChanged;
    public ForegroundApplicationWatcher()
    {
        callback = Changed;
        hook = SetWinEventHook(3, 3, IntPtr.Zero, callback, 0, 0, 0); // EVENT_SYSTEM_FOREGROUND, OUTOFCONTEXT
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception();
    }
    void Changed(IntPtr handle, uint eventId, IntPtr hwnd, int objectId, int childId, uint thread, uint time)
    {
        if (disposed || Interlocked.Exchange(ref pending, 1) != 0) return;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
                if (pid == 0 || disposed) return;
                using var process = Process.GetProcessById((int)pid);
                string name = process.ProcessName + ".exe";
                if (!disposed) ApplicationChanged?.Invoke(name);
            }
            catch { /* Protected or exited process is inconclusive, not an application error. */ }
            finally { Volatile.Write(ref pending, 0); }
        });
    }
    public void Dispose() { disposed = true; if (hook != IntPtr.Zero) { UnhookWinEvent(hook); hook = IntPtr.Zero; } }
    delegate void WinEventProc(IntPtr hook, uint eventId, IntPtr hwnd, int objectId, int childId, uint thread, uint time);
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc proc, uint process, uint thread, uint flags);
    [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint process);
}
