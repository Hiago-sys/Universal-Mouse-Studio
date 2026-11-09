namespace UniversalMouse.Core.Motion;

/// <summary>All strengths are fractions (0..1); angles are degrees and time is milliseconds.</summary>
public sealed record MotionSettings
{
    public bool Enabled { get; init; }
    public double Strength { get; init; } = 0.65;
    public double ToleranceDegrees { get; init; } = 8;
    public bool Horizontal { get; init; } = true;
    public bool Vertical { get; init; } = true;
    public double HorizontalStrength { get; init; } = 1;
    public double VerticalStrength { get; init; } = 1;
    public bool Diagonal { get; init; }
    public double DiagonalDegrees { get; init; } = 45;
    public double Smoothing { get; init; }
    public double DetectionDistance { get; init; } = 3;
    public double HysteresisDegrees { get; init; } = 3;
    public double ResetAfterMilliseconds { get; init; } = 80;

    public void Validate()
    {
        Check(Strength, 0, 1, nameof(Strength));
        Check(ToleranceDegrees, 0, 30, nameof(ToleranceDegrees));
        Check(HorizontalStrength, 0, 1, nameof(HorizontalStrength));
        Check(VerticalStrength, 0, 1, nameof(VerticalStrength));
        Check(DiagonalDegrees, 1, 89, nameof(DiagonalDegrees));
        Check(Smoothing, 0, 0.9, nameof(Smoothing));
        Check(DetectionDistance, 0, 100, nameof(DetectionDistance));
        Check(HysteresisDegrees, 0, 10, nameof(HysteresisDegrees));
        Check(ResetAfterMilliseconds, 1, 2000, nameof(ResetAfterMilliseconds));
    }

    private static void Check(double value, double min, double max, string name)
    {
        if (!double.IsFinite(value) || value < min || value > max)
            throw new ArgumentOutOfRangeException(name, value, $"Use um valor entre {min} e {max}.");
    }
}

public readonly record struct MotionResult(double X, double Y, double AngleDegrees, bool IsCorrected);
