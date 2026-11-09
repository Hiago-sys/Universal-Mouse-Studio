namespace UniversalMouse.Core.Hardware;

public enum CapabilitySupport { WindowsNative, Software, Hardware, SpecificIntegration, Unsupported, Unknown }

public sealed record DeviceCapability(string Key, string Name, CapabilitySupport Support, string Detail);
public sealed record DeviceIdentity(string? VendorId, string? ProductId, string? InterfacePath = null);
public sealed record RgbConfiguration(byte Red, byte Green, byte Blue, byte Brightness = 255, string Effect = "Estático");

/// <summary>An adapter must match an exact documented protocol/interface. Generic HID identity alone is insufficient.</summary>
public interface IMouseDeviceAdapter
{
    string Name { get; }
    string ProtocolDocumentation { get; }
    bool Supports(DeviceIdentity device);
    IReadOnlyList<DeviceCapability> GetCapabilities(DeviceIdentity device);
}

public interface ILightingAdapter : IMouseDeviceAdapter
{
    Task ApplyAsync(DeviceIdentity device, RgbConfiguration configuration, CancellationToken cancellationToken = default);
}

public interface ISensorAdapter : IMouseDeviceAdapter
{
    Task<int?> ReadDpiAsync(DeviceIdentity device, CancellationToken cancellationToken = default);
    Task SetDpiAsync(DeviceIdentity device, int dpi, CancellationToken cancellationToken = default);
}

/// <summary>No adapters are registered by default; this component never guesses or sends generic HID feature reports.</summary>
public sealed class DeviceCapabilityManager
{
    private readonly IReadOnlyList<IMouseDeviceAdapter> _adapters;
    public DeviceCapabilityManager(IEnumerable<IMouseDeviceAdapter>? adapters = null)
    {
        _adapters = (adapters ?? []).ToArray();
        if (_adapters.Any(a => string.IsNullOrWhiteSpace(a.ProtocolDocumentation)))
            throw new ArgumentException("Adaptadores devem informar a documentação do protocolo.");
    }

    public IReadOnlyList<DeviceCapability> GetCapabilities(string? vendorId = null, string? productId = null)
        => GetCapabilities(new DeviceIdentity(vendorId, productId));

    public IReadOnlyList<DeviceCapability> GetCapabilities(DeviceIdentity identity)
    {
        var known = new List<DeviceCapability>
        {
            new("pointer-speed", "Velocidade do ponteiro", CapabilitySupport.WindowsNative, "Configuração global do Windows; não altera o DPI físico."),
            new("acceleration", "Aprimorar precisão do ponteiro", CapabilitySupport.WindowsNative, "Aceleração do ponteiro do Windows; consumidores de Raw Input podem ignorá-la."),
            new("buttons", "Inversão dos botões principais", CapabilitySupport.WindowsNative, "Configuração global do Windows."),
            new("click-scroll", "Cliques e rolagem", CapabilitySupport.WindowsNative, "Propriedades de clique duplo, ClickLock e linhas de rolagem do Windows."),
            new("angle-snapping", "Angle Snapping por software", CapabilitySupport.Software, "Correção do cursor desktop em modo usuário. Não substitui dados Raw Input de outros aplicativos; validação no Windows necessária."),
            new("polling-estimate", "Estimativa de frequência", CapabilitySupport.Software, "Intervalos entre eventos recebidos; não mede precisamente o barramento USB."),
            new("dpi", "DPI físico do sensor", CapabilitySupport.Unknown, "Nenhum protocolo documentado reconhecido; não informado pelo dispositivo."),
            new("rgb", "Iluminação física", CapabilitySupport.Unknown, "Nenhum adaptador documentado reconhecido. Comandos HID não serão enviados."),
            new("polling-hardware", "Polling rate físico", CapabilitySupport.Unknown, "Depende de protocolo específico; a estimativa por eventos não altera o hardware."),
            new("lift-off", "Lift-off Distance / Motion Sync", CapabilitySupport.Unknown, "Depende do sensor e do protocolo do fabricante."),
            new("debounce", "Debounce físico ou firmware", CapabilitySupport.Unknown, "Não é inferido de intervalos de clique observados."),
            new("filter-driver", "Filtro de entrada opcional", CapabilitySupport.Unsupported, "Nenhum driver distribuído ou instalado nesta versão.")
        };
        foreach (var adapter in _adapters.Where(a => a.Supports(identity)))
            foreach (var capability in adapter.GetCapabilities(identity))
            {
                known.RemoveAll(c => c.Key == capability.Key);
                known.Add(capability);
            }
        return known;
    }

    public Task ApplyLightingAsync(DeviceIdentity identity, RgbConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var matches = _adapters.OfType<ILightingAdapter>().Where(a => a.Supports(identity) &&
            a.GetCapabilities(identity).Any(c => c.Key == "rgb" && c.Support == CapabilitySupport.Hardware)).ToArray();
        if (matches.Length != 1) throw new NotSupportedException("Nenhum adaptador de iluminação unívoco e documentado foi identificado para este dispositivo.");
        return matches[0].ApplyAsync(identity, configuration, cancellationToken);
    }
}
