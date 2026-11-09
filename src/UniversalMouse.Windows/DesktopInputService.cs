using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UniversalMouse.Core.Motion;
using Microsoft.Win32;
namespace UniversalMouse.Windows;

/// <summary>
/// Experimental desktop cursor transform. It sees accelerated screen-space deltas,
/// not per-device raw motion. It cannot replace Raw Input used by games. Windows validation pending.
/// No drivers, no elevation, no interception of ordinary clicks or wheel input.
/// </summary>
public sealed class DesktopInputService : IDesktopInputService
{
    const uint StopMessage = 0x8001;
    static readonly System.Collections.Concurrent.ConcurrentDictionary<IntPtr, HookProc> RetainedHooks = new();
    static readonly nuint InjectionTag = 0x554D5301;
    readonly object lifecycle = new();
    Thread? thread;
    uint threadId;
    IntPtr hook;
    HookProc? callback;
    volatile bool running;
    volatile bool stopRequested;
    volatile string? error;
    MotionSettings requested = new();
    MotionSettings? configured;
    readonly MotionProcessor processor = new();
    readonly NativeInput[] moveInput = new NativeInput[1];
    readonly MotionQuantizer quantizer = new();
    double lastMovement = double.NaN;
    ButtonConfiguration buttons = new("original", "original");
    bool x1Suppressed, x2Suppressed;
    public bool IsRunning => running;
    public string? LastError => error;
    public event Action? StateChanged;

    public void ConfigureButtons(string x1, string x2)
    {
        if (!Actions.Contains(x1) || !Actions.Contains(x2)) throw new ArgumentException("Ação de botão desconhecida.");
        Volatile.Write(ref buttons, new(x1, x2));
    }
    static readonly HashSet<string> Actions = ["original", "copy", "paste", "cut", "undo", "volume-up", "volume-down", "play-pause"];
    sealed record ButtonConfiguration(string X1, string X2);

