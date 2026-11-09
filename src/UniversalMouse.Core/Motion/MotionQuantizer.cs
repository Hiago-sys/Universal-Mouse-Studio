namespace UniversalMouse.Core.Motion;

/// <summary>Preserves subpixel rounding remainder, not the intentionally removed snapping component.</summary>
public sealed class MotionQuantizer
{
    private double _remainderX, _remainderY;
    public void Reset() => _remainderX = _remainderY = 0;

    /// <summary>Identity events bypass rounding. Fractions can survive aligned packets inside a confirmed direction lock; turns discard them.</summary>
    public (int X, int Y) QuantizeOrPassthrough(int rawX, int rawY, MotionResult result, bool preserveFractionOnIdentity = false)
    {
        if (result.X == rawX && result.Y == rawY)
        {
            if (!preserveFractionOnIdentity) Reset();
            return (rawX, rawY);
        }
        return Quantize(result.X, result.Y);
    }

    public (int X, int Y) Quantize(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y) || Math.Abs(x) > 1_000_000 || Math.Abs(y) > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(x));
        double totalX = x + _remainderX, totalY = y + _remainderY;
        int resultX = (int)Math.Round(totalX, MidpointRounding.ToEven);
        int resultY = (int)Math.Round(totalY, MidpointRounding.ToEven);
        _remainderX = totalX - resultX;
        _remainderY = totalY - resultY;
        return (resultX, resultY);
    }
}
