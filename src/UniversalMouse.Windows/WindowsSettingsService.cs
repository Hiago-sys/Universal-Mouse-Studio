using System.ComponentModel;
using System.Runtime.InteropServices;
namespace UniversalMouse.Windows;

public sealed record WindowsMouseSettings
{
    public int PointerSpeed { get; init; } = 10;
    public bool EnhancePointerPrecision { get; init; }
    public bool SwapButtons { get; init; }
    public int DoubleClickMilliseconds { get; init; } = 500;
    /// <summary>-1 means one page; other values mean lines.</summary>
    public int WheelLines { get; init; } = 3;
    public bool InactiveScroll { get; init; } = true;
    public bool ClickLock { get; init; }
    internal int AccelerationMode { get; init; }
    internal int? AccelerationThreshold1 { get; init; }
    internal int? AccelerationThreshold2 { get; init; }
    internal int WheelRouting { get; init; }
}

/// <summary>Changes global Windows preferences. Does not change physical sensor DPI.</summary>
public sealed class WindowsSettingsService : IWindowsSettingsService
{
    const uint Flags = 3; // SPIF_UPDATEINIFILE | SPIF_SENDCHANGE
    public WindowsMouseSettings Read()
    {
        EnsureWindows();
        var acceleration = new int[3];
        Check(SpiArray(0x0003, 0, acceleration, 0));
        return new()
        {
            PointerSpeed = ReadInt(0x0070), EnhancePointerPrecision = acceleration[2] != 0, AccelerationMode = acceleration[2], AccelerationThreshold1 = acceleration[0], AccelerationThreshold2 = acceleration[1], WheelRouting = ReadInt(0x201C),
            SwapButtons = GetSystemMetrics(23) != 0, DoubleClickMilliseconds = checked((int)GetDoubleClickTime()),
            WheelLines = unchecked((int)(uint)ReadInt(0x0068)),
            InactiveScroll = ReadInt(0x201C) == 2, ClickLock = ReadInt(0x101E) != 0
        };
    }
    public void Apply(WindowsMouseSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.PointerSpeed is < 1 or > 20 || settings.DoubleClickMilliseconds is < 1 or > 5000 || settings.WheelLines < -1)
            throw new ArgumentOutOfRangeException(nameof(settings), "Velocidade: 1–20; clique duplo: 1–5000 ms; rolagem: -1 ou número não negativo.");
        var before = Read();
        try
        {
            Write(settings);
            if (!Matches(Read(), settings)) throw new InvalidOperationException("O Windows não confirmou todas as configurações aplicadas.");
        }
        catch (Exception original)
        {
            try { Write(before); if (Read() != before) throw new InvalidOperationException("A restauração não foi confirmada pelo Windows."); }
            catch (Exception recovery) { throw new AggregateException("Falha ao aplicar e restaurar as configurações do Windows.", original, recovery); }
            throw;
        }
    }
    static void Write(WindowsMouseSettings s)
    {
        Check(Spi(0x0071, 0, new IntPtr(s.PointerSpeed), Flags));
        var acceleration = new int[3];
        Check(SpiArray(0x0003, 0, acceleration, 0));
        if (s.AccelerationThreshold1.HasValue) acceleration[0] = s.AccelerationThreshold1.Value;
        if (s.AccelerationThreshold2.HasValue) acceleration[1] = s.AccelerationThreshold2.Value;
        acceleration[2] = s.EnhancePointerPrecision ? (s.AccelerationMode is 1 or 2 ? s.AccelerationMode : 1) : 0;
        Check(SpiArray(0x0004, 0, acceleration, Flags));
        Check(Spi(0x0021, s.SwapButtons ? 1u : 0, IntPtr.Zero, Flags));
        Check(Spi(0x0020, (uint)s.DoubleClickMilliseconds, IntPtr.Zero, Flags));
        Check(Spi(0x0069, unchecked((uint)s.WheelLines), IntPtr.Zero, Flags));
        int routing = s.InactiveScroll ? 2 : s.WheelRouting is 0 or 1 ? s.WheelRouting : 0;
        Check(SpiRef(0x201D, 0, ref routing, Flags));
        Check(Spi(0x101F, 0, new IntPtr(s.ClickLock ? 1 : 0), Flags));
    }
    static bool Matches(WindowsMouseSettings a, WindowsMouseSettings b) => a.PointerSpeed == b.PointerSpeed && a.EnhancePointerPrecision == b.EnhancePointerPrecision && a.SwapButtons == b.SwapButtons && a.DoubleClickMilliseconds == b.DoubleClickMilliseconds && a.WheelLines == b.WheelLines && a.InactiveScroll == b.InactiveScroll && a.ClickLock == b.ClickLock;
    static int ReadInt(uint action) { Check(SpiRead(action, 0, out int result, 0)); return result; }
    static void Check(bool success) { if (!success) throw new Win32Exception(Marshal.GetLastWin32Error()); }
    static void EnsureWindows() { if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Esta operação requer Windows."); }
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] static extern bool Spi(uint action, uint ui, IntPtr pv, uint flags);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] static extern bool SpiRef(uint action, uint ui, ref int pv, uint flags);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] static extern bool SpiRead(uint action, uint ui, out int pv, uint flags);
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)] static extern bool SpiArray(uint action, uint ui, [In, Out] int[] pv, uint flags);
    [DllImport("user32.dll")] static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] static extern uint GetDoubleClickTime();
}
