namespace UniversalMouse.Core.Motion;

/// <summary>Converts a virtual-desktop pixel to a representable 16-bit absolute input bin.</summary>
public static class DesktopCoordinates
{
    public static int NormalizePixel(int pixel, int origin, int extent)
    {
        if (extent is < 2 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(extent), "A dimensão precisa estar entre 2 e 65.536 pixels.");
        long offset = (long)pixel - origin;
        if (offset < 0 || offset >= extent)
            throw new ArgumentOutOfRangeException(nameof(pixel));

        // Windows absolute input selects floor(normalized * extent / 65536).
        // Center-floor works for usual desktop sizes, but can leave the bin when extent > 32768.
        // Exact integer bin bounds preserve every representable pixel even at those dimensions.
        long first = (offset * 65536 + extent - 1) / extent;
        long last = ((offset + 1) * 65536 + extent - 1) / extent - 1;
        long center = ((2 * offset + 1) * 65536) / (2 * extent);
        return (int)Math.Clamp(center, first, last);
    }
}
