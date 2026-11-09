using System.Diagnostics;
using System.Text.Json;
using UniversalMouse.Core.Motion;

var tests = new List<(string Name, Action Run)>();
void Test(string name, Action action) => tests.Add((name, action));
void Near(double expected, double actual, double epsilon = 1e-9)
{
    if (Math.Abs(expected - actual) > epsilon || !double.IsFinite(actual))
        throw new Exception($"Esperado {expected}; recebido {actual}.");
}
void True(bool condition, string message = "Condição não atendida")
{
    if (!condition) throw new Exception(message);
}
void Throws<T>(Action action) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception($"Exceção {typeof(T).Name} esperada.");
}
MotionSettings full = new() { Enabled = true, Strength = 1, DetectionDistance = 0, ToleranceDegrees = 10 };
MotionProcessor Make(MotionSettings? settings = null) => new(settings ?? full);

Test("Horizontal reta preserva deslocamento nos dois sentidos", () =>
{
    var p = Make();
    foreach (double dx in new[] { 1d, 10d, 300d, -10d, -1d })
    {
        var r = p.Process(dx, 0, 0); Near(dx, r.X); Near(0, r.Y); True(!r.IsCorrected);
    }
});
Test("Vertical reta preserva deslocamento nos dois sentidos", () =>
{
    var p = Make();
    foreach (double dy in new[] { 1d, 10d, 300d, -10d, -1d })
    {
        var r = p.Process(0, dy, 0); Near(0, r.X); Near(dy, r.Y); True(!r.IsCorrected);
    }
});
Test("Pequenos desvios horizontais são corrigidos de verdade", () =>
{
    var p = Make();
    for (int i = 0; i < 100; i++)
    {
        var r = p.Process(10, i % 2 == 0 ? 1 : -1, i); Near(10, r.X); Near(0, r.Y); True(r.IsCorrected);
    }
});
Test("Pequenos desvios verticais são corrigidos", () =>
{
    var p = Make(); var r = p.Process(1, 10, 0); Near(0, r.X); Near(10, r.Y); True(r.IsCorrected);
});
Test("Desvio grande não é transformado em eixo", () =>
{
    var p = Make(); p.Process(10, 1, 0);
    var r = p.Process(10, 6, 1); Near(10, r.X); Near(6, r.Y); True(!r.IsCorrected);
});
Test("Diagonal desligada por padrão não corrige diagonal", () =>
{
    var p = Make(); var r = p.Process(10, 9, 0); Near(10, r.X); Near(9, r.Y);
});
Test("Diagonal configurada corrige 45 e 135 graus", () =>
{
    var p = Make(full with { Diagonal = true });
    var r = p.Process(10, 9, 0); Near(9.5, r.X); Near(9.5, r.Y); True(r.IsCorrected);
    p.Reset(); r = p.Process(-10, 9, 0); Near(-9.5, r.X); Near(9.5, r.Y);
});
Test("Movimento circular preserva pontos fora das tolerâncias", () =>
{
    var p = Make();
    for (int degrees = 0; degrees < 720; degrees++)
    {
        double a = degrees * Math.PI / 180, x = 10 * Math.Cos(a), y = 10 * Math.Sin(a);
        var r = p.Process(x, y, degrees);
        double nearestAxis = Math.Abs(degrees % 90);
        nearestAxis = Math.Min(nearestAxis, 90 - nearestAxis);
        if (nearestAxis > full.ToleranceDegrees + full.HysteresisDegrees + 0.01)
        { Near(x, r.X); Near(y, r.Y); }
        True(double.IsFinite(r.X) && double.IsFinite(r.Y));
    }
});
Test("Movimentos lentos adquirem direção após distância mínima", () =>
{
    var p = Make(full with { DetectionDistance = 3 });
    var initial = p.Process(0.5, 0.02, 0); Near(0.02, initial.Y);
    MotionResult r = default;
    for (int i = 1; i <= 10; i++) r = p.Process(0.5, 0.02, i);
    Near(0, r.Y); True(r.IsCorrected);
});
Test("Movimentos rápidos são corrigidos sem saturar", () =>
{
    var p = Make(); var r = p.Process(5000, 50, 0); Near(5000, r.X); Near(0, r.Y);
});
Test("Movimentos curtos abaixo do limiar passam sem alteração", () =>
{
    var p = Make(full with { DetectionDistance = 10 }); var r = p.Process(1, 0.1, 0); Near(1, r.X); Near(0.1, r.Y);
});
Test("Mudança horizontal para vertical libera eixo anterior", () =>
{
    var p = Make(); p.Process(10, 1, 0);
    var r = p.Process(1, 10, 1); Near(0, r.X); Near(10, r.Y);
});
Test("Pausa limpa estado de aquisição e não cria cauda", () =>
{
    var p = Make(full with { DetectionDistance = 5, Smoothing = 0.5 }); p.Process(10, 1, 0);
    var zero = p.Process(0, 0, 1); Near(0, zero.X); Near(0, zero.Y);
    var r = p.Process(1, 0.1, 2); Near(1, r.X); Near(0.1, r.Y);
    p.Process(10, 1, 3); r = p.Process(1, 0.1, 1000); Near(1, r.X); Near(0.1, r.Y);
});
Test("Ativação e desativação são imediatas e limpam estado", () =>
{
    var p = Make(); p.Process(10, 1, 0); p.Configure(full with { Enabled = false });
    var r = p.Process(10, 1, 1); Near(10, r.X); Near(1, r.Y); True(!r.IsCorrected);
    p.Configure(full); r = p.Process(10, 1, 2); Near(0, r.Y); True(r.IsCorrected);
});
Test("Intensidade zero é identidade; intensidade intermediária interpola", () =>
{
    var p = Make(full with { Strength = 0 }); var r = p.Process(10, 1, 0); Near(1, r.Y);
    p.Configure(full with { Strength = 0.5 }); r = p.Process(10, 1, 0); Near(0.5, r.Y);
});
Test("Forças de eixos e desativação são independentes", () =>
{
    var p = Make(full with { HorizontalStrength = 0.25 }); var r = p.Process(10, 1, 0); Near(0.75, r.Y);
    p.Configure(full with { Horizontal = false }); r = p.Process(10, 1, 0); Near(1, r.Y);
    p.Configure(full with { Vertical = false }); r = p.Process(1, 10, 0); Near(1, r.X);
});
Test("Histerese mantém eixo perto do limite e libera fora dele", () =>
{
    var p = Make(full with { ToleranceDegrees = 5, HysteresisDegrees = 3 });
    p.Process(10, 0.5, 0);
    var r = p.Process(10, 10 * Math.Tan(6 * Math.PI / 180), 1); Near(0, r.Y); True(r.IsCorrected);
    double y = 10 * Math.Tan(9 * Math.PI / 180);
    r = p.Process(10, y, 2); Near(y, r.Y); True(!r.IsCorrected);
});
Test("Quantização conserva frações sem armazenar erro angular", () =>
{
    var q = new MotionQuantizer(); int sumX = 0, sumY = 0;
    for (int i = 0; i < 1000; i++) { var r = q.Quantize(0.1, -0.2); sumX += r.X; sumY += r.Y; }
    Near(100, sumX); Near(-200, sumY);
    q.Reset(); var zero = q.Quantize(0, 0); Near(0, zero.X); Near(0, zero.Y);
});
Test("Entradas inválidas são rejeitadas; configurações inválidas não substituem as válidas", () =>
{
    var p = Make();
    Throws<ArgumentOutOfRangeException>(() => p.Process(double.NaN, 0, 0));
    Throws<ArgumentOutOfRangeException>(() => p.Process(0, double.PositiveInfinity, 0));
    Throws<ArgumentOutOfRangeException>(() => p.Process(1_000_001, 0, 0));
    Throws<ArgumentOutOfRangeException>(() => p.Process(0, 0, double.NaN));
    Throws<ArgumentOutOfRangeException>(() => p.Configure(full with { Strength = 2 }));
    Throws<ArgumentOutOfRangeException>(() => p.Configure(full with { ToleranceDegrees = 31 }));
    Throws<ArgumentOutOfRangeException>(() => p.Configure(full with { Smoothing = double.NaN }));
    Near(0, p.Process(10, 1, 0).Y);
});
Test("Evento sem transformação preserva inteiros após resíduos de meio pixel", () =>
{
    foreach (double corrected in new[] { 0.5, -0.5 })
    {
        var q = new MotionQuantizer();
        q.Quantize(corrected, -corrected);
        var result = q.QuantizeOrPassthrough(1, -1, new(1, -1, -45, false));
        Near(1, result.X); Near(-1, result.Y);
        var next = q.Quantize(0.1, -0.1);
        Near(0, next.X); Near(0, next.Y);
    }
});
Test("Quantização de evento corrigido preserva frações até um passthrough", () =>
{
    var q = new MotionQuantizer();
    var first = q.QuantizeOrPassthrough(10, 1, new(10, 0.5, 0, true));
    var second = q.QuantizeOrPassthrough(10, 1, new(10, 0.5, 0, true));
    Near(0, first.Y); Near(1, second.Y);
    var raw = q.QuantizeOrPassthrough(3, 4, new(3, 4, 0, false));
    Near(3, raw.X); Near(4, raw.Y);
});
Test("Coordenadas absolutas representam cada pixel em dimensões de desktop comuns e extremas", () =>
{
    foreach (int extent in new[] { 2, 3, 1920, 2560, 3840, 7680, 15360, 32768, 50000, 65535, 65536 })
    foreach (int origin in new[] { 0, -7680 })
    {
        for (int offset = 0; offset < extent; offset++)
        {
            int normalized = DesktopCoordinates.NormalizePixel(origin + offset, origin, extent);
            True(normalized is >= 0 and <= 65535);
            int decoded = (int)((long)normalized * extent / 65536);
            if (decoded != offset) throw new Exception($"Extent={extent}, origin={origin}, pixel={offset}, normalized={normalized}, decoded={decoded}");
        }
    }
});
Test("Conversão de coordenadas recusa dimensões e pixels inválidos sem overflow", () =>
{
    Throws<ArgumentOutOfRangeException>(() => DesktopCoordinates.NormalizePixel(0, 0, 1));
    Throws<ArgumentOutOfRangeException>(() => DesktopCoordinates.NormalizePixel(0, 0, 65537));
    Throws<ArgumentOutOfRangeException>(() => DesktopCoordinates.NormalizePixel(-1, 0, 1920));
    Throws<ArgumentOutOfRangeException>(() => DesktopCoordinates.NormalizePixel(1920, 0, 1920));
    Throws<ArgumentOutOfRangeException>(() => DesktopCoordinates.NormalizePixel(int.MaxValue, int.MinValue, 1920));
});
Test("Trajetória lenta em degraus inteiros recebe correção no laboratório", () =>
{
    var p = Make(new MotionSettings { Enabled = true });
    double rawY = 0, correctedY = 0;
    int correctedEvents = 0;
    for (int i = 0; i < 640; i++)
    {
        int dy = i % 8 == 7 ? 1 : 0;
        var r = p.Process(1, dy, i * 2);
        rawY += dy; correctedY += r.Y;
        if(r.IsCorrected)correctedEvents++;
    }
    Console.WriteLine($"LABTRACE brutoY={rawY:F3} corrigidoY={correctedY:F3} eventosCorrigidos={correctedEvents}");
    True(correctedEvents >= 70, $"Somente {correctedEvents} eventos corrigidos.");
    True(correctedY < rawY * 0.5, $"Desvio bruto {rawY}; corrigido {correctedY}.");
});

