using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;
namespace UniversalMouse.Windows;

public sealed record MouseDevice(string Id, string Name, string Manufacturer, ushort? VendorId,
    ushort? ProductId, string Connection, int? ButtonCount, string DevicePath);
public readonly record struct RawMouseSample(string DeviceId, int DeltaX, int DeltaY, double TimestampMilliseconds,
    ushort Buttons, short WheelDelta, short HorizontalWheelDelta);

/// <summary>Observes relative raw input without suppressing legacy mouse messages.</summary>
public sealed class RawInputService : IDisposable
{
    readonly HwndSource source;
    Dictionary<IntPtr, string> identities = new();
    bool disposed;
    public event Action<RawMouseSample>? SampleReceived;
    public event Action? DevicesChanged;
    public RawInputService(IntPtr hwnd)
    {
        source = HwndSource.FromHwnd(hwnd) ?? throw new ArgumentException("Janela nativa indisponível.");
        var registrations = new[] { new RawDevice { UsagePage = 1, Usage = 2, Flags = 0x2100, Target = hwnd } }; // INPUTSINK + DEVNOTIFY
        if (!RegisterRawInputDevices(registrations, 1, (uint)Marshal.SizeOf<RawDevice>())) throw new Win32Exception();
        source.AddHook(WndProc);
    }
    public IReadOnlyList<MouseDevice> EnumerateDevices()
    {
        uint count = 0;
        uint size = (uint)Marshal.SizeOf<DeviceList>();
        if (GetRawInputDeviceList(null, ref count, size) == uint.MaxValue) throw new Win32Exception();
        if (count > 4096) throw new InvalidOperationException("Inventário HID excede o limite seguro.");
        var list = new DeviceList[count];
        if (GetRawInputDeviceList(list, ref count, size) == uint.MaxValue) throw new Win32Exception();
        var result = new List<MouseDevice>();
        var newIdentities = new Dictionary<IntPtr, string>();
        foreach (var device in list.Take((int)count).Where(d => d.Type == 0))
        {
            uint chars = 0;
            GetRawInputDeviceName(device.Handle, 0x20000007, null, ref chars);
            if (chars is 0 or > 32768) continue;
            var path = new StringBuilder((int)chars + 1);
            if (GetRawInputDeviceName(device.Handle, 0x20000007, path, ref chars) == uint.MaxValue) continue;
            string id = path.ToString();
            var info = new DeviceInfo { Size = (uint)Marshal.SizeOf<DeviceInfo>() };
            uint infoSize = info.Size;
            bool known = GetRawInputDeviceInformation(device.Handle, 0x2000000b, ref info, ref infoSize) != uint.MaxValue;
            string name = "Mouse HID", manufacturer = "Não informado pelo dispositivo";
            ushort? vid = null, pid = null;
            using var handle = CreateFile(id, 0, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (!handle.IsInvalid)
            {
                var attributes = new HidAttributes { Size = Marshal.SizeOf<HidAttributes>() };
                if (HidD_GetAttributes(handle, ref attributes)) { vid = attributes.VendorId; pid = attributes.ProductId; }
                var buffer = new byte[512];
                if (HidD_GetProductString(handle, buffer, buffer.Length)) name = Decode(buffer, name);
                Array.Clear(buffer);
                if (HidD_GetManufacturerString(handle, buffer, buffer.Length)) manufacturer = Decode(buffer, manufacturer);
            }
            if (vid is null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(id, @"VID_([0-9A-F]{4}).*PID_([0-9A-F]{4})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success) { vid = Convert.ToUInt16(match.Groups[1].Value, 16); pid = Convert.ToUInt16(match.Groups[2].Value, 16); }
            }
            var setup = DeviceProperties.Read(id);
            if (name == "Mouse HID" && setup.Name != null) name = setup.Name;
            if (manufacturer == "Não informado pelo dispositivo" && setup.Manufacturer != null) manufacturer = setup.Manufacturer;
            string connection = setup.Transport ?? (id.Contains("BTH", StringComparison.OrdinalIgnoreCase) ? "Bluetooth (identificador)" : "HID — transporte não informado");
            result.Add(new(id, name, manufacturer, vid, pid, connection, known ? (int)info.Buttons : null, id));
            newIdentities[device.Handle] = id;
        }
        Volatile.Write(ref identities, newIdentities);
        return result;
    }
    static string Decode(byte[] bytes, string fallback)
    {
        string value = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
    unsafe IntPtr WndProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x00FE) { DevicesChanged?.Invoke(); return IntPtr.Zero; }
        if (message != 0x00FF || disposed) return IntPtr.Zero;
        uint size = 0, headerSize = (uint)Marshal.SizeOf<RawHeader>();
        if (GetRawInputData(lParam, 0x10000003, IntPtr.Zero, ref size, headerSize) == uint.MaxValue || size > 256 || size < headerSize + 24) return IntPtr.Zero;
        byte* buffer = stackalloc byte[256];
        if (GetRawInputData(lParam, 0x10000003, (IntPtr)buffer, ref size, headerSize) == uint.MaxValue) return IntPtr.Zero;
        var header = *(RawHeader*)buffer;
        if (header.Type != 0) return IntPtr.Zero;
        byte* mouse = buffer + headerSize;
        if ((*(ushort*)mouse & 1) != 0) return IntPtr.Zero; // Absolute packets aren't relative motion.
        uint buttons = *(uint*)(mouse + 4);
        ushort flags = (ushort)buttons;
        short wheel = (short)(buttons >> 16);
        string id = Volatile.Read(ref identities).GetValueOrDefault(header.Device, "Dispositivo ainda não enumerado");
        var sample = new RawMouseSample(id, *(int*)(mouse + 12), *(int*)(mouse + 16),
            Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency, flags,
            (flags & 0x0400) != 0 ? wheel : (short)0, (flags & 0x0800) != 0 ? wheel : (short)0);
        SampleReceived?.Invoke(sample);
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.RemoveHook(WndProc);
        var remove = new[] { new RawDevice { UsagePage = 1, Usage = 2, Flags = 1, Target = IntPtr.Zero } };
        RegisterRawInputDevices(remove, 1, (uint)Marshal.SizeOf<RawDevice>());
        Volatile.Write(ref identities, new());
    }
    [StructLayout(LayoutKind.Sequential)] struct DeviceList { public IntPtr Handle; public uint Type; }
    [StructLayout(LayoutKind.Sequential)] struct RawDevice { public ushort UsagePage, Usage; public uint Flags; public IntPtr Target; }
    [StructLayout(LayoutKind.Sequential)] struct RawHeader { public uint Type, Size; public IntPtr Device, WParam; }
    [StructLayout(LayoutKind.Sequential)] struct DeviceInfo { public uint Size, Type, Id, Buttons, SampleRate, HorizontalWheel; public uint Padding1, Padding2; }
    [StructLayout(LayoutKind.Sequential)] struct HidAttributes { public int Size; public ushort VendorId, ProductId, Version; }
    [DllImport("user32.dll", SetLastError = true)] static extern uint GetRawInputDeviceList([In, Out] DeviceList[]? devices, ref uint count, uint size);
    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", CharSet = CharSet.Unicode, SetLastError = true)] static extern uint GetRawInputDeviceName(IntPtr device, uint command, StringBuilder? data, ref uint size);
    [DllImport("user32.dll", EntryPoint = "GetRawInputDeviceInfoW", SetLastError = true)] static extern uint GetRawInputDeviceInformation(IntPtr device, uint command, ref DeviceInfo info, ref uint size);
    [DllImport("user32.dll", SetLastError = true)] static extern bool RegisterRawInputDevices(RawDevice[] devices, uint count, uint size);
    [DllImport("user32.dll", SetLastError = true)] static extern uint GetRawInputData(IntPtr raw, uint command, IntPtr data, ref uint size, uint headerSize);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetAttributes(SafeFileHandle handle, ref HidAttributes attributes);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetProductString(SafeFileHandle handle, [Out] byte[] buffer, int length);
    [DllImport("hid.dll")] [return: MarshalAs(UnmanagedType.U1)] static extern bool HidD_GetManufacturerString(SafeFileHandle handle, [Out] byte[] buffer, int length);
}
