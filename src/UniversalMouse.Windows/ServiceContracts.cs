using UniversalMouse.Core.Motion;
namespace UniversalMouse.Windows;

public interface IWindowsSettingsService
{
    WindowsMouseSettings Read();
    void Apply(WindowsMouseSettings settings);
}

public readonly record struct DesktopInputStatistics(long PhysicalMoves, long TransformedMoves, long ReinjectedMoves, long ZeroDeltaMoves);

public interface IDesktopInputService : IDisposable
{
    DesktopInputStatistics GetStatistics() => default;
    bool IsRunning { get; }
    string? LastError { get; }
    event Action? StateChanged;
    void Start(MotionSettings settings);
    void Stop();
    void ConfigureButtons(string x1, string x2);
}