Test("Pequeno movimento vertical em degraus inteiros também é corrigido", () =>
{
    var p = Make(new MotionSettings { Enabled = true }); double rawX=0, correctedX=0;
    for(int i=0;i<640;i++){int dx=i%8==7?1:0;var r=p.Process(dx,1,i*2);rawX+=dx;correctedX+=r.X;}
    True(correctedX<rawX*0.5);
});
Test("Ruído perpendicular isolado não desmonta a direção dominante", () =>
{
    var p=Make(new MotionSettings{Enabled=true,Strength=1});
    for(int i=0;i<30;i++)p.Process(1,0,i);
    var up=p.Process(0,1,31);Near(0,up.Y);True(up.IsCorrected);
    var down=p.Process(0,-1,32);Near(0,down.Y);True(down.IsCorrected);
    Near(1,p.Process(1,0,33).X);
});
Test("Virada intencional lenta não é bloqueada por ruído permitido", () =>
{
    var p=Make(new MotionSettings{Enabled=true,Strength=1});
    for(int i=0;i<30;i++)p.Process(1,0,i);
    double y=0;for(int i=0;i<20;i++)y+=p.Process(0,1,31+i).Y;
    True(y>=18,$"A virada perdeu mais de duas contagens: {y}");
});
Test("Força intermediária conserva frações entre pacotes alinhados", () =>
{
    foreach(double strength in new[]{0d,0.25,0.65,1})
    {
        var p=Make(new MotionSettings{Enabled=true,Strength=strength});var q=new MotionQuantizer();int total=0;
        for(int i=0;i<640;i++){int dy=i%8==7?1:0;var r=p.Process(1,dy,i*2);total+=q.QuantizeOrPassthrough(1,dy,r,p.HasDirectionLock).Y;}
        Near(80*(1-strength),total,0.500000001);
    }
});

