using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace MediVault.Licensing;

public sealed class MachineFingerprintProvider : IMachineFingerprintProvider
{
    private string? _cached;

    public string GetFingerprint()
    {
        if (_cached != null) return _cached;

        var machineGuid = ReadMachineGuid() ?? "no-guid";
        var machineName = Environment.MachineName;
        var processorCount = Environment.ProcessorCount.ToString();
        var osPlatform = Environment.OSVersion.Platform.ToString();

        var raw = $"{machineGuid}|{machineName}|{processorCount}|{osPlatform}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        _cached = Convert.ToHexString(hash);
        return _cached;
    }

    private static string? ReadMachineGuid()
    {
        try
        {
            using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            return key?.GetValue("MachineGuid") as string;
        }
        catch
        {
            return null;
        }
    }
}
