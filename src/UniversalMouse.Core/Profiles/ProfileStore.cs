using System.Text.Json;

namespace UniversalMouse.Core.Profiles;

/// <summary>Local JSON store. Recovery preserves the corrupt primary; it never treats it as an empty profile list.</summary>
public sealed class ProfileStore(string directory)
{
    private const int MaximumBytes = 1024 * 1024;
    private const int MaximumProfiles = 256;
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, MaxDepth = 16 };
    private readonly string _path = Path.Combine(Path.GetFullPath(directory), "profiles.json");
    private readonly SemaphoreSlim _gate = new(1, 1);
    public string? LastWarning { get; private set; }

    public async Task<IReadOnlyList<StudioProfile>> LoadAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            LastWarning = null;
            if (!File.Exists(_path)) return StudioProfile.CreateDefaults();
            try { return await ReadProfilesAsync(_path).ConfigureAwait(false); }
            catch (Exception e) when (e is JsonException or ArgumentException or InvalidDataException)
            {
                string backup = _path + ".bak";
                if (File.Exists(backup))
                {
                    try
                    {
                        var recovered = await ReadProfilesAsync(backup).ConfigureAwait(false);
                        string retained = _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
                        File.Copy(_path, retained, false);
                        LastWarning = "Perfis recuperados do backup. O arquivo corrompido foi preservado: " + retained;
                        return recovered;
                    }
                    catch (Exception backupError) when (backupError is JsonException or ArgumentException or InvalidDataException)
                    { throw new InvalidDataException("O arquivo de perfis e seu backup são inválidos. Os arquivos foram preservados.", backupError); }
                }
                throw new InvalidDataException("O arquivo de perfis é inválido. Foi preservado para recuperação; não será sobrescrito automaticamente.", e);
            }
        }
        finally { _gate.Release(); }
    }

    public async Task SaveAsync(IEnumerable<StudioProfile> profiles)
    {
        var items = profiles.Take(MaximumProfiles + 1).ToArray();
        ValidateList(items);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            // Do not silently turn corruption into a new empty store or destroy the last valid backup.
            if (File.Exists(_path))
            {
                try
                {
                    await ReadProfilesAsync(_path).ConfigureAwait(false);
                    File.Copy(_path, _path + ".bak", true);
                }
                catch (Exception e) when (e is JsonException or ArgumentException or InvalidDataException)
                {
                    if (LastWarning is null) throw new InvalidDataException("Carregue e recupere os perfis antes de salvar sobre um arquivo inválido.", e);
                    // LoadAsync already preserved this invalid primary and verified the backup.
                }
            }
            await AtomicWriteAsync(_path, items).ConfigureAwait(false);
            LastWarning = null;
        }
        finally { _gate.Release(); }
    }

    public async Task<StudioProfile> ImportAsync(string path)
    {
        var profile = await ReadBoundedAsync<StudioProfile>(path).ConfigureAwait(false)
            ?? throw new InvalidDataException("O perfil importado está vazio.");
        profile.Validate();
        // An imported profile is a new entity, never an overwrite of an existing ID.
        return profile with { Id = Guid.NewGuid() };
    }

    public async Task ExportAsync(StudioProfile profile, string path)
    {
        profile.Validate();
        await AtomicWriteAsync(Path.GetFullPath(path), profile).ConfigureAwait(false);
    }

    private static async Task<StudioProfile[]> ReadProfilesAsync(string path)
    {
        var profiles = await ReadBoundedAsync<StudioProfile[]>(path).ConfigureAwait(false)
            ?? throw new InvalidDataException("A lista de perfis está vazia.");
        ValidateList(profiles);
        return profiles;
    }

    private static void ValidateList(StudioProfile[] profiles)
    {
        if (profiles.Length is < 1 or > MaximumProfiles) throw new InvalidDataException("A lista deve conter entre 1 e 256 perfis.");
        var ids = new HashSet<Guid>();
        foreach (var profile in profiles)
        {
            if (profile is null) throw new InvalidDataException("Perfil nulo.");
            profile.Validate();
            if (!ids.Add(profile.Id)) throw new InvalidDataException("A lista contém identificadores duplicados.");
        }
    }

    private static async Task<T?> ReadBoundedAsync<T>(string path)
    {
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        if (file.Length > MaximumBytes) throw new InvalidDataException("O arquivo excede o limite de 1 MiB.");
        // Read through a bounded buffer so concurrent file growth cannot bypass the size limit.
        byte[] buffer = new byte[MaximumBytes + 1];
        int total = 0, read;
        while (total < buffer.Length && (read = await file.ReadAsync(buffer.AsMemory(total)).ConfigureAwait(false)) > 0) total += read;
        if (total > MaximumBytes) throw new InvalidDataException("O arquivo excede o limite de 1 MiB.");
        return JsonSerializer.Deserialize<T>(buffer.AsSpan(0, total), Json);
    }

    private static async Task AtomicWriteAsync<T>(string path, T value)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, Json);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("O arquivo excede o limite de 1 MiB.");
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true))
            {
                await stream.WriteAsync(bytes).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
