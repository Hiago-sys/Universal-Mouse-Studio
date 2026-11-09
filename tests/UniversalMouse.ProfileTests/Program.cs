using System.Text.Json;
using UniversalMouse.Core.Profiles;
using UniversalMouse.Core.Hardware;
using UniversalMouse.Core.Diagnostics;
using UniversalMouse.Core.Motion;

int passed = 0;
void Assert(bool value, string name) { if (!value) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
async Task MustFail(Func<Task> task, string name) { try { await task(); } catch (Exception e) when (e is ArgumentException or InvalidDataException or NotSupportedException) { Assert(true, name); return; } throw new Exception("Did not reject: " + name); }
string directory = Path.Combine(Path.GetTempPath(), "ums-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    var store = new ProfileStore(directory);
    var profiles = await store.LoadAsync();
    Assert(profiles.Count == 6 && profiles.All(p => !p.Motion.Enabled), "Six safe defaults with processing disabled");
    await store.SaveAsync(profiles);
    var loaded = await store.LoadAsync();
    Assert(loaded.Select(p => p.Id).SequenceEqual(profiles.Select(p => p.Id)), "Round-trip preserves profile identity");
    await store.SaveAsync(loaded);
    string path = Path.Combine(directory, "profiles.json");
    await File.WriteAllTextAsync(path, "{corrupted");
    var recovered = await store.LoadAsync();
    Assert(recovered.Count == 6 && store.LastWarning is not null, "Valid backup recovers corrupt primary with warning");
    Assert(Directory.GetFiles(directory, "profiles.json.corrupt-*").Length == 1 && await File.ReadAllTextAsync(path) == "{corrupted", "Recovery preserves corrupt primary bytes");
    await store.SaveAsync(recovered);
    Assert((await store.LoadAsync()).Count == 6, "Recovered profiles save without destroying backup");
    string exported = Path.Combine(directory, "export.json");
    await store.ExportAsync(loaded[0], exported);
    var imported = await store.ImportAsync(exported);
    Assert(imported.Id != loaded[0].Id && imported.Name == loaded[0].Name, "Import assigns independent identity");
    await MustFail(() => store.SaveAsync([loaded[0], loaded[0]]), "Duplicate identity rejected");
    await MustFail(() => store.SaveAsync([loaded[0] with { PointerSpeed = 99 }]), "Invalid pointer speed rejected");
    await MustFail(() => store.SaveAsync([loaded[0] with { Motion = new MotionSettings { Strength = double.NaN } }]), "Non-finite motion rejected");
    await MustFail(() => store.SaveAsync([loaded[0] with { ApplicationExecutable = "C:\\test.exe" }]), "Executable association rejects paths");
    await File.WriteAllTextAsync(exported, new string('a', 1024 * 1024 + 1));
    await MustFail(() => store.ImportAsync(exported), "Oversized import rejected");
    await File.WriteAllTextAsync(path, "{bad");
    await File.WriteAllTextAsync(path + ".bak", "{badbackup");
    await MustFail(() => store.LoadAsync(), "Both corrupt files reported rather than replaced by defaults");

    var manager = new DeviceCapabilityManager();
    var caps = manager.GetCapabilities("FFFF", "FFFF");
    Assert(caps.Single(c => c.Key == "dpi").Support == CapabilitySupport.Unknown, "Unknown hardware DPI remains unknown");
    await MustFail(() => manager.ApplyLightingAsync(new DeviceIdentity("FFFF", "FFFF"), new(255, 0, 0)), "Unknown RGB emits no command");
    var polling = new PollingEstimator();
    for (int i = 0; i < 2000; i++) polling.Add(i * 2);
    var snapshot = polling.Snapshot();
    Assert(snapshot.Intervals == 1024 && Math.Abs(snapshot.AverageHz!.Value - 500) < .001, "Polling estimator is bounded and calculates 500 Hz fixture");
    polling.Reset();
    Assert(polling.Snapshot().AverageHz is null, "No samples produce unknown frequency");
    polling.Add(double.NaN); polling.Add(0); polling.Add(1000);
    Assert(polling.Snapshot().Intervals == 0, "Idle gaps and invalid timestamps excluded");
    var clicks = new ClickDiagnostics();
    clicks.Record("Left", 0); clicks.Record("Left", 20); clicks.Record("Left", 200);
    Assert(clicks.Snapshot().PossibleDuplicateIntervals == 1 && clicks.Snapshot().TotalClicks == 3, "Short clicks counted without asserting physical fault");
    string report = Path.Combine(directory, "report.json");
    await new DiagnosticReport(DateTimeOffset.UtcNow, snapshot, clicks.Snapshot()).SaveAsync(report);
    Assert(!await File.ReadAllTextAsync(report).ContinueWith(t => t.Result.Contains("DevicePath")), "Report excludes persistent device identifiers");
    Console.WriteLine($"{passed} tests passed.");
}
finally { Directory.Delete(directory, true); }