Test("Pacotes de quatro contagens mantêm correção de um pequeno desvio isolado", () =>
{
    var p=Make(new MotionSettings{Enabled=true});double raw=0,corrected=0;
    for(int i=0;i<160;i++){int dy=i%2;var r=p.Process(4,dy,i);raw+=dy;corrected+=r.Y;}
    True(corrected<raw*0.5,$"Desvio bruto {raw}; corrigido {corrected}");
});
Test("Suavização não amplifica deslocamento ao desacelerar", () =>
{
    var p=Make(full with{Smoothing=0.9});p.Process(1000,1,0);
    var r=p.Process(1,0,1);True(Math.Sqrt(r.X*r.X+r.Y*r.Y)<=1+1e-9);
    var zero=p.Process(0,0,2);Near(0,zero.X);Near(0,zero.Y);
});
Test("Trajetórias lentas são simétricas nos quatro sentidos", () =>
{
    foreach(int sx in new[]{-1,1})foreach(int sy in new[]{-1,1})
    {var p=Make(new MotionSettings{Enabled=true});double y=0;for(int i=0;i<640;i++)y+=p.Process(sx,i%8==7?sy:0,i).Y;Near(sy*28,y,1e-8);}
});
Test("Mesma sequência espacial mantém resultado em 125 e 8000 eventos por segundo", () =>
{
    var a=Make(new MotionSettings{Enabled=true});var b=Make(new MotionSettings{Enabled=true});
    for(int i=0;i<2000;i++){int y=i%8==7?1:0;var x=a.Process(1,y,i*8d);var z=b.Process(1,y,i*0.125);Near(x.X,z.X);Near(x.Y,z.Y);}
});
Test("Cinquenta mil movimentos variados mantêm saída finita e sem ganho de velocidade", () =>
{
    var random=new Random(20261109);var p=Make(full with{Smoothing=0.8,Diagonal=true});
    for(int i=0;i<50000;i++)
    {double x=random.Next(-1000,1001),y=random.Next(-1000,1001);var r=p.Process(x,y,i);True(double.IsFinite(r.X)&&double.IsFinite(r.Y));True(r.X*r.X+r.Y*r.Y<=x*x+y*y+1e-6);}
});
Test("Desativação com suavização limpa estado e preserva novos eventos", () =>
{
    var p=Make(full with{Smoothing=0.9});p.Process(1000,1,0);p.Configure(full with{Enabled=false,Smoothing=0.9});
    var r=p.Process(1,-1,1);Near(1,r.X);Near(-1,r.Y);True(!p.HasDirectionLock);
});

