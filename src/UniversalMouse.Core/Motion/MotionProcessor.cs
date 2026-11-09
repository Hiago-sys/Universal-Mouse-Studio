namespace UniversalMouse.Core.Motion;

/// <summary>
/// Stateful, single-consumer relative-motion transformer. No timers, tasks or allocations in Process.
/// A decaying evidence vector selects a line; bounded integer noise is tolerated within it.
/// Deliberate turns release the lock. Perpendicular displacement removed by snapping is intentional:
/// it must not be stored then replayed after a stop. Quantization remainder is handled separately.
/// </summary>
public sealed class MotionProcessor
{
    private MotionSettings _settings = new();
    private double _evidenceX, _evidenceY, _distance, _lastTime = double.NaN;
    private double _lockedAngle = double.NaN, _previousX, _previousY;
    private bool _hasPrevious;
    private double _microDeparture;
    public bool HasDirectionLock => !double.IsNaN(_lockedAngle);
    private const double Degrees = 180 / Math.PI;

    public MotionProcessor() { }
    public MotionProcessor(MotionSettings settings) => Configure(settings);
    public MotionSettings Settings => _settings;

    public void Configure(MotionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        _settings = settings;
        Reset();
    }

    public void Reset()
    {
        _evidenceX = _evidenceY = _distance = _previousX = _previousY = 0;
        _lastTime = _lockedAngle = double.NaN;
        _hasPrevious = false;
        _microDeparture = 0;
    }

