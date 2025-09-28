using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ArchSitePlan.Licensing;

public static class MachineCodeHelpers
{
    // Method to get MAC Address on Windows and macOS
    public static string GetHashedMacAddress()
    {
        string mac = GetMacAddress(); 
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(mac));
            return Convert.ToBase64String(bytes);
        }
    }

    private static string GetMacAddress()
    {
        // Retrieve the MAC address based on the platform
        string platformName = "Unknown OS";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            platformName = "Windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            platformName = "OSX";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            platformName = "Linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
            platformName = "Free BSD";

        else
            throw new PlatformNotSupportedException("Only Windows and macOS are supported.");

        return $"{platformName}:{CalculateMacAddress()}";
    }

    // Method to get MAC address on Windows
    private static string CalculateMacAddress()
    {
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback && nic.OperationalStatus == OperationalStatus.Up);

        foreach (var adapter in networkInterfaces)
        {
            var macAddress = adapter.GetPhysicalAddress();
            if (macAddress != null && !string.IsNullOrEmpty(macAddress.ToString()))
            {
                return macAddress.ToString();
            }
        }

        return "Windows MAC address not found.";
    }
}