Test("Processamento determinístico e sem alocações no trecho quente", () =>
{
    var a = Make(); var b = Make();
    for (int i = 0; i < 5000; i++)
    {
        double dx = Math.Sin(i * 0.03) * 10, dy = Math.Cos(i * 0.02);
        var x = a.Process(dx, dy, i);
        var y = b.Process(dx, dy, i); True(x == y);
    }
    long before = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 10000; i++) a.Process(10, 1, i);
    long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
    Near(0, allocated);
});

int failed = 0;
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception e) { failed++; Console.WriteLine($"FAIL {test.Name}: {e.Message}"); }
}
Console.WriteLine($"RESULT {tests.Count - failed}/{tests.Count} testes aprovados");

if (args.Contains("--benchmark"))
{
    const int count = 2_000_000;
    var p = Make(); double checksum = 0;
    for (int i = 0; i < 100_000; i++) checksum += p.Process(10, 1, i).X;
    long allocated = GC.GetAllocatedBytesForCurrentThread();
    long start = Stopwatch.GetTimestamp();
    for (int i = 0; i < count; i++)
    {
        var r = p.Process(10, (i & 1) == 0 ? 1 : -1, i);
        checksum += r.X + r.Y;
    }
    double seconds = Stopwatch.GetElapsedTime(start).TotalSeconds;
    allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
    var result = new { runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString(), architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(), events = count, seconds, nanosecondsPerEvent = seconds * 1e9 / count, allocatedBytes = allocated, checksum, limitation = "Custo do algoritmo isolado; não mede latência de cursor, USB, hooks ou interface." };
    Console.WriteLine("BENCHMARK " + JsonSerializer.Serialize(result));
}
return failed == 0 ? 0 : 1;