    public void Start(MotionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        lock (lifecycle)
        {
            Volatile.Write(ref requested, settings);
            if (thread?.IsAlive == true)
            {
                if (stopRequested) throw new InvalidOperationException("Aguarde o encerramento do processamento anterior.");
                return;
            }
            if (hook != IntPtr.Zero) throw new InvalidOperationException("O hook anterior ainda possui limpeza pendente. Encerre a aplicação antes de reativar.");
            error = null; stopRequested = false;
            thread = new Thread(Run) { IsBackground = true, Name = "UMS desktop input" };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
    public void Stop()
    {
        stopRequested = true;
        uint id = Volatile.Read(ref threadId);
        if (id != 0) PostThreadMessage(id, StopMessage, IntPtr.Zero, IntPtr.Zero);
    }
    void Run()
    {
        try
        {
            threadId = GetCurrentThreadId();
            // Force a queue before publishing a stoppable hook thread.
            PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
            if (stopRequested) return;
            if (!RegisterHotKey(IntPtr.Zero, 1, 0x4003, 0x7A)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Não foi possível registrar Ctrl+Alt+F11. O processamento não foi ativado.");
            callback = OnMouse;
            hook = SetWindowsHookEx(14, callback, GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception();
            SystemEvents.PowerModeChanged += OnPowerChanged;
            SystemEvents.SessionSwitch += OnSessionChanged;
            SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            configured = null; processor.Reset(); quantizer.Reset(); lastMovement = double.NaN; running = true; Notify();
            while (!stopRequested)
            {
                int result = GetMessage(out var message, IntPtr.Zero, 0, 0);
                if (result == -1) throw new Win32Exception();
                if (result == 0 || message.Id == StopMessage || message.Id == 0x0312) break;
                TranslateMessage(ref message); DispatchMessage(ref message);
            }
        }
        catch (Exception ex) { error = ex.Message; }
        finally
        {
            running = false; stopRequested = true;
            SystemEvents.PowerModeChanged -= OnPowerChanged;
            SystemEvents.SessionSwitch -= OnSessionChanged;
            SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
            CleanupHook();
            UnregisterHotKey(IntPtr.Zero, 1);
            processor.Reset(); quantizer.Reset(); configured = null; threadId = 0;
            x1Suppressed = x2Suppressed = false;
            Notify();
        }
    }
    void OnPowerChanged(object sender, PowerModeChangedEventArgs args)
    {
        if (args.Mode is PowerModes.Suspend or PowerModes.Resume) Stop();
    }
    void OnSessionChanged(object sender, SessionSwitchEventArgs args) => Stop();
    void OnDisplayChanged(object? sender, EventArgs args) => Stop();
    IntPtr OnMouse(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code < 0 || !running || stopRequested) return CallNextHookEx(hook, code, wParam, lParam);
        long start = Stopwatch.GetTimestamp();
        try
        {
            var data = Marshal.PtrToStructure<MouseHookData>(lParam);
            if ((data.Flags & 1) != 0) return CallNextHookEx(hook, code, wParam, lParam);
            int message = wParam.ToInt32();
            if (message is 0x020B or 0x020C)
            {
                bool first = (data.MouseData >> 16) == 1;
                ref bool suppressed = ref (first ? ref x1Suppressed : ref x2Suppressed);
                if (message == 0x020C)
                {
                    if (suppressed) { suppressed = false; return new IntPtr(1); }
                }
                else
                {
                    var mapping = Volatile.Read(ref buttons);
                    string action = first ? mapping.X1 : mapping.X2;
                    if (action != "original" && SendAction(action)) { suppressed = true; return new IntPtr(1); }
                }
            }
            if (message != 0x0200) return CallNextHookEx(hook, code, wParam, lParam);
            var settings = Volatile.Read(ref requested);
            if (!ReferenceEquals(settings, configured)) { processor.Configure(settings); quantizer.Reset(); configured = settings; }
            if (!settings.Enabled) return CallNextHookEx(hook, code, wParam, lParam);
            if (!GetCursorPos(out var current)) throw new Win32Exception();
            double dx = data.Point.X - current.X, dy = data.Point.Y - current.Y;
            if (Math.Abs(dx) > 16384 || Math.Abs(dy) > 16384) { processor.Reset(); quantizer.Reset(); lastMovement = double.NaN; return CallNextHookEx(hook, code, wParam, lParam); }
            double time = start * 1000.0 / Stopwatch.Frequency;
            if (double.IsNaN(lastMovement) || time - lastMovement > settings.ResetAfterMilliseconds) quantizer.Reset();
            lastMovement = time;
            var result = processor.Process(dx, dy, time);
            if (Stopwatch.GetElapsedTime(start).TotalMilliseconds > 8) throw new TimeoutException("Processamento de entrada excedeu o limite de 8 ms e foi desativado.");
            if (!double.IsFinite(result.X) || !double.IsFinite(result.Y)) throw new InvalidOperationException("Resultado de movimento inválido.");
            var (stepX, stepY) = quantizer.QuantizeOrPassthrough((int)dx, (int)dy, result);
            int x = current.X + stepX, y = current.Y + stepY;
            if (x == data.Point.X && y == data.Point.Y) return CallNextHookEx(hook, code, wParam, lParam);
            int left = GetSystemMetrics(76), top = GetSystemMetrics(77), width = GetSystemMetrics(78), height = GetSystemMetrics(79);
            if (width < 2 || height < 2 || width > 65536 || height > 65536) throw new InvalidOperationException("Área de trabalho inválida.");
            x = Math.Clamp(x, left, left + width - 1); y = Math.Clamp(y, top, top + height - 1);
            moveInput[0] = new NativeInput { Type = 0, Data = new InputUnion { Mouse = new MouseInput {
                X = DesktopCoordinates.NormalizePixel(x, left, width),
                Y = DesktopCoordinates.NormalizePixel(y, top, height),
                Flags = 0xC001, ExtraInfo = InjectionTag } } }; // MOVE | ABSOLUTE | VIRTUALDESK
            if (SendInput(1, moveInput, Marshal.SizeOf<NativeInput>()) != 1) throw new Win32Exception(Marshal.GetLastWin32Error(), "Reinjeção recusada pelo Windows; movimento original preservado.");
            return new IntPtr(1); // Suppress only after confirmed successful injection.
        }
        catch (Exception ex)
        {
            error = ex.Message; Stop();
            return CallNextHookEx(hook, code, wParam, lParam);
        }
    }
    static bool SendAction(string action)
    {
        ushort key = action switch { "copy" => 0x43, "paste" => 0x56, "cut" => 0x58, "undo" => 0x5A, "volume-up" => 0xAF, "volume-down" => 0xAE, "play-pause" => 0xB3, _ => 0 };
        if (key == 0) return false;
        bool chord = key < 0x80 && (GetAsyncKeyState(0x11) & 0x8000) == 0;
        NativeInput Key(ushort code, bool up) => new() { Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput { VirtualKey = code, Flags = up ? 2u : 0u, ExtraInfo = InjectionTag } } };
        NativeInput[] sequence = chord ? [Key(0x11, false), Key(key, false), Key(key, true), Key(0x11, true)] : [Key(key, false), Key(key, true)];
        if (SendInput((uint)sequence.Length, sequence, Marshal.SizeOf<NativeInput>()) == sequence.Length) return true;
        int failure = Marshal.GetLastWin32Error();
        NativeInput[] release = chord ? [Key(key, true), Key(0x11, true)] : [Key(key, true)];
        SendInput((uint)release.Length, release, Marshal.SizeOf<NativeInput>());
        throw new Win32Exception(failure, "Remapeamento recusado; processamento desativado.");
    }
    void Notify() => ThreadPool.QueueUserWorkItem(_ => { try { StateChanged?.Invoke(); } catch { /* Observers cannot terminate the hook pump. */ } });
    void CleanupHook()
    {
        if (hook == IntPtr.Zero) { callback = null; return; }
        if (UnhookWindowsHookEx(hook) || Marshal.GetLastWin32Error() == 1404)
        {
            RetainedHooks.TryRemove(hook, out _); hook = IntPtr.Zero; callback = null;
        }
        else
        {
            if (callback != null) RetainedHooks[hook] = callback;
            error = "O Windows não confirmou a remoção do hook; processamento interrompido. Encerre a aplicação para liberação pelo sistema.";
        }
    }
    public void Dispose()
    {
        Stop(); var current = thread;
        if (current != null && current != Thread.CurrentThread && current.Join(1000)) CleanupHook();
    }

    [StructLayout(LayoutKind.Sequential)] struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] struct MouseHookData { public Point Point; public uint MouseData, Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] struct Message { public IntPtr Hwnd; public uint Id; public nuint WParam; public nint LParam; public uint Time; public Point Point; public uint Private; }
    [StructLayout(LayoutKind.Sequential)] struct NativeInput { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] struct KeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }
    delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int hook, HookProc proc, IntPtr module, uint thread);
    [DllImport("user32.dll", SetLastError = true)] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll")] static extern bool PostThreadMessage(uint thread, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] static extern int GetMessage(out Message message, IntPtr hwnd, uint min, uint max);
    [DllImport("user32.dll")] static extern bool PeekMessage(out Message message, IntPtr hwnd, uint min, uint max, uint remove);
    [DllImport("user32.dll")] static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] static extern IntPtr DispatchMessage(ref Message message);
    [DllImport("user32.dll", SetLastError = true)] static extern uint SendInput(uint count, NativeInput[] inputs, int size);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int virtualKey);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string? module);
}