    public MotionResult Process(double dx, double dy, double timestampMilliseconds)
    {
        if (!double.IsFinite(dx) || !double.IsFinite(dy) || Math.Abs(dx) > 1_000_000 || Math.Abs(dy) > 1_000_000)
            throw new ArgumentOutOfRangeException(nameof(dx), "Deslocamento inválido ou acima do limite seguro.");
        if (!double.IsFinite(timestampMilliseconds))
            throw new ArgumentOutOfRangeException(nameof(timestampMilliseconds));

        if (!_settings.Enabled)
        {
            Reset();
            return new(dx, dy, Direction(dx, dy), false);
        }

        if (!double.IsNaN(_lastTime) && (timestampMilliseconds < _lastTime || timestampMilliseconds - _lastTime > _settings.ResetAfterMilliseconds))
            Reset();
        _lastTime = timestampMilliseconds;

        // Zero input never generates a tail. A physical pause is also detected by the timestamp gap.
        if (dx == 0 && dy == 0)
        {
            Reset();
            return new(0, 0, 0, false);
        }

        double length = Math.Sqrt(dx * dx + dy * dy);
        double angle = Direction(dx, dy);
        double evidenceLength = Math.Sqrt(_evidenceX * _evidenceX + _evidenceY * _evidenceY);
        // Tiny perpendicular packets are not a reversal of the established trajectory.
        bool reversed = dx * _evidenceX + dy * _evidenceY < -0.25 * length * evidenceLength;
        if (reversed)
        {
            _evidenceX = _evidenceY = _distance = 0;
            _lockedAngle = double.NaN;
            _microDeparture = 0;
            _hasPrevious = false;
        }

        // Bounded spatial window: old evidence loses half its weight per 24 units traveled.
        double retain = Math.Exp(-Math.Min(length, 1000) / 24);
        _evidenceX = _evidenceX * retain + dx;
        _evidenceY = _evidenceY * retain + dy;
        _distance = Math.Min(1000, _distance + length);

        double releaseTolerance = _settings.ToleranceDegrees + _settings.HysteresisDegrees;
        if (!double.IsNaN(_lockedAngle))
        {
            bool release = LineDistance(angle, _lockedAngle) > releaseTolerance;
            if (release)
            {
                double radians = _lockedAngle / Degrees;
                double perpendicular = _lockedAngle == 0 ? dy : _lockedAngle == 90 ? -dx :
                    -dx * Math.Sin(radians) + dy * Math.Cos(radians);
                double departure = _microDeparture + perpendicular;
                // Integer HID packets can be 45/90 degrees even when the whole stroke is nearly
                // horizontal. Accept only isolated one-count cross-axis packets, inside the
                // established trajectory cone and at most two net counts before releasing.
                bool microNoise = Math.Abs(perpendicular) <= 1.000000001 && Math.Abs(departure) <= 2 &&
                    LineDistance(Direction(_evidenceX, _evidenceY), _lockedAngle) <= releaseTolerance;
                if (microNoise) { _microDeparture = departure; release = false; }
            }
            else _microDeparture = 0;
            if (release)
            {
                _lockedAngle = double.NaN;
                _microDeparture = 0;
                _evidenceX = dx;
                _evidenceY = dy;
                _distance = length;
                _hasPrevious = false;
            }
        }

        if (double.IsNaN(_lockedAngle) && _distance >= _settings.DetectionDistance)
        {
            double evidenceAngle = Direction(_evidenceX, _evidenceY);
            double nearest = NearestLine(evidenceAngle);
            if (!double.IsNaN(nearest) && LineDistance(evidenceAngle, nearest) <= _settings.ToleranceDegrees &&
                LineDistance(angle, nearest) <= _settings.ToleranceDegrees)
                _lockedAngle = nearest;
        }

        double x = dx, y = dy;
        if (!double.IsNaN(_lockedAngle) && _settings.Strength > 0)
        {
            double radians = _lockedAngle / Degrees;
            double ux = Math.Cos(radians), uy = Math.Sin(radians);
            // Exact axes avoid tiny trigonometric noise on perfectly straight input.
            if (_lockedAngle == 0) { ux = 1; uy = 0; }
            if (_lockedAngle == 90) { ux = 0; uy = 1; }
            double projection = dx * ux + dy * uy;
            double axisStrength = _lockedAngle == 0 ? _settings.HorizontalStrength :
                                  _lockedAngle == 90 ? _settings.VerticalStrength : 1;
            double blend = _settings.Strength * axisStrength;
            x += (projection * ux - dx) * blend;
            y += (projection * uy - dy) * blend;
        }

        // Independent optional delta smoothing. No timer-driven residual motion is generated.
        if (_settings.Smoothing > 0 && _hasPrevious && dx * _previousX + dy * _previousY >= 0)
        {
            x = x * (1 - _settings.Smoothing) + _previousX * _settings.Smoothing;
            y = y * (1 - _settings.Smoothing) + _previousY * _settings.Smoothing;
            // Deceleration must not replay the speed of the previous packet as a cursor jump.
            double smoothedLength = Math.Sqrt(x * x + y * y);
            if (smoothedLength > length) { double scale = length / smoothedLength; x *= scale; y *= scale; }
        }
        _previousX = x;
        _previousY = y;
        _hasPrevious = true;
        return new(x, y, Direction(x, y), Math.Abs(x - dx) > 1e-10 || Math.Abs(y - dy) > 1e-10);
    }

    private double NearestLine(double angle)
    {
        double result = double.NaN, distance = double.PositiveInfinity;
        if (_settings.Horizontal) Consider(0, angle, ref result, ref distance);
        if (_settings.Vertical) Consider(90, angle, ref result, ref distance);
        if (_settings.Diagonal)
        {
            Consider(_settings.DiagonalDegrees, angle, ref result, ref distance);
            Consider(180 - _settings.DiagonalDegrees, angle, ref result, ref distance);
        }
        return result;
    }

    private static void Consider(double candidate, double angle, ref double result, ref double distance)
    {
        double current = LineDistance(angle, candidate);
        if (current < distance) { result = candidate; distance = current; }
    }

    private static double Direction(double x, double y) => Math.Atan2(y, x) * Degrees;
    private static double LineDistance(double first, double second)
    {
        double delta = Math.Abs(first - second) % 180;
        return Math.Min(delta, 180 - delta);
    }
}
