using UniversalMouse.Core.Motion;

namespace UniversalMouse.Core.Profiles;

public sealed record StudioProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Personalizado";
    public MotionSettings Motion { get; init; } = new();
    public int PointerSpeed { get; init; } = 10;
    public bool EnhancePointerPrecision { get; init; } = true;
    public bool SwapButtons { get; init; }
    public int DoubleClickMilliseconds { get; init; } = 500;
    public int WheelLines { get; init; } = 3;
    public bool InactiveScroll { get; init; } = true;
    public bool ClickLock { get; init; }
    public string? ApplicationExecutable { get; init; }

    public static IReadOnlyList<StudioProfile> CreateDefaults() =>
    [
        new() { Name = "Padrão" },
        new() { Name = "Trabalho" },
        new() { Name = "Estudos" },
        new() { Name = "Jogos", EnhancePointerPrecision = false },
        new() { Name = "Precisão", PointerSpeed = 8 },
        new() { Name = "Personalizado" }
    ];

    public void Validate()
    {
        if (Id == Guid.Empty) throw new ArgumentException("O perfil precisa ter um identificador válido.");
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Name.Any(char.IsControl))
            throw new ArgumentException("O nome do perfil deve conter de 1 a 80 caracteres, sem controles.");
        if (PointerSpeed is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(PointerSpeed));
        if (DoubleClickMilliseconds is < 200 or > 900) throw new ArgumentOutOfRangeException(nameof(DoubleClickMilliseconds));
        if (WheelLines is < -1 or > 100) throw new ArgumentOutOfRangeException(nameof(WheelLines));
        if (Motion is null) throw new ArgumentException("As configurações de movimento são obrigatórias.");
        Motion.Validate();
        if (!string.IsNullOrEmpty(ApplicationExecutable) &&
            (ApplicationExecutable.Length > 180 || ApplicationExecutable.IndexOfAny(['/', '\\', ':']) >= 0 ||
             ApplicationExecutable.Any(char.IsControl) || !ApplicationExecutable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("A associação deve ser apenas o nome de um executável .exe, sem caminho.");
    }

}
