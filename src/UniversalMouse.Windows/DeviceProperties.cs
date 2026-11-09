using System.Runtime.InteropServices;
using System.Text;
namespace UniversalMouse.Windows;

/// <summary>Read-only SetupAPI enrichment. Missing properties remain unknown.</summary>
internal static class DeviceProperties
{
    internal sealed record Information(string? Name, string? Manufacturer, string? Transport);
    public static Information Read(string path)
    {
        IntPtr set = SetupDiCreateDeviceInfoList(IntPtr.Zero, IntPtr.Zero);
        if (set == new IntPtr(-1)) return new(null, null, null);
        IntPtr detail = IntPtr.Zero;
        try
        {
            var iface = new InterfaceData { Size = Marshal.SizeOf<InterfaceData>() };
            if (!SetupDiOpenDeviceInterface(set, path, 0, ref iface)) return new(null, null, null);
            var dev = new DeviceInfoData { Size = Marshal.SizeOf<DeviceInfoData>() };
            SetupDiGetDeviceInterfaceDetail(set, ref iface, IntPtr.Zero, 0, out uint needed, ref dev);
            if (needed is < 8 or > 65536) return new(null, null, null);
            detail = Marshal.AllocHGlobal((int)needed);
            Marshal.WriteInt32(detail, IntPtr.Size == 8 ? 8 : 6);
            if (!SetupDiGetDeviceInterfaceDetail(set, ref iface, detail, needed, out _, ref dev)) return new(null, null, null);
            string? name = Get(set, ref dev, 12) ?? Get(set, ref dev, 0);
            string? manufacturer = Get(set, ref dev, 11);
            string? enumerator = Get(set, ref dev, 22);
            string? transport = enumerator?.ToUpperInvariant() switch { "USB" => "USB", "BTHENUM" or "BTHLEDEVICE" => "Bluetooth", _ => null };
            transport ??= AncestorTransport(dev.DevInst);
            return new(name, manufacturer, transport);
        }
        catch { return new(null, null, null); }
        finally { if (detail != IntPtr.Zero) Marshal.FreeHGlobal(detail); SetupDiDestroyDeviceInfoList(set); }
    }
    static string? Get(IntPtr set, ref DeviceInfoData device, uint property)
    {
        var buffer = new byte[2048];
        if (!SetupDiGetDeviceRegistryProperty(set, ref device, property, out uint type, buffer, (uint)buffer.Length, out uint size) || type != 1 || size > buffer.Length) return null;
        var value = Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    static string? AncestorTransport(uint node)
    {
        for (int depth = 0; depth < 8; depth++)
        {
            var id = new StringBuilder(1024);
            if (CM_Get_Device_ID(node, id, id.Capacity, 0) == 0)
            {
                string value = id.ToString();
                if (value.StartsWith("BTH", StringComparison.OrdinalIgnoreCase)) return "Bluetooth (árvore PnP)";
                if (value.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase)) return "USB (direto ou receptor)";
            }
            if (CM_Get_Parent(out uint parent, node, 0) != 0) break;
            node = parent;
        }
        return null;
    }
    [DllImport("cfgmgr32.dll")] static extern uint CM_Get_Parent(out uint parent, uint node, uint flags);
    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_IDW", CharSet = CharSet.Unicode)] static extern uint CM_Get_Device_ID(uint node, StringBuilder id, int size, uint flags);
    [StructLayout(LayoutKind.Sequential)] struct InterfaceData { public int Size; public Guid InterfaceClass; public uint Flags; public nuint Reserved; }
    [StructLayout(LayoutKind.Sequential)] struct DeviceInfoData { public int Size; public Guid ClassGuid; public uint DevInst; public nuint Reserved; }
    [DllImport("setupapi.dll", SetLastError = true)] static extern IntPtr SetupDiCreateDeviceInfoList(IntPtr classGuid, IntPtr hwnd);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetupDiOpenDeviceInterface(IntPtr set, string path, uint flags, ref InterfaceData data);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data, IntPtr detail, uint size, out uint needed, ref DeviceInfoData device);
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool SetupDiGetDeviceRegistryProperty(IntPtr set, ref DeviceInfoData device, uint property, out uint type, [Out] byte[] buffer, uint size, out uint needed);
    [DllImport("setupapi.dll")] static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
}
