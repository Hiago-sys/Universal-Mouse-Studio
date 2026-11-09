namespace UniversalMouse.Core.Diagnostics;

public sealed record PollingSnapshot(int Intervals, double? AverageHz, double? MaximumObservedHz, double? IntervalStandardDeviationMilliseconds,
    IReadOnlyList<double> RecentHz, string Limitation);

/// <summary>Bounded event-arrival estimator. This is not a USB polling-frequency measurement.</summary>
public sealed class PollingEstimator
{
    private readonly double[] _intervals = new double[1024];
    private int _next, _count;
    private double? _last;

    public void Add(double timestampMilliseconds)
    {
        if (!double.IsFinite(timestampMilliseconds)) return;
        if (_last is double previous)
        {
            double dt = timestampMilliseconds - previous;
            if (dt > 0 && dt < 1000)
            {
                _intervals[_next] = dt;
                _next = (_next + 1) % _intervals.Length;
                _count = Math.Min(_count + 1, _intervals.Length);
            }
        }
        _last = timestampMilliseconds;
    }

    public void Reset() { _next = _count = 0; _last = null; }

    public PollingSnapshot Snapshot()
    {
        const string limitation = "Estimativa de eventos de movimento recebidos pelo software, influenciada pelo movimento, escalonamento, coalescência e resolução temporal. Não mede o polling USB físico nem a latência de entrada.";
        if (_count == 0) return new(0, null, null, null, [], limitation);
        double sum = 0, min = double.PositiveInfinity;
        for (int i = 0; i < _count; i++) { sum += _intervals[i]; min = Math.Min(min, _intervals[i]); }
        double mean = sum / _count, variance = 0;
        for (int i = 0; i < _count; i++) variance += Math.Pow(_intervals[i] - mean, 2);
        var recent = new double[Math.Min(60, _count)];
        for (int i = 0; i < recent.Length; i++) recent[i] = 1000 / _intervals[(_next - recent.Length + i + _intervals.Length) % _intervals.Length];
        return new(_count, 1000 / mean, 1000 / min, Math.Sqrt(variance / _count), recent, limitation);
    }
}
