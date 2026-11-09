using System.Text.Json;

namespace UniversalMouse.Core.Diagnostics;

public sealed record ClickDiagnosticSnapshot(int TotalClicks, int PossibleDuplicateIntervals, double? LastIntervalMilliseconds, string Assessment);

/// <summary>Transient counts only. A short interval can be intentional and does not establish a physical switch fault.</summary>
public sealed class ClickDiagnostics
{
    private readonly Dictionary<string, double> _last = new(StringComparer.Ordinal);
    private int _total, _suspect;
    private double? _interval;
    public void Record(string button, double timestampMilliseconds)
    {
        if (!double.IsFinite(timestampMilliseconds) || button is not ("Left" or "Right" or "Middle" or "X1" or "X2")) return;
        _total++;
        if (_last.TryGetValue(button, out double previous) && timestampMilliseconds >= previous)
        {
            _interval = timestampMilliseconds - previous;
            if (_interval is >= 0 and < 35) _suspect++;
        }
        _last[button] = timestampMilliseconds;
    }
    public void Reset() { _last.Clear(); _total = _suspect = 0; _interval = null; }
    public ClickDiagnosticSnapshot Snapshot() => new(_total, _suspect, _interval,
        _total < 2 ? "Inconclusivo: faça uma sequência de cliques no teste." :
        _suspect > 0 ? "Atenção: intervalos abaixo de 35 ms observados. Podem ser intencionais; não comprovam falha física." :
        "Nenhum intervalo abaixo de 35 ms observado nesta sessão. Isso não comprova ausência de falha física.");
}

public sealed record DiagnosticReport(DateTimeOffset CreatedUtc, PollingSnapshot Polling, ClickDiagnosticSnapshot Clicks,
    string Scope = "Dados agregados da sessão de teste; sem trajetórias, teclas ou identificadores persistentes de dispositivos.")
{
    public Task SaveAsync(string path) => File.WriteAllTextAsync(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
}
